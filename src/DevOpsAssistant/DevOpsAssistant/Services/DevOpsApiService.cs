using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Linq;
using System.Text.Json;

namespace DevOpsAssistant.Services;

public class DevOpsApiService
{
    private const string ApiVersion = "7.0";
    private readonly DevOpsConfigService _configService;

    private readonly HttpClient _httpClient;
    private readonly DeploymentConfigService _deploymentConfig;

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    private string ApiBaseUrl => string.IsNullOrEmpty(_deploymentConfig.Config.DevOpsApiBaseUrl)
        ? "https://dev.azure.com"
        : _deploymentConfig.Config.DevOpsApiBaseUrl!.TrimEnd('/');

    private string SearchBaseUrl
    {
        get
        {
            var url = _deploymentConfig.Config.DevOpsSearchApiBaseUrl;
            if (!string.IsNullOrEmpty(url))
                return url.TrimEnd('/');

            var baseUrl = ApiBaseUrl;
            return baseUrl.Contains("dev.azure.com")
                ? baseUrl.Replace("dev.azure.com", "almsearch.dev.azure.com")
                : baseUrl;
        }
    }

    private string? StaticApiPath => _deploymentConfig.Config.StaticApiPath;

    public DevOpsApiService(HttpClient httpClient, DevOpsConfigService configService, DeploymentConfigService deploymentConfig)
    {
        _httpClient = httpClient;
        _configService = configService;
        _deploymentConfig = deploymentConfig;
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

    private string BuildBaseUri(DevOpsConfig config)
    {
        return $"{ApiBaseUrl}/{config.Organization}/{config.Project}/_apis/wit";
    }

    private string BuildItemUrlBase(DevOpsConfig config)
    {
        return $"{ApiBaseUrl}/{config.Organization}/{config.Project}/_workitems/edit/";
    }

    private async Task<T?> GetJsonAsync<T>(string url)
    {
        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            await HandleError(response);
        return await response.Content.ReadFromJsonAsync<T>();
    }

    private async Task<T?> PostJsonAsync<T>(string url, object body)
    {
        var response = await _httpClient.PostAsJsonAsync(url, body);
        if (!response.IsSuccessStatusCode)
            await HandleError(response);
        return await response.Content.ReadFromJsonAsync<T>();
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            await HandleError(response);
        return response;
    }

    private static async Task HandleError(HttpResponseMessage response)
    {
        string message = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Authentication failed. Please verify your PAT token.",
            HttpStatusCode.Forbidden => "Access denied. Please check your PAT permissions.",
            HttpStatusCode.NotFound => "The requested project was not found.",
            _ => string.Empty
        };

        var content = await response.Content.ReadAsStringAsync();
        try
        {
            var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("message", out var msg))
            {
                var detail = msg.GetString();
                if (!string.IsNullOrWhiteSpace(detail))
                    message = string.IsNullOrWhiteSpace(message) ? detail : $"{message} ({detail})";
            }
        }
        catch
        {
            // ignore JSON parse errors
        }

        if (string.IsNullOrWhiteSpace(message))
            message = $"Request failed with status code {(int)response.StatusCode} ({response.ReasonPhrase})";

