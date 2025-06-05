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

    public async Task<List<WorkItemNode>> GetWorkItemHierarchyAsync(string areaPath, string? state = null, string? tags = null)
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

        var wiql = BuildWiql(areaPath, state, tags);
        var wiqlResponse = await _httpClient.PostAsJsonAsync($"{baseUri}/wiql?api-version=7.0", new { query = wiql });
        wiqlResponse.EnsureSuccessStatusCode();
        var wiqlResult = await wiqlResponse.Content.ReadFromJsonAsync<WiqlResult>();
        if (wiqlResult == null || wiqlResult.WorkItemRelations == null || wiqlResult.WorkItemRelations.Length == 0)
            return new List<WorkItemNode>();

        var ids = wiqlResult.WorkItemRelations
            .SelectMany(r => new[] { r.Source?.Id, r.Target?.Id })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct();

        var workItems = new List<WorkItem>();
        foreach (var chunk in ids.Chunk(200))
        {
            var idList = string.Join(',', chunk);
            var itemsResult = await _httpClient.GetFromJsonAsync<WorkItemsResult>($"{baseUri}/workitems?ids={idList}&$expand=relations&api-version=7.0");
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
                Tags = w.Fields.TryGetValue("System.Tags", out var tagEl) ? tagEl.GetString() ?? string.Empty : string.Empty
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
        var roots = nodes.Values.Where(n => !childIds.Contains(n.Info.Id)).ToList();
        foreach (var root in roots)
            ComputeStatus(root);
        return roots;
    }

    private static string BuildWiql(string areaPath, string? state, string? tags)
    {
        var conditions = new List<string>
        {
            "[Source].[System.TeamProject] = @project",
            $"[Source].[System.AreaPath] UNDER '{areaPath}'",
            "[Source].[System.WorkItemType] in ('Epic','Feature','User Story','Task','Bug')",
            "[System.Links.LinkType] = 'System.LinkTypes.Hierarchy-Forward'"
        };
        if (!string.IsNullOrWhiteSpace(state))
        {
            var states = state.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (states.Length == 1)
                conditions.Add($"[Source].[System.State] = '{states[0]}'");
            else
                conditions.Add($"[Source].[System.State] in ({string.Join(',', states.Select(s => $"'{s}'"))})");
        }

        if (!string.IsNullOrWhiteSpace(tags))
        {
            var tagList = tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var tag in tagList)
                conditions.Add($"[Source].[System.Tags] CONTAINS '{tag}'");
        }

        var where = string.Join(" AND ", conditions);
        return $@"SELECT [System.Id] FROM WorkItemLinks WHERE {where} ORDER BY [System.Id] MODE (Recursive, ReturnMatchingChildren)";
    }

    private static void ComputeStatus(WorkItemNode node)
    {
        foreach (var child in node.Children)
            ComputeStatus(child);

        if (!node.Children.Any())
        {
            node.StatusValid = true;
            return;
        }

        var allDone = node.Children.All(c => c.Info.State.Equals("Done", StringComparison.OrdinalIgnoreCase));
        node.StatusValid = node.Info.State.Equals("Done", StringComparison.OrdinalIgnoreCase) ? allDone : !allDone;
    }

    private class WiqlResult
    {
        public WorkItemRelation[] WorkItemRelations { get; set; } = Array.Empty<WorkItemRelation>();
    }

    private class WorkItemRelation
    {
        public WorkItemRef? Source { get; set; }
        public WorkItemRef? Target { get; set; }
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
    public string Tags { get; set; } = string.Empty;
}

public class WorkItemNode
{
    public WorkItemInfo Info { get; set; } = new();
    public List<WorkItemNode> Children { get; } = new();
    public bool StatusValid { get; set; }
}
