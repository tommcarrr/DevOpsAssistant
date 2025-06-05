using System.Net.Http.Headers;
using System.Text.Json;
using System.Net.Http.Json;
using System.Linq;
using DevOpsAssistant.Services;

namespace DevOpsAssistant.Services;

public class DevOpsApiService
{
    private readonly HttpClient _httpClient;
    private readonly DevOpsConfigService _configService;

    public DevOpsApiService(HttpClient httpClient, DevOpsConfigService configService)
    {
        _httpClient = httpClient;
        _configService = configService;
    }

    public async Task<List<WorkItemNode>> GetWorkItemHierarchyAsync(string areaPath)
    {
        var config = _configService.Config;
        if (string.IsNullOrWhiteSpace(config.Organization) ||
            string.IsNullOrWhiteSpace(config.Project) ||
            string.IsNullOrWhiteSpace(config.PatToken))
        {
            throw new InvalidOperationException("DevOps configuration is incomplete.");
        }

        var baseUri = $"https://dev.azure.com/{config.Organization}/{config.Project}/_apis/wit";
        var itemUrlBase = $"https://dev.azure.com/{config.Organization}/{config.Project}/_workitems/edit/";
        var pat = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{config.PatToken}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", pat);

        var wiql = BuildWiql(areaPath);
        var wiqlResponse = await _httpClient.PostAsJsonAsync($"{baseUri}/wiql?api-version=7.0", new { query = wiql });
        wiqlResponse.EnsureSuccessStatusCode();
        var wiqlResult = await wiqlResponse.Content.ReadFromJsonAsync<WiqlResult>();
        if (wiqlResult == null || wiqlResult.WorkItems == null || wiqlResult.WorkItems.Length == 0)
            return new List<WorkItemNode>();

        var ids = wiqlResult.WorkItems
            .Select(w => w.Id)
            .Distinct();

        var workItems = new List<WorkItem>();
        var fetchTasks = ids.Chunk(200)
            .Select(chunk =>
            {
                var idList = string.Join(',', chunk);
                return _httpClient.GetFromJsonAsync<WorkItemsResult>($"{baseUri}/workitems?ids={idList}&$expand=relations&api-version=7.0");
            })
            .ToArray();

        var results = await Task.WhenAll(fetchTasks);
        foreach (var itemsResult in results)
        {
            if (itemsResult?.Value != null)
                workItems.AddRange(itemsResult.Value);
        }
        if (!workItems.Any())
            return new List<WorkItemNode>();

        var dict = workItems.ToDictionary(i => i.Id);
        var nodes = dict.Values.Select(w => new WorkItemNode
        {
            Info = new WorkItemInfo
            {
                Id = w.Id,
                Title = w.Fields["System.Title"].GetString() ?? string.Empty,
                State = w.Fields["System.State"].GetString() ?? string.Empty,
                WorkItemType = w.Fields["System.WorkItemType"].GetString() ?? string.Empty,
                Url = $"{itemUrlBase}{w.Id}"
            }
        }).ToDictionary(n => n.Info.Id);

        foreach (var item in workItems)
        {
            if (item.Relations == null) continue;
            foreach (var rel in item.Relations.Where(r => r.Rel == "System.LinkTypes.Hierarchy-Forward"))
            {
                var childId = int.Parse(rel.Url.Split('/').Last());
                if (nodes.TryGetValue(item.Id, out var parent) && nodes.TryGetValue(childId, out var child))
                {
                    parent.Children.Add(child);
                }
            }
        }

        var childIds = new HashSet<int>(nodes.Values.SelectMany(n => n.Children.Select(c => c.Info.Id)));
        var roots = nodes.Values.Where(n =>
            !childIds.Contains(n.Info.Id) &&
            n.Info.WorkItemType.Equals("Epic", StringComparison.OrdinalIgnoreCase))
            .ToList();
        roots = FilterClosedEpics(roots);
        foreach (var root in roots)
            ComputeStatus(root);
        return roots;
    }

