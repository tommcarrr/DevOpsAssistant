using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace DevOpsAssistant.Services;

public class DevOpsApiService
{
    private const string ApiVersion = "7.0";
    private readonly DevOpsConfigService _configService;

    private readonly HttpClient _httpClient;

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
            throw new InvalidOperationException("DevOps configuration is incomplete.");
        return config;
    }

    private void ApplyAuthentication(DevOpsConfig config)
    {
        var pat = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{config.PatToken}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", pat);
    }

    private static string BuildBaseUri(DevOpsConfig config)
    {
        return $"https://dev.azure.com/{config.Organization}/{config.Project}/_apis/wit";
    }

    private static string BuildItemUrlBase(DevOpsConfig config)
    {
        return $"https://dev.azure.com/{config.Organization}/{config.Project}/_workitems/edit/";
    }

    public async Task<List<WorkItemNode>> GetWorkItemHierarchyAsync(string areaPath)
    {
        var config = GetValidatedConfig();
        ApplyAuthentication(config);

        var baseUri = BuildBaseUri(config);
        var itemUrlBase = BuildItemUrlBase(config);

        var wiql = BuildWiql(areaPath);
        var wiqlResponse =
            await _httpClient.PostAsJsonAsync($"{baseUri}/wiql?api-version={ApiVersion}", new { query = wiql });
        wiqlResponse.EnsureSuccessStatusCode();
        var wiqlResult = await wiqlResponse.Content.ReadFromJsonAsync<WiqlResult>();
        if (wiqlResult == null || wiqlResult.WorkItems.Length == 0)
            return [];

        var ids = wiqlResult.WorkItems
            .Select(w => w.Id)
            .Distinct();

        var workItems = new List<WorkItem>();
        var fetchTasks = ids.Chunk(200)
            .Select(chunk =>
            {
                var idList = string.Join(',', chunk);
                return _httpClient.GetFromJsonAsync<WorkItemsResult>(
                    $"{baseUri}/workitems?ids={idList}&$expand=relations&api-version={ApiVersion}");
            })
            .ToArray();

        var results = await Task.WhenAll(fetchTasks);
        foreach (var itemsResult in results)
            if (itemsResult?.Value != null)
                workItems.AddRange(itemsResult.Value);
        if (!workItems.Any())
            return [];

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
                    parent.Children.Add(child);
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

        var result =
            await _httpClient.GetFromJsonAsync<JsonElement>(
                $"{baseUri}/classificationnodes/areas?$depth=2&api-version={ApiVersion}");
        var list = new List<string>();
        if (result.TryGetProperty("children", out var children))
            foreach (var child in children.EnumerateArray())
                ExtractPaths(child, list);

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
        var wiqlResponse =
            await _httpClient.PostAsJsonAsync($"{baseUri}/wiql?api-version={ApiVersion}", new { query = wiql });
        wiqlResponse.EnsureSuccessStatusCode();
        var wiqlResult = await wiqlResponse.Content.ReadFromJsonAsync<WiqlResult>();
        if (wiqlResult == null || wiqlResult.WorkItems.Length == 0)
            return [];

        var ids = wiqlResult.WorkItems.Select(w => w.Id).Distinct();
        var workItems = new List<WorkItem>();
        var fetchTasks = ids.Chunk(200).Select(chunk =>
        {
            var idList = string.Join(',', chunk);
            return _httpClient.GetFromJsonAsync<WorkItemsResult>(
                $"{baseUri}/workitems?ids={idList}&$expand=relations&api-version={ApiVersion}");
        }).ToArray();

        var results = await Task.WhenAll(fetchTasks);
        foreach (var itemsResult in results)
            if (itemsResult?.Value != null)
                workItems.AddRange(itemsResult.Value);
        if (!workItems.Any())
            return [];

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
                HasDescription = w.Fields.TryGetValue("System.Description", out var desc) &&
                                 !string.IsNullOrWhiteSpace(desc.GetString()),
                HasStoryPoints = w.Fields.TryGetValue("Microsoft.VSTS.Scheduling.StoryPoints", out var sp) &&
                                 (sp.ValueKind == JsonValueKind.Number || !string.IsNullOrWhiteSpace(sp.ToString())),
                HasAcceptanceCriteria = w.Fields.TryGetValue("Microsoft.VSTS.Common.AcceptanceCriteria", out var ac) &&
                                        !string.IsNullOrWhiteSpace(ac.GetString()),
                HasAssignee = w.Fields.TryGetValue("System.AssignedTo.displayName", out var at) &&
                              !string.IsNullOrWhiteSpace(at.GetString()),
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
            foreach (var child in children.EnumerateArray())
                ExtractPaths(child, list);
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
            areaPath = string.Join('\\', new[] { segments[0] }.Concat(segments.Skip(2)));
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
        return
            $"SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = @project AND [System.AreaPath] UNDER '{areaPath}' AND [System.State] IN ('New', 'Active') AND [System.WorkItemType] IN ('Epic','Feature','User Story') ORDER BY [System.Id]";
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

        var allClosed = node.Children.All(c =>
            c.Info.State.Equals("Closed", StringComparison.OrdinalIgnoreCase) ||
            c.Info.State.Equals("Removed", StringComparison.OrdinalIgnoreCase) ||
            c.Info.State.Equals("Done", StringComparison.OrdinalIgnoreCase));

        var anyNotNew = node.Children.Any(c => !c.Info.State.Equals("New", StringComparison.OrdinalIgnoreCase));

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

    public async Task<List<WorkItemInfo>> SearchUserStoriesAsync(string term)
    {
        var config = GetValidatedConfig();
        ApplyAuthentication(config);

        var baseUri = BuildBaseUri(config);
        var itemUrlBase = BuildItemUrlBase(config);

        var wiql = BuildStorySearchWiql(term);
        var wiqlResponse =
            await _httpClient.PostAsJsonAsync($"{baseUri}/wiql?api-version={ApiVersion}", new { query = wiql });
        wiqlResponse.EnsureSuccessStatusCode();
        var wiqlResult = await wiqlResponse.Content.ReadFromJsonAsync<WiqlResult>();
        if (wiqlResult?.WorkItems == null || wiqlResult.WorkItems.Length == 0)
            return [];

        var ids = wiqlResult.WorkItems.Select(w => w.Id).Take(20).ToArray();
        var idList = string.Join(',', ids);
        var itemsResult =
            await _httpClient.GetFromJsonAsync<WorkItemsResult>(
                $"{baseUri}/workitems?ids={idList}&api-version={ApiVersion}");
        var list = new List<WorkItemInfo>();
        if (itemsResult?.Value != null)
        {
            var dict = new Dictionary<int, WorkItem>();
            foreach (var w in itemsResult.Value) dict[w.Id] = w;

            foreach (var id in ids)
            {
                if (!dict.TryGetValue(id, out var w))
                    continue;

                list.Add(new WorkItemInfo
                {
                    Id = w.Id,
                    Title = w.Fields["System.Title"].GetString() ?? string.Empty,
                    State = w.Fields["System.State"].GetString() ?? string.Empty,
                    WorkItemType = w.Fields["System.WorkItemType"].GetString() ?? string.Empty,
                    Url = $"{itemUrlBase}{w.Id}"
                });
            }
        }

        return list;
    }

    public async Task<List<StoryHierarchyDetails>> GetStoryHierarchyDetailsAsync(IEnumerable<int> storyIds)
    {
        var idsToFetch = new HashSet<int>(storyIds);
        var fetched = new HashSet<int>();
        var items = new Dictionary<int, WorkItem>();

        var config = GetValidatedConfig();
        ApplyAuthentication(config);
        var baseUri = BuildBaseUri(config);
        var itemUrlBase = BuildItemUrlBase(config);

        while (idsToFetch.Except(fetched).Any())
        {
            var batch = idsToFetch.Except(fetched).Take(200).ToArray();
            fetched.UnionWith(batch);
            var idList = string.Join(',', batch);
            var result =
                await _httpClient.GetFromJsonAsync<WorkItemsResult>(
                    $"{baseUri}/workitems?ids={idList}&$expand=relations&api-version={ApiVersion}");
            if (result?.Value == null) continue;
            foreach (var w in result.Value)
                if (items.TryAdd(w.Id, w))
                {
                    if (w.Relations == null) continue;
                    foreach (var rel in w.Relations.Where(r => r.Rel == "System.LinkTypes.Hierarchy-Reverse"))
                        if (int.TryParse(rel.Url.Split('/').Last(), out var parentId))
                            idsToFetch.Add(parentId);
                }
        }

        var list = new List<StoryHierarchyDetails>();
        foreach (var id in storyIds)
        {
            if (!items.TryGetValue(id, out var story))
                continue;

            var storyInfo = new WorkItemInfo
            {
                Id = story.Id,
                Title = story.Fields["System.Title"].GetString() ?? string.Empty,
                State = story.Fields["System.State"].GetString() ?? string.Empty,
                WorkItemType = story.Fields["System.WorkItemType"].GetString() ?? string.Empty,
                Url = $"{itemUrlBase}{story.Id}"
            };
            var desc = story.Fields.TryGetValue("System.Description", out var d)
                ? d.GetString() ?? string.Empty
                : string.Empty;

            WorkItemInfo? featureInfo = null;
            WorkItemInfo? epicInfo = null;
            var featureId = story.Relations?.FirstOrDefault(r => r.Rel == "System.LinkTypes.Hierarchy-Reverse")?.Url
                .Split('/').Last();
            if (featureId != null && int.TryParse(featureId, out var fid) && items.TryGetValue(fid, out var feature))
            {
                featureInfo = new WorkItemInfo
                {
                    Id = feature.Id,
                    Title = feature.Fields["System.Title"].GetString() ?? string.Empty,
                    State = feature.Fields["System.State"].GetString() ?? string.Empty,
                    WorkItemType = feature.Fields["System.WorkItemType"].GetString() ?? string.Empty,
                    Url = $"{itemUrlBase}{feature.Id}"
                };
                var epicId = feature.Relations?.FirstOrDefault(r => r.Rel == "System.LinkTypes.Hierarchy-Reverse")?.Url
                    .Split('/').Last();
                if (epicId != null && int.TryParse(epicId, out var eid) && items.TryGetValue(eid, out var epic))
                    epicInfo = new WorkItemInfo
                    {
                        Id = epic.Id,
                        Title = epic.Fields["System.Title"].GetString() ?? string.Empty,
                        State = epic.Fields["System.State"].GetString() ?? string.Empty,
                        WorkItemType = epic.Fields["System.WorkItemType"].GetString() ?? string.Empty,
                        Url = $"{itemUrlBase}{epic.Id}"
                    };
            }

            list.Add(new StoryHierarchyDetails
            {
                Story = storyInfo,
                Description = desc,
                Feature = featureInfo,
                Epic = epicInfo
            });
        }

        return list;
    }

    public async Task<List<StoryMetric>> GetStoryMetricsAsync(string areaPath)
    {
        var config = GetValidatedConfig();
        ApplyAuthentication(config);

        var baseUri = BuildBaseUri(config);

        var wiql = BuildMetricsWiql(areaPath);
        var wiqlResponse =
            await _httpClient.PostAsJsonAsync($"{baseUri}/wiql?api-version={ApiVersion}", new { query = wiql });
        wiqlResponse.EnsureSuccessStatusCode();
        var wiqlResult = await wiqlResponse.Content.ReadFromJsonAsync<WiqlResult>();
        if (wiqlResult == null || wiqlResult.WorkItems.Length == 0)
            return [];

        var ids = wiqlResult.WorkItems.Select(w => w.Id).Distinct();
        var workItems = new List<WorkItem>();
        var fetchTasks = ids.Chunk(200)
            .Select(chunk =>
            {
                var idList = string.Join(',', chunk);
                return _httpClient.GetFromJsonAsync<WorkItemsResult>(
                    $"{baseUri}/workitems?ids={idList}&fields=System.CreatedDate,Microsoft.VSTS.Common.ActivatedDate,Microsoft.VSTS.Common.ClosedDate&api-version={ApiVersion}");
            })
            .ToArray();

        var results = await Task.WhenAll(fetchTasks);
        foreach (var itemsResult in results)
            if (itemsResult?.Value != null)
                workItems.AddRange(itemsResult.Value);

        var list = new List<StoryMetric>();
        foreach (var w in workItems)
        {
            if (!w.Fields.TryGetValue("Microsoft.VSTS.Common.ClosedDate", out var cd) || cd.ValueKind != JsonValueKind.String)
                continue;

            var closed = cd.GetDateTime();
            var created = w.Fields.TryGetValue("System.CreatedDate", out var cr) && cr.ValueKind == JsonValueKind.String
                ? cr.GetDateTime()
                : closed;
            var activated = w.Fields.TryGetValue("Microsoft.VSTS.Common.ActivatedDate", out var ad) && ad.ValueKind == JsonValueKind.String
                ? ad.GetDateTime()
                : created;

            list.Add(new StoryMetric
            {
                Id = w.Id,
                CreatedDate = created,
                ActivatedDate = activated,
                ClosedDate = closed
            });
        }

        return list;
    }

    private static string BuildMetricsWiql(string areaPath)
    {
        areaPath = NormalizeAreaPath(areaPath);
        return $"SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = @project AND [System.AreaPath] UNDER '{areaPath}' AND [System.WorkItemType] = 'User Story' AND [Microsoft.VSTS.Common.ClosedDate] >= @today - 84 ORDER BY [Microsoft.VSTS.Common.ClosedDate]";
    }

    private static string BuildStorySearchWiql(string term)
    {
        term = term.Replace("'", "''");
        return
            $"SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = @project AND [System.WorkItemType] = 'User Story' AND [System.Title] CONTAINS '{term}' ORDER BY [System.ChangedDate] DESC";
    }

    private class WiqlResult
    {
        public WorkItemRef[] WorkItems { get; set; } = [];
    }

    private class WorkItemRef
    {
        public int Id { get; set; }
    }

    private class WorkItemsResult
    {
        public WorkItem[] Value { get; set; } = [];
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