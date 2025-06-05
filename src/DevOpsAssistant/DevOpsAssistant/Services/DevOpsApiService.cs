using System.Net.Http.Headers;
using System.Text.Json;
using System.Net.Http.Json;
using System.Linq;

namespace DevOpsAssistant.Services;

public class DevOpsApiService
{
    private const string ApiVersion = "7.0";

    private readonly HttpClient _httpClient;
    private readonly DevOpsConfigService _configService;

    public DevOpsApiService(HttpClient httpClient, DevOpsConfigService configService)
    {
        _httpClient = httpClient;
        _configService = configService;
    }

    private DevOpsConfig GetValidatedConfig()
    {
        var config = _configService.Config;
        if (string.IsNullOrWhiteSpace(config.Organization) ||
            string.IsNullOrWhiteSpace(config.Project) ||
            string.IsNullOrWhiteSpace(config.PatToken))
        {
            throw new InvalidOperationException("DevOps configuration is incomplete.");
        }
        return config;
    }

    private void ApplyAuthentication(DevOpsConfig config)
    {
        var pat = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{config.PatToken}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", pat);
    }

    private static string BuildBaseUri(DevOpsConfig config) =>
        $"https://dev.azure.com/{config.Organization}/{config.Project}/_apis/wit";

    private static string BuildItemUrlBase(DevOpsConfig config) =>
        $"https://dev.azure.com/{config.Organization}/{config.Project}/_workitems/edit/";

    public async Task<List<WorkItemNode>> GetWorkItemHierarchyAsync(string areaPath)
    {
        var config = GetValidatedConfig();
        ApplyAuthentication(config);

        var baseUri = BuildBaseUri(config);
        var itemUrlBase = BuildItemUrlBase(config);

        var wiql = BuildWiql(areaPath);
        var wiqlResponse = await _httpClient.PostAsJsonAsync($"{baseUri}/wiql?api-version={ApiVersion}", new { query = wiql });
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
                return _httpClient.GetFromJsonAsync<WorkItemsResult>($"{baseUri}/workitems?ids={idList}&$expand=relations&api-version={ApiVersion}");
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
        var config = GetValidatedConfig();
        ApplyAuthentication(config);

        var baseUri = BuildBaseUri(config);

        var result = await _httpClient.GetFromJsonAsync<JsonElement>($"{baseUri}/classificationnodes/areas?$depth=2&api-version={ApiVersion}");
        var list = new List<string>();
        if (result.TryGetProperty("children", out var children))
        {
            foreach (var child in children.EnumerateArray())
                ExtractPaths(child, list);
        }

        var normalized = list.Select(NormalizeAreaPath).ToArray();
        return normalized;
    }

    public async Task<List<WorkItemDetails>> GetValidationItemsAsync(string areaPath)
    {
        var config = GetValidatedConfig();
        ApplyAuthentication(config);

        var baseUri = BuildBaseUri(config);
        var itemUrlBase = BuildItemUrlBase(config);

        var wiql = BuildValidationWiql(areaPath);
        var wiqlResponse = await _httpClient.PostAsJsonAsync($"{baseUri}/wiql?api-version={ApiVersion}", new { query = wiql });
        wiqlResponse.EnsureSuccessStatusCode();
        var wiqlResult = await wiqlResponse.Content.ReadFromJsonAsync<WiqlResult>();
        if (wiqlResult == null || wiqlResult.WorkItems == null || wiqlResult.WorkItems.Length == 0)
            return new List<WorkItemDetails>();

        var ids = wiqlResult.WorkItems.Select(w => w.Id).Distinct();
        var workItems = new List<WorkItem>();
        var fetchTasks = ids.Chunk(200).Select(chunk =>
        {
            var idList = string.Join(',', chunk);
            return _httpClient.GetFromJsonAsync<WorkItemsResult>($"{baseUri}/workitems?ids={idList}&$expand=relations&api-version={ApiVersion}");
        }).ToArray();

        var results = await Task.WhenAll(fetchTasks);
        foreach (var itemsResult in results)
        {
            if (itemsResult?.Value != null)
                workItems.AddRange(itemsResult.Value);
        }
        if (!workItems.Any())
            return new List<WorkItemDetails>();

        var list = new List<WorkItemDetails>();
        foreach (var w in workItems)
        {
            var details = new WorkItemDetails
            {
                Info = new WorkItemInfo
                {
                    Id = w.Id,
                    Title = w.Fields["System.Title"].GetString() ?? string.Empty,
                    State = w.Fields["System.State"].GetString() ?? string.Empty,
                    WorkItemType = w.Fields["System.WorkItemType"].GetString() ?? string.Empty,
                    Url = $"{itemUrlBase}{w.Id}"
                },
                HasDescription = w.Fields.TryGetValue("System.Description", out var desc) && !string.IsNullOrWhiteSpace(desc.GetString()),
                HasStoryPoints = w.Fields.TryGetValue("Microsoft.VSTS.Scheduling.StoryPoints", out var sp) && (sp.ValueKind == JsonValueKind.Number || !string.IsNullOrWhiteSpace(sp.ToString())),
                HasAcceptanceCriteria = w.Fields.TryGetValue("Microsoft.VSTS.Common.AcceptanceCriteria", out var ac) && !string.IsNullOrWhiteSpace(ac.GetString()),
                HasAssignee = w.Fields.TryGetValue("System.AssignedTo", out var at) && !string.IsNullOrWhiteSpace(at.GetString()),
                HasParent = w.Relations?.Any(r => r.Rel == "System.LinkTypes.Hierarchy-Reverse") == true
            };
            list.Add(details);
        }

        return list;
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

    private static string BuildValidationWiql(string areaPath)
    {
        areaPath = NormalizeAreaPath(areaPath);
        return $"SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = @project AND [System.AreaPath] UNDER '{areaPath}' AND [System.WorkItemType] IN ('Epic','Feature','User Story') ORDER BY [System.Id]";
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

    public async Task UpdateWorkItemStateAsync(int id, string state)
    {
        var config = GetValidatedConfig();
        ApplyAuthentication(config);

        var baseUri = BuildBaseUri(config);
        var patch = new[]
        {
            new { op = "add", path = "/fields/System.State", value = state }
        };
        var content = new StringContent(JsonSerializer.Serialize(patch));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json-patch+json");
        var request = new HttpRequestMessage(HttpMethod.Patch, $"{baseUri}/workitems/{id}?api-version={ApiVersion}")
        {
            Content = content
        };
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
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