    public async Task<string[]> GetBacklogsAsync()
    {
        var config = _configService.Config;
        if (string.IsNullOrWhiteSpace(config.Organization) ||
            string.IsNullOrWhiteSpace(config.Project) ||
            string.IsNullOrWhiteSpace(config.PatToken))
        {
            throw new InvalidOperationException("DevOps configuration is incomplete.");
        }

        var baseUri = $"https://dev.azure.com/{config.Organization}/{config.Project}/_apis/wit";
        var pat = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{config.PatToken}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", pat);

        var result = await _httpClient.GetFromJsonAsync<JsonElement>($"{baseUri}/classificationnodes/areas?$depth=2&api-version=7.0");
        var list = new List<string>();
        if (result.TryGetProperty("children", out var children))
        {
            foreach (var child in children.EnumerateArray())
                ExtractPaths(child, list);
        }

        var normalized = list.Select(NormalizeAreaPath).ToArray();
        return normalized;
    }

    private static void ExtractPaths(JsonElement el, List<string> list)
    {
        if (el.TryGetProperty("path", out var path))
        {
            var p = path.GetString();
            if (!string.IsNullOrEmpty(p))
                list.Add(p);
        }
        if (el.TryGetProperty("children", out var children))
        {
            foreach (var child in children.EnumerateArray())
                ExtractPaths(child, list);
        }
    }

    private static List<WorkItemNode> FilterClosedEpics(IEnumerable<WorkItemNode> roots)
    {
        return roots.Where(r => !(r.Info.WorkItemType.Equals("Epic", StringComparison.OrdinalIgnoreCase) &&
                                  (r.Info.State.Equals("Closed", StringComparison.OrdinalIgnoreCase) ||
                                   r.Info.State.Equals("Removed", StringComparison.OrdinalIgnoreCase)))).ToList();
    }

    private static string NormalizeAreaPath(string areaPath)
    {
        areaPath = areaPath.TrimStart('\\', '/');
        var segments = areaPath.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length >= 2 && segments[1].Equals("Area", StringComparison.OrdinalIgnoreCase))
        {
            areaPath = string.Join('\\', new[] { segments[0] }.Concat(segments.Skip(2)));
        }
        return areaPath;
    }

    private static string BuildWiql(string areaPath)
    {
        areaPath = NormalizeAreaPath(areaPath);
        var conditions = new List<string>
        {
            "[System.TeamProject] = @project",
            $"[System.AreaPath] UNDER '{areaPath}'",
            "[System.WorkItemType] in ('Epic','Feature','User Story','Task','Bug')",
            "([System.WorkItemType] <> 'Epic' OR ([System.State] <> 'Closed' AND [System.State] <> 'Removed'))"
        };

        var where = string.Join(" AND ", conditions);
        return $"SELECT [System.Id] FROM WorkItems WHERE {where} ORDER BY [System.Id]";
    }

    private static void ComputeStatus(WorkItemNode node)
    {
        foreach (var child in node.Children)
            ComputeStatus(child);

        if (!node.Children.Any())
        {
            node.ExpectedState = node.Info.State;
            node.StatusValid = true;
            return;
        }

        bool allClosed = node.Children.All(c =>
            c.Info.State.Equals("Closed", StringComparison.OrdinalIgnoreCase) ||
            c.Info.State.Equals("Removed", StringComparison.OrdinalIgnoreCase) ||
            c.Info.State.Equals("Done", StringComparison.OrdinalIgnoreCase));

        bool anyNotNew = node.Children.Any(c => !c.Info.State.Equals("New", StringComparison.OrdinalIgnoreCase));

        var expected = allClosed ? "Closed" : anyNotNew ? "Active" : "New";

        node.ExpectedState = expected;
        node.StatusValid = node.Info.State.Equals(expected, StringComparison.OrdinalIgnoreCase);
    }

    private class WiqlResult
    {
        public WorkItemRef[] WorkItems { get; set; } = Array.Empty<WorkItemRef>();
    }

    private class WorkItemRef
    {
        public int Id { get; set; }
    }

    private class WorkItemsResult
    {
        public WorkItem[] Value { get; set; } = Array.Empty<WorkItem>();
    }

    private class WorkItem
    {
        public int Id { get; set; }
        public Dictionary<string, JsonElement> Fields { get; set; } = new();
        public Relation[]? Relations { get; set; }
    }

    private class Relation
    {
        public string Rel { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}

public class WorkItemInfo
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string WorkItemType { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class WorkItemNode
{
    public WorkItemInfo Info { get; set; } = new();
    public List<WorkItemNode> Children { get; } = new();
    public string ExpectedState { get; set; } = string.Empty;
    public bool StatusValid { get; set; }
}