        throw new InvalidOperationException(message);
    }

    public async Task<List<WorkItemNode>> GetWorkItemHierarchyAsync(string areaPath)
    {
        var config = GetValidatedConfig();
        ApplyAuthentication(config);

        var baseUri = BuildBaseUri(config);
        var itemUrlBase = BuildItemUrlBase(config);

        var wiql = BuildEpicsWiql(areaPath);
        var wiqlResult = await PostJsonAsync<WiqlResult>($"{baseUri}/wiql?api-version={ApiVersion}", new { query = wiql });
        if (wiqlResult == null || wiqlResult.WorkItems.Length == 0)
            return [];

        var ids = wiqlResult.WorkItems
            .Select(w => w.Id)
            .Distinct();

        var items = await FetchHierarchyAsync(ids, baseUri);
        if (items.Count == 0)
            return [];

        var nodes = new Dictionary<int, WorkItemNode>();
        foreach (var w in items.Values)
        {
            var state = w.Fields["System.State"].GetString() ?? string.Empty;
            var type = w.Fields["System.WorkItemType"].GetString() ?? string.Empty;
            if ((type.Equals("Task", StringComparison.OrdinalIgnoreCase) || type.Equals("Bug", StringComparison.OrdinalIgnoreCase)) &&
                (state.Equals("Closed", StringComparison.OrdinalIgnoreCase) || state.Equals("Removed", StringComparison.OrdinalIgnoreCase)))
                continue;

            nodes[w.Id] = new WorkItemNode
            {
                Info = new WorkItemInfo
                {
                    Id = w.Id,
                    Title = w.Fields["System.Title"].GetString() ?? string.Empty,
                    State = state,
                    WorkItemType = type,
                    Url = $"{itemUrlBase}{w.Id}"
                }
            };
        }

        foreach (var item in items.Values)
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
            WorkItemHelpers.ComputeStatus(root);
        return roots;
    }

    public async Task<string[]> GetBacklogsAsync()
    {
        var config = GetValidatedConfig();
        ApplyAuthentication(config);

        var baseUri = BuildBaseUri(config);

        var result =
            await GetJsonAsync<JsonElement>(
                $"{baseUri}/classificationnodes/areas?$depth=2&api-version={ApiVersion}");
        List<string> list = [];
        if (result.TryGetProperty("path", out var rootPath))
        {
            var p = rootPath.GetString();
            if (!string.IsNullOrEmpty(p))
                list.Add(p);
        }
        if (result.TryGetProperty("children", out var children))
            foreach (var child in children.EnumerateArray())
                ExtractPaths(child, list);

        var normalized = list.Select(NormalizeAreaPath).ToArray();
        return normalized;
    }

    public async Task<string[]> GetStatesAsync()
    {
        var config = GetValidatedConfig();
        ApplyAuthentication(config);

        var baseUri = BuildBaseUri(config);
        var result =
            await GetJsonAsync<JsonElement>(
                $"{baseUri}/workitemtypes/User%20Story/states?api-version={ApiVersion}");
        List<string> list = [];
        if (result.TryGetProperty("value", out var values))
            foreach (var s in values.EnumerateArray())
                if (s.TryGetProperty("name", out var name))
                {
                    var n = name.GetString();
                    if (!string.IsNullOrWhiteSpace(n))
                        list.Add(n);
                }
        return list.ToArray();
    }

    public async Task<List<WorkItemDetails>> GetValidationItemsAsync(string areaPath, IEnumerable<string> states)
    {
        var config = GetValidatedConfig();
        ApplyAuthentication(config);

        var baseUri = BuildBaseUri(config);
        var itemUrlBase = BuildItemUrlBase(config);

        var wiql = BuildValidationWiql(areaPath, states);
        var wiqlResult = await PostJsonAsync<WiqlResult>($"{baseUri}/wiql?api-version={ApiVersion}", new { query = wiql });
        if (wiqlResult == null || wiqlResult.WorkItems.Length == 0)
            return [];

        var ids = wiqlResult.WorkItems.Select(w => w.Id).Distinct();
        List<WorkItem> workItems = [];
        var fetchTasks = ids.Chunk(200).Select(chunk =>
        {
            var idList = string.Join(',', chunk);
            return GetJsonAsync<WorkItemsResult>(
                $"{baseUri}/workitems?ids={idList}&$expand=relations&api-version={ApiVersion}");
        }).ToArray();

        var results = await Task.WhenAll(fetchTasks);
        foreach (var itemsResult in results)
            if (itemsResult?.Value != null)
                workItems.AddRange(itemsResult.Value);
        if (!workItems.Any())
            return [];

        List<WorkItemDetails> list = [];
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

    public async Task<List<StoryHierarchyDetails>> GetStoriesAsync(string areaPath, IEnumerable<string> states)
    {
        var config = GetValidatedConfig();
        ApplyAuthentication(config);

        var baseUri = BuildBaseUri(config);

        var wiql = BuildStoriesWiql(areaPath, states);
        var wiqlResult = await PostJsonAsync<WiqlResult>($"{baseUri}/wiql?api-version={ApiVersion}", new { query = wiql });
        if (wiqlResult == null || wiqlResult.WorkItems.Length == 0)
            return [];

        var ids = wiqlResult.WorkItems.Select(w => w.Id).Distinct();
        return await GetStoryHierarchyDetailsAsync(ids);
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

    private static string BuildEpicsWiql(string areaPath)
    {
        areaPath = NormalizeAreaPath(areaPath);
        List<string> conditions =
        [
            "[System.TeamProject] = @project",
            $"[System.AreaPath] UNDER '{areaPath}'",
            "[System.WorkItemType] = 'Epic'",
            "[System.State] <> 'Closed'",
            "[System.State] <> 'Removed'"
        ];

        var where = string.Join(" AND ", conditions);
        return $"SELECT [System.Id] FROM WorkItems WHERE {where} ORDER BY [System.Id]";
    }

    private async Task<Dictionary<int, WorkItem>> FetchHierarchyAsync(IEnumerable<int> rootIds, string baseUri)
    {
        var idsToFetch = new HashSet<int>(rootIds);
        var fetched = new HashSet<int>();
        var items = new Dictionary<int, WorkItem>();

        while (true)
        {
            var batch = idsToFetch.Except(fetched).Take(200).ToArray();
            if (batch.Length == 0)
                break;
            fetched.UnionWith(batch);
            var idList = string.Join(',', batch);
            var result = await GetJsonAsync<WorkItemsResult>($"{baseUri}/workitems?ids={idList}&$expand=relations&api-version={ApiVersion}");
            if (result?.Value == null) continue;
            foreach (var w in result.Value)
                if (items.TryAdd(w.Id, w))
                {
                    if (w.Relations == null) continue;
                    foreach (var rel in w.Relations.Where(r => r.Rel == "System.LinkTypes.Hierarchy-Forward"))
                        if (int.TryParse(rel.Url.Split('/').Last(), out var childId))
                            idsToFetch.Add(childId);
                }
        }

        return items;
    }

    private static string BuildValidationWiql(string areaPath, IEnumerable<string> states)
    {
        areaPath = NormalizeAreaPath(areaPath);
        var stateList = string.Join(", ", states.Select(s => $"'{s.Replace("'", "''")}'"));
        var stateCondition = string.IsNullOrWhiteSpace(stateList) ? string.Empty : $" AND [System.State] IN ({stateList})";
        return
            $"SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = @project AND [System.AreaPath] UNDER '{areaPath}'{stateCondition} AND [System.WorkItemType] IN ('Epic','Feature','User Story') ORDER BY [System.Id]";
    }

    private static string BuildStoriesWiql(string areaPath, IEnumerable<string> states)
    {
        areaPath = NormalizeAreaPath(areaPath);
        var stateList = string.Join(", ", states.Select(s => $"'{s.Replace("'", "''")}'"));
        var stateCondition = string.IsNullOrWhiteSpace(stateList) ? string.Empty : $" AND [System.State] IN ({stateList})";
        return
            $"SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = @project AND [System.AreaPath] UNDER '{areaPath}' AND [System.WorkItemType] = 'User Story'{stateCondition} ORDER BY [System.Id]";
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
        await SendAsync(request);
    }

    public async Task<List<WorkItemInfo>> SearchUserStoriesAsync(string term)
    {
        var config = GetValidatedConfig();
        ApplyAuthentication(config);

        var baseUri = BuildBaseUri(config);
        var itemUrlBase = BuildItemUrlBase(config);

        var wiql = BuildStorySearchWiql(term);
        var wiqlResult = await PostJsonAsync<WiqlResult>($"{baseUri}/wiql?api-version={ApiVersion}", new { query = wiql });
        if (wiqlResult?.WorkItems == null || wiqlResult.WorkItems.Length == 0)
            return [];

        var ids = wiqlResult.WorkItems.Select(w => w.Id).Take(20).ToArray();
        var idList = string.Join(',', ids);
        var itemsResult =
            await GetJsonAsync<WorkItemsResult>($"{baseUri}/workitems?ids={idList}&api-version={ApiVersion}");
        List<WorkItemInfo> list = [];
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

    public async Task<List<WorkItemInfo>> SearchReleaseItemsAsync(string term)
    {
        var config = GetValidatedConfig();
        ApplyAuthentication(config);

        var baseUri = BuildBaseUri(config);
        var itemUrlBase = BuildItemUrlBase(config);

        var wiql = BuildReleaseSearchWiql(term);
        var wiqlResult = await PostJsonAsync<WiqlResult>($"{baseUri}/wiql?api-version={ApiVersion}", new { query = wiql });
        if (wiqlResult?.WorkItems == null || wiqlResult.WorkItems.Length == 0)
            return [];

        var ids = wiqlResult.WorkItems.Select(w => w.Id).Take(20).ToArray();
        var idList = string.Join(',', ids);
        var itemsResult = await GetJsonAsync<WorkItemsResult>($"{baseUri}/workitems?ids={idList}&api-version={ApiVersion}");
        List<WorkItemInfo> list = [];
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

        while (true)
        {
            var batch = idsToFetch.Except(fetched).Take(200).ToArray();
            if (batch.Length == 0)
                break;
            fetched.UnionWith(batch);
            var idList = string.Join(',', batch);
            var result =
                await GetJsonAsync<WorkItemsResult>(
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

        List<StoryHierarchyDetails> list = [];
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
            var type = storyInfo.WorkItemType;
            string desc = string.Empty;
            string repro = string.Empty;
            string sysInfo = string.Empty;
            string acceptance = string.Empty;
            if (type.Equals("Bug", StringComparison.OrdinalIgnoreCase))
            {
                repro = story.Fields.TryGetValue("Microsoft.VSTS.TCM.ReproSteps", out var rs)
                    ? rs.GetString() ?? string.Empty
                    : string.Empty;
                sysInfo = story.Fields.TryGetValue("Microsoft.VSTS.TCM.SystemInfo", out var si)
                    ? si.GetString() ?? string.Empty
                    : string.Empty;
            }
            else
            {
                desc = story.Fields.TryGetValue("System.Description", out var d)
                    ? d.GetString() ?? string.Empty
                    : string.Empty;
                acceptance = story.Fields.TryGetValue("Microsoft.VSTS.Common.AcceptanceCriteria", out var ac)
                    ? ac.GetString() ?? string.Empty
                    : string.Empty;
            }

            WorkItemInfo? featureInfo = null;
            WorkItemInfo? epicInfo = null;
            string featureDesc = string.Empty;
            string epicDesc = string.Empty;
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
                featureDesc = feature.Fields.TryGetValue("System.Description", out var fd)
                    ? fd.GetString() ?? string.Empty
                    : string.Empty;
                var epicId = feature.Relations?.FirstOrDefault(r => r.Rel == "System.LinkTypes.Hierarchy-Reverse")?.Url
                    .Split('/').Last();
                if (epicId != null && int.TryParse(epicId, out var eid) && items.TryGetValue(eid, out var epic))
                {
                    epicInfo = new WorkItemInfo
                    {
                        Id = epic.Id,
                        Title = epic.Fields["System.Title"].GetString() ?? string.Empty,
                        State = epic.Fields["System.State"].GetString() ?? string.Empty,
                        WorkItemType = epic.Fields["System.WorkItemType"].GetString() ?? string.Empty,
                        Url = $"{itemUrlBase}{epic.Id}"
                    };
                    epicDesc = epic.Fields.TryGetValue("System.Description", out var ed)
                        ? ed.GetString() ?? string.Empty
                        : string.Empty;
                }
            }

            list.Add(new StoryHierarchyDetails
            {
                Story = storyInfo,
                Description = desc,
                ReproSteps = repro,
                SystemInfo = sysInfo,
                AcceptanceCriteria = acceptance,
                Feature = featureInfo,
                Epic = epicInfo,
                FeatureDescription = featureDesc,
                EpicDescription = epicDesc
            });
        }

        return list;
    }

    public async Task<List<StoryMetric>> GetStoryMetricsAsync(string areaPath, DateTime? startDate = null)
    {
        var config = GetValidatedConfig();
        ApplyAuthentication(config);

        var baseUri = BuildBaseUri(config);

        var wiql = BuildMetricsWiql(areaPath, startDate ?? DateTime.Today.AddDays(-84));
        var wiqlResult = await PostJsonAsync<WiqlResult>($"{baseUri}/wiql?api-version={ApiVersion}", new { query = wiql });
        if (wiqlResult == null || wiqlResult.WorkItems.Length == 0)
            return [];

        var ids = wiqlResult.WorkItems.Select(w => w.Id).Distinct();
        List<WorkItem> workItems = [];
        var fetchTasks = ids.Chunk(200)
            .Select(chunk =>
            {
                var idList = string.Join(',', chunk);
                return GetJsonAsync<WorkItemsResult>($"{baseUri}/workitems?ids={idList}&fields=System.CreatedDate,Microsoft.VSTS.Common.ActivatedDate,Microsoft.VSTS.Common.ClosedDate,Microsoft.VSTS.Scheduling.StoryPoints,Microsoft.VSTS.Scheduling.OriginalEstimate&api-version={ApiVersion}");
            })
            .ToArray();

        var results = await Task.WhenAll(fetchTasks);
        foreach (var itemsResult in results)
            if (itemsResult?.Value != null)
                workItems.AddRange(itemsResult.Value);

        List<StoryMetric> list = [];
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

            var storyPoints = w.Fields.TryGetValue("Microsoft.VSTS.Scheduling.StoryPoints", out var sp) && sp.ValueKind == JsonValueKind.Number
                ? sp.GetDouble()
                : 0;
            var originalEstimate = w.Fields.TryGetValue("Microsoft.VSTS.Scheduling.OriginalEstimate", out var oe) && oe.ValueKind == JsonValueKind.Number
                ? oe.GetDouble()
                : 0;

            list.Add(new StoryMetric
            {
                Id = w.Id,
                CreatedDate = created,
                ActivatedDate = activated,
                ClosedDate = closed,
                StoryPoints = storyPoints,
                OriginalEstimate = originalEstimate
            });
        }

        return list;
    }

    private static string BuildMetricsWiql(string areaPath, DateTime startDate)
    {
        areaPath = NormalizeAreaPath(areaPath);
        var start = startDate.ToString("yyyy-MM-dd");
        return
            $"SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = @project AND [System.AreaPath] UNDER '{areaPath}' AND [System.WorkItemType] = 'User Story' AND [Microsoft.VSTS.Common.ClosedDate] >= '{start}' ORDER BY [Microsoft.VSTS.Common.ClosedDate]";
    }

    private static string BuildStorySearchWiql(string term)
    {
        term = term.Replace("'", "''");
        return
            $"SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = @project AND [System.WorkItemType] = 'User Story' AND [System.Title] CONTAINS '{term}' ORDER BY [System.ChangedDate] DESC";
    }

    private static string BuildReleaseSearchWiql(string term)
    {
        term = term.Replace("'", "''");
        return
            $"SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = @project AND [System.WorkItemType] IN ('User Story','Bug') AND [System.Title] CONTAINS '{term}' ORDER BY [System.ChangedDate] DESC";
    }

    public async Task<List<WikiSearchResult>> SearchWikiPagesAsync(string term)
    {
        if (StaticApiPath != null)
        {
            var staticResult = await _httpClient.GetFromJsonAsync<WikiSearchResults>($"{StaticApiPath}/wiki-search.json");
            return staticResult?.Results.Select(r => new WikiSearchResult
            {
                WikiId = r.Wiki.Id,
                Path = r.Path ?? string.Empty,
                Id = r.Id ?? string.Empty,
                Url = r.Url ?? string.Empty
            }).ToList() ?? [];
        }

        var config = GetValidatedConfig();
        ApplyAuthentication(config);

        var baseUrl = SearchBaseUrl;
        var url =
            $"{baseUrl}/{config.Organization}/{config.Project}/_apis/search/wikisearchresults?api-version=7.1";
        var apiResult = await PostJsonAsync<WikiSearchResults>(url, new { searchText = term });
        return apiResult?.Results.Select(r => new WikiSearchResult
        {
            WikiId = r.Wiki.Id,
            Path = r.Path ?? string.Empty,
            Id = r.Id ?? string.Empty,
            Url = r.Url ?? string.Empty
        }).ToList() ?? [];
    }

    public async Task<string> GetWikiPageContentAsync(string wikiId, string path)
    {
        if (StaticApiPath != null)
        {
            var staticResult = await _httpClient.GetFromJsonAsync<WikiPageContentResult>($"{StaticApiPath}/wiki-page.json");
            return staticResult?.Content ?? string.Empty;
        }

        var config = GetValidatedConfig();
        ApplyAuthentication(config);

        path = Uri.EscapeDataString(path);
        var url =
            $"{ApiBaseUrl}/{config.Organization}/{config.Project}/_apis/wiki/wikis/{wikiId}/pages?path={path}&includeContent=true&api-version=7.1-preview.1";
        var result = await GetJsonAsync<WikiPageContentResult>(url);
        return result?.Content ?? string.Empty;
    }

    public async Task<List<WikiInfo>> GetWikisAsync()
    {
        if (StaticApiPath != null)
        {
            var staticResult = await _httpClient.GetFromJsonAsync<WikisResult>($"{StaticApiPath}/wikis.json");
            return staticResult?.Value.Select(w => new WikiInfo
            {
                Id = w.Id ?? string.Empty,
                Name = w.Name ?? string.Empty
            }).ToList() ?? [];
        }

        var config = GetValidatedConfig();
        ApplyAuthentication(config);

        var url = $"{ApiBaseUrl}/{config.Organization}/{config.Project}/_apis/wiki/wikis?api-version=7.1-preview.1";
        var result = await GetJsonAsync<WikisResult>(url);
        return result?.Value.Select(w => new WikiInfo
        {
            Id = w.Id ?? string.Empty,
            Name = w.Name ?? string.Empty
        }).ToList() ?? [];
    }

    public async Task<WikiPageNode?> GetWikiPageTreeAsync(string wikiId)
    {
        if (StaticApiPath != null)
        {
            var staticDoc = await _httpClient.GetFromJsonAsync<JsonElement>($"{StaticApiPath}/wiki-tree.json");
            return ParseWikiTreeJson(staticDoc);
        }

        var config = GetValidatedConfig();
        ApplyAuthentication(config);

        var url = $"{ApiBaseUrl}/{config.Organization}/{config.Project}/_apis/wiki/wikis/{wikiId}/pages?recursionLevel=Full&api-version=7.1-preview.1";
        var apiDoc = await GetJsonAsync<JsonElement>(url);
        return ParseWikiTreeJson(apiDoc);
    }

    private static WikiPageNode? ParseWikiTreeJson(JsonElement doc)
    {
        if (doc.ValueKind != JsonValueKind.Object)
            return null;

        var page = doc.Deserialize<WikiPage>(_jsonOptions);
        return page != null ? ParseWikiPage(page) : null;
    }

    private static WikiPageNode ParseWikiPage(WikiPage page)
    {
        var node = new WikiPageNode
        {
            Path = page.Path ?? string.Empty
        };
        if (page.SubPages != null)
        {
            foreach (var child in page.SubPages)
                node.Children.Add(ParseWikiPage(child));
        }
        return node;
    }

    public async Task<List<RepositoryInfo>> GetRepositoriesAsync()
    {
        var config = GetValidatedConfig();
        ApplyAuthentication(config);

        var url = $"{ApiBaseUrl}/{config.Organization}/{config.Project}/_apis/git/repositories?api-version=7.1";
        var result = await GetJsonAsync<ReposResult>(url);
        return result?.Value.Select(r => new RepositoryInfo
        {
            Id = r.Id ?? string.Empty,
            Name = r.Name ?? string.Empty,
            DefaultBranch = r.DefaultBranch ?? string.Empty
        }).ToList() ?? [];
    }

    public async Task<List<BranchInfo>> GetBranchesAsync(string repositoryId, string? baseBranch = null)
    {
        var config = GetValidatedConfig();
        ApplyAuthentication(config);

        var url = $"{ApiBaseUrl}/{config.Organization}/{config.Project}/_apis/git/repositories/{repositoryId}/stats/branches?api-version=7.1";
        if (!string.IsNullOrWhiteSpace(baseBranch))
            url += "&baseVersion=GB" + Uri.EscapeDataString(baseBranch);
        var result = await GetJsonAsync<BranchStatsResult>(url);
        return result?.Value.Select(b => new BranchInfo
        {
            Name = (b.Name ?? string.Empty).Replace("refs/heads/", string.Empty),
            CommitDate = b.Commit.Committer.Date,
            Ahead = b.AheadCount,
            Behind = b.BehindCount
        }).ToList() ?? [];
    }

    public async Task<int> CreateWorkItemAsync(
        string type,
        string title,
        string description,
        string areaPath,
        int? parentId = null,
        string? acceptanceCriteria = null,
        string[]? tags = null)
    {
        var config = GetValidatedConfig();
        ApplyAuthentication(config);

        var baseUri = BuildBaseUri(config);
        List<object> patches =
        [
            new { op = "add", path = "/fields/System.Title", value = title },
            new { op = "add", path = "/fields/System.Description", value = description },
            new { op = "add", path = "/fields/System.AreaPath", value = areaPath }
        ];
        if (!string.IsNullOrWhiteSpace(acceptanceCriteria))
            patches.Add(new { op = "add", path = "/fields/Microsoft.VSTS.Common.AcceptanceCriteria", value = acceptanceCriteria });
        if (tags != null && tags.Length > 0)
            patches.Add(new { op = "add", path = "/fields/System.Tags", value = string.Join(';', tags) });
        if (parentId.HasValue)
        {
            var parentUrl = $"{BuildItemUrlBase(config)}{parentId.Value}";
            patches.Add(new { op = "add", path = "/relations/-", value = new { rel = "System.LinkTypes.Hierarchy-Reverse", url = parentUrl } });
        }

        var content = new StringContent(JsonSerializer.Serialize(patches));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json-patch+json");
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"{baseUri}/workitems/${Uri.EscapeDataString(type)}?api-version={ApiVersion}")
        {
            Content = content
        };

        var response = await SendAsync(request);
        var doc = await response.Content.ReadFromJsonAsync<JsonElement>();
        return doc.GetProperty("id").GetInt32();
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

    private class WikisResult
    {
        public Wiki[] Value { get; set; } = [];
    }

    private class Wiki
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
    }

    private class WikiSearchResults
    {
        public WikiSearchItem[] Results { get; set; } = [];
    }

    private class WikiSearchItem
    {
        public string? Id { get; set; }
        public string? Path { get; set; }
        public string? Url { get; set; }
        public SearchWiki Wiki { get; set; } = new();
    }

    private class SearchWiki
    {
        public string Id { get; set; } = string.Empty;
    }

    private class WikiPageContentResult
    {
        public string? Content { get; set; }
    }

    private class WikiPage
    {
        public string? Path { get; set; }
        public int Order { get; set; }
        public bool IsParentPage { get; set; }
        public string? GitItemPath { get; set; }
        public WikiPage[]? SubPages { get; set; }
        public string? Url { get; set; }
        public string? RemoteUrl { get; set; }
        public string? Content { get; set; }
    }

    private class ReposResult
    {
        public Repo[] Value { get; set; } = [];
    }

    private class Repo
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? DefaultBranch { get; set; }
    }

    private class BranchStatsResult
    {
        public BranchStat[] Value { get; set; } = [];
    }

    private class BranchStat
    {
        public string? Name { get; set; }
        public CommitInfo Commit { get; set; } = new();
        public int AheadCount { get; set; }
        public int BehindCount { get; set; }
    }

    private class CommitInfo
    {
        public GitUser Committer { get; set; } = new();
    }

    private class GitUser
    {
        public DateTime Date { get; set; }
    }
}
