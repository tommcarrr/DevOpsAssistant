using System.Net;
using System.Text.Json;
using System.Reflection;
using DevOpsAssistant.Services;

namespace DevOpsAssistant.Tests;

public class DevOpsApiServiceTests
{
    private static string InvokeBuildEpicsWiql(string area)
    {
        var method = typeof(DevOpsApiService).GetMethod("BuildEpicsWiql", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (string)method.Invoke(null, [area])!;
    }

    private static string InvokeBuildValidationWiql(string area, IEnumerable<string> states)
    {
        var method =
            typeof(DevOpsApiService).GetMethod("BuildValidationWiql", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (string)method.Invoke(null, [area, states])!;
    }


    private static List<WorkItemNode> InvokeFilterClosedEpics(List<WorkItemNode> nodes)
    {
        var method =
            typeof(DevOpsApiService).GetMethod("FilterClosedEpics", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (List<WorkItemNode>)method.Invoke(null, [nodes])!;
    }

    private static string InvokeNormalizeAreaPath(string path)
    {
        var method =
            typeof(DevOpsApiService).GetMethod("NormalizeAreaPath", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (string)method.Invoke(null, [path])!;
    }

    private static void InvokeExtractPaths(JsonElement el, List<string> list)
    {
        var method = typeof(DevOpsApiService).GetMethod("ExtractPaths", BindingFlags.NonPublic | BindingFlags.Static)!;
        method.Invoke(null, [el, list]);
    }

    private static string InvokeBuildStorySearchWiql(string term)
    {
        var method =
            typeof(DevOpsApiService).GetMethod("BuildStorySearchWiql", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (string)method.Invoke(null, [term])!;
    }

    private static string InvokeBuildReleaseSearchWiql(string term)
    {
        var method =
            typeof(DevOpsApiService).GetMethod("BuildReleaseSearchWiql", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (string)method.Invoke(null, [term])!;
    }

    private static string InvokeBuildMetricsWiql(string area, DateTime start)
    {
        var method =
            typeof(DevOpsApiService).GetMethod("BuildMetricsWiql", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (string)method.Invoke(null, [area, start])!;
    }

    private static string InvokeBuildStoriesWiql(string area, string[] states)
    {
        var method =
            typeof(DevOpsApiService).GetMethod("BuildStoriesWiql", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (string)method.Invoke(null, [area, states])!;
    }

    [Fact]
    public void BuildEpicsWiql_Filters_Closed_Epics()
    {
        var query = InvokeBuildEpicsWiql("Area");

        Assert.Contains("[System.State] <> 'Closed'", query);
        Assert.Contains("[System.State] <> 'Removed'", query);
        Assert.Contains("[System.WorkItemType] = 'Epic'", query);
        Assert.DoesNotContain("System.Tags", query);
        Assert.Contains("[System.AreaPath] UNDER 'Area'", query);
    }

    [Fact]
    public void BuildEpicsWiql_Trims_Leading_Backslash()
    {
        var query = InvokeBuildEpicsWiql("\\Area");

        Assert.Contains("[System.AreaPath] UNDER 'Area'", query);
    }

    [Fact]
    public void BuildEpicsWiql_Removes_Area_Prefix()
    {
        var query = InvokeBuildEpicsWiql("Project\\Area\\Development");

        Assert.Contains("[System.AreaPath] UNDER 'Project\\Development'", query);
    }

    [Fact]
    public void BuildEpicsWiql_Selects_WorkItems()
    {
        var query = InvokeBuildEpicsWiql("Area");

        Assert.DoesNotContain("WorkItemLinks", query);
        Assert.DoesNotContain("System.Links.LinkType", query);
        Assert.Contains("FROM WorkItems", query);
    }

    [Fact]
    public void BuildValidationWiql_Selects_Epic_Feature_Story()
    {
        var query = InvokeBuildValidationWiql("Area", ["New", "Active"]);

        Assert.Contains("'Epic'", query);
        Assert.Contains("'Feature'", query);
        Assert.Contains("'User Story'", query);
        Assert.DoesNotContain("Task", query);
    }

    [Fact]
    public void ComputeStatus_Leaf_Node_ExpectedState_Equals_Current()
    {
        var node = new WorkItemNode { Info = new WorkItemInfo { State = "New" } };

        WorkItemHelpers.ComputeStatus(node);

        Assert.True(node.StatusValid);
        Assert.Equal("New", node.ExpectedState);
    }

    [Fact]
    public void ComputeStatus_Invalid_When_Child_Not_New()
    {
        var root = new WorkItemNode { Info = new WorkItemInfo { State = "New" } };
        root.Children.Add(new WorkItemNode { Info = new WorkItemInfo { State = "New" } });
        root.Children.Add(new WorkItemNode { Info = new WorkItemInfo { State = "Active" } });

        WorkItemHelpers.ComputeStatus(root);

        Assert.False(root.StatusValid);
        Assert.Equal("Active", root.ExpectedState);
    }

    [Fact]
    public void ComputeStatus_Valid_When_All_Children_Closed()
    {
        var root = new WorkItemNode { Info = new WorkItemInfo { State = "Closed" } };
        root.Children.Add(new WorkItemNode { Info = new WorkItemInfo { State = "Closed" } });
        root.Children.Add(new WorkItemNode { Info = new WorkItemInfo { State = "Removed" } });

        WorkItemHelpers.ComputeStatus(root);

        Assert.True(root.StatusValid);
        Assert.Equal("Closed", root.ExpectedState);
    }

    [Fact]
    public void ComputeStatus_Uses_Child_Expected_State()
    {
        var root = new WorkItemNode { Info = new WorkItemInfo { State = "New" } };
        var feature = new WorkItemNode { Info = new WorkItemInfo { State = "New" } };
        feature.Children.Add(new WorkItemNode { Info = new WorkItemInfo { State = "Closed" } });
        root.Children.Add(feature);

        WorkItemHelpers.ComputeStatus(root);

        Assert.Equal("Closed", feature.ExpectedState);
        Assert.Equal("Closed", root.ExpectedState);
        Assert.False(root.StatusValid);
    }

    [Fact]
    public void FilterClosedEpics_Removes_Closed_Epics()
    {
        var closed = new WorkItemNode { Info = new WorkItemInfo { WorkItemType = "Epic", State = "Closed" } };
        var open = new WorkItemNode { Info = new WorkItemInfo { WorkItemType = "Epic", State = "New" } };
        List<WorkItemNode> list = [closed, open];

        var result = InvokeFilterClosedEpics(list);

        Assert.DoesNotContain(closed, result);
        Assert.Contains(open, result);
    }

    [Fact]
    public async Task GetWorkItemHierarchyAsync_Throws_When_Config_Incomplete()
    {
        var configService = new DevOpsConfigService(new FakeLocalStorageService());
        var service = new DevOpsApiService(new HttpClient(), configService, new DeploymentConfigService(new HttpClient()));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetWorkItemHierarchyAsync("Area"));
    }

    [Fact]
    public async Task GetValidationItemsAsync_Throws_When_Config_Incomplete()
    {
        var configService = new DevOpsConfigService(new FakeLocalStorageService());
        var service = new DevOpsApiService(new HttpClient(), configService, new DeploymentConfigService(new HttpClient()));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetValidationItemsAsync("Area", new[] { "New" }));
    }

    [Theory]
    [InlineData("\\Project\\Area\\Dev", "Project\\Dev")]
    [InlineData("Project\\Dev", "Project\\Dev")]
    public void NormalizeAreaPath_Returns_Expected(string input, string expected)
    {
        var normalized = InvokeNormalizeAreaPath(input);

        Assert.Equal(expected, normalized);
    }

    [Fact]
    public void ExtractPaths_Collects_All_Paths()
    {
        var json =
            "{\"path\":\"A\",\"children\":[{\"path\":\"A\\\\B\"},{\"path\":\"A\\\\C\",\"children\":[{\"path\":\"A\\\\C\\\\D\"}]}]}";
        var doc = JsonDocument.Parse(json);
        List<string> list = [];

        InvokeExtractPaths(doc.RootElement, list);

        Assert.Equal(4, list.Count);
        Assert.Contains("A", list);
        Assert.Contains("A\\B", list);
        Assert.Contains("A\\C", list);
        Assert.Contains("A\\C\\D", list);
    }

    [Fact]
    public async Task GetBacklogsAsync_Returns_Normalized_Paths()
    {
        var classificationJson =
            "{\"children\":[{\"path\":\"Project\\\\Area\"},{\"path\":\"Project\\\\Area\\\\Sub\"}]}";
        var handler = new FakeHttpMessageHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(classificationJson)
            };
            return response;
        });
        var client = new HttpClient(handler);
        var storage = new FakeLocalStorageService();
        var configService = new DevOpsConfigService(storage);
        await configService.SaveAsync(
            new DevOpsConfig { Organization = "Org", Project = "Project", PatToken = "token" });
        var service = new DevOpsApiService(client, configService, new DeploymentConfigService(new HttpClient()));

        var result = await service.GetBacklogsAsync();

        Assert.Equal(["Project", "Project\\Sub"], result);
    }

    [Fact]
    public async Task UpdateWorkItemStateAsync_Sends_Patch_Request()
    {
        HttpRequestMessage? captured = null;
        var handler = new FakeHttpMessageHandler(req =>
        {
            captured = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            };
        });
        var client = new HttpClient(handler);
        var storage = new FakeLocalStorageService();
        var configService = new DevOpsConfigService(storage);
        await configService.SaveAsync(new DevOpsConfig { Organization = "Org", Project = "Proj", PatToken = "token" });
        var service = new DevOpsApiService(client, configService, new DeploymentConfigService(new HttpClient()));

        await service.UpdateWorkItemStateAsync(42, "Active");

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Patch, captured!.Method);
        Assert.NotNull(captured.RequestUri);
        Assert.Equal("https://dev.azure.com/Org/Proj/_apis/wit/workitems/42?api-version=7.0",
            captured.RequestUri.ToString());
        var body = await captured.Content!.ReadAsStringAsync();
        Assert.Contains("\"Active\"", body);
    }

    [Fact]
    public async Task GetValidationItemsAsync_Returns_Work_Items()
    {
        var wiqlJson = "{\"workItems\":[{\"id\":1}]}";
        var itemsJson =
            "{\"value\":[{\"id\":1,\"fields\":{\"System.Title\":\"Story\",\"System.State\":\"New\",\"System.WorkItemType\":\"User Story\"}}]}";
        var call = 0;
        var handler = new FakeHttpMessageHandler(_ =>
        {
            call++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(call == 1 ? wiqlJson : itemsJson)
            };
        });
        var client = new HttpClient(handler);
        var storage = new FakeLocalStorageService();
        var configService = new DevOpsConfigService(storage);
        await configService.SaveAsync(new DevOpsConfig { Organization = "Org", Project = "Proj", PatToken = "token" });
        var service = new DevOpsApiService(client, configService, new DeploymentConfigService(new HttpClient()));

        var result = await service.GetValidationItemsAsync("Area", new[] { "New" });

        Assert.Single(result);
        Assert.Equal(1, result[0].Info.Id);
        Assert.Equal("https://dev.azure.com/Org/Proj/_workitems/edit/1", result[0].Info.Url);
    }

    [Fact]
    public void BuildStorySearchWiql_Contains_Conditions()
    {
        var query = InvokeBuildStorySearchWiql("test");

        Assert.Contains("User Story", query);
        Assert.Contains("CONTAINS 'test'", query);
        Assert.Contains("ORDER BY [System.ChangedDate] DESC", query);
    }

    [Fact]
    public void BuildReleaseSearchWiql_Contains_Conditions()
    {
        var query = InvokeBuildReleaseSearchWiql("foo");

        Assert.Contains("User Story", query);
        Assert.Contains("Bug", query);
        Assert.Contains("CONTAINS 'foo'", query);
    }

    [Fact]
    public async Task GetStoryHierarchyDetailsAsync_Throws_When_Config_Incomplete()
    {
        var configService = new DevOpsConfigService(new FakeLocalStorageService());
        var service = new DevOpsApiService(new HttpClient(), configService, new DeploymentConfigService(new HttpClient()));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetStoryHierarchyDetailsAsync([1]));
    }

    [Fact]
    public void BuildMetricsWiql_Uses_Start_Date()
    {
        var query = InvokeBuildMetricsWiql("Area", new DateTime(2024, 1, 1));

        Assert.Contains("2024-01-01", query);
    }

    [Fact]
    public void BuildStoriesWiql_Includes_States_When_Provided()
    {
        var query = InvokeBuildStoriesWiql("Area", new[] { "New", "Active" });

        Assert.Contains("'New'", query);
        Assert.Contains("'Active'", query);
        Assert.Contains("User Story", query);
    }

    [Fact]
    public void BuildStoriesWiql_Omits_State_Filter_When_None()
    {
        var query = InvokeBuildStoriesWiql("Area", []);

        Assert.DoesNotContain("System.State", query);
    }

    [Fact]
    public async Task SearchUserStoriesAsync_Returns_Items()
    {
        var wiqlJson = "{\"workItems\":[{\"id\":1}]}";
        var itemsJson = "{\"value\":[{\"id\":1,\"fields\":{\"System.Title\":\"Story\",\"System.State\":\"New\",\"System.WorkItemType\":\"User Story\"}}]}";
        var call = 0;
        var handler = new FakeHttpMessageHandler(_ =>
        {
            call++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(call == 1 ? wiqlJson : itemsJson)
            };
        });
        var client = new HttpClient(handler);
        var storage = new FakeLocalStorageService();
        var configService = new DevOpsConfigService(storage);
        await configService.SaveAsync(new DevOpsConfig { Organization = "Org", Project = "Proj", PatToken = "token" });
        var service = new DevOpsApiService(client, configService, new DeploymentConfigService(new HttpClient()));

        var result = await service.SearchUserStoriesAsync("test");

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public async Task SearchReleaseItemsAsync_Returns_Items()
    {
        var wiqlJson = "{\"workItems\":[{\"id\":2}]}";
        var itemsJson = "{\"value\":[{\"id\":2,\"fields\":{\"System.Title\":\"Bug\",\"System.State\":\"New\",\"System.WorkItemType\":\"Bug\"}}]}";
        var call = 0;
        var handler = new FakeHttpMessageHandler(_ =>
        {
            call++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(call == 1 ? wiqlJson : itemsJson)
            };
        });
        var client = new HttpClient(handler);
        var storage = new FakeLocalStorageService();
        var configService = new DevOpsConfigService(storage);
        await configService.SaveAsync(new DevOpsConfig { Organization = "Org", Project = "Proj", PatToken = "token" });
        var service = new DevOpsApiService(client, configService, new DeploymentConfigService(new HttpClient()));

        var result = await service.SearchReleaseItemsAsync("bug");

        Assert.Single(result);
        Assert.Equal(2, result[0].Id);
    }

    [Fact]
    public async Task GetStoryMetricsAsync_Returns_Metrics()
    {
        var wiqlJson = "{\"workItems\":[{\"id\":1}]}";
        var itemsJson = "{\"value\":[{\"id\":1,\"fields\":{\"System.CreatedDate\":\"2024-01-01T00:00:00Z\",\"Microsoft.VSTS.Common.ActivatedDate\":\"2024-01-02T00:00:00Z\",\"Microsoft.VSTS.Common.ClosedDate\":\"2024-01-03T00:00:00Z\",\"Microsoft.VSTS.Scheduling.StoryPoints\":5,\"Microsoft.VSTS.Scheduling.OriginalEstimate\":8}}]}";
        var call = 0;
        var handler = new FakeHttpMessageHandler(_ =>
        {
            call++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(call == 1 ? wiqlJson : itemsJson)
            };
        });
        var client = new HttpClient(handler);
        var storage = new FakeLocalStorageService();
        var configService = new DevOpsConfigService(storage);
        await configService.SaveAsync(new DevOpsConfig { Organization = "Org", Project = "Proj", PatToken = "token" });
        var service = new DevOpsApiService(client, configService, new DeploymentConfigService(new HttpClient()));

        var result = await service.GetStoryMetricsAsync("Area", new DateTime(2024, 1, 1));

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.Equal(new DateTime(2024, 1, 1), result[0].CreatedDate);
        Assert.Equal(new DateTime(2024, 1, 2), result[0].ActivatedDate);
        Assert.Equal(new DateTime(2024, 1, 3), result[0].ClosedDate);
        Assert.Equal(5, result[0].StoryPoints);
        Assert.Equal(8, result[0].OriginalEstimate);
    }

    [Fact]
    public async Task SearchWikiPagesAsync_Uses_Api_Endpoint()
    {
        HttpRequestMessage? captured = null;
        var handler = new FakeHttpMessageHandler(req =>
        {
            captured = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"results\":[]}")
            };
        });
        var client = new HttpClient(handler);
        var storage = new FakeLocalStorageService();
        var configService = new DevOpsConfigService(storage);
        await configService.SaveAsync(new DevOpsConfig { Organization = "Org", Project = "Proj", PatToken = "token" });
        var service = new DevOpsApiService(client, configService, new DeploymentConfigService(new HttpClient()));

        var results = await service.SearchWikiPagesAsync("test");

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Post, captured!.Method);
        Assert.NotNull(captured.RequestUri);
        Assert.Equal("https://almsearch.dev.azure.com/Org/Proj/_apis/search/wikisearchresults?api-version=7.1",
            captured.RequestUri.ToString());
        var body = await captured.Content!.ReadAsStringAsync();
        Assert.Contains("test", body);
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetWikisAsync_Uses_Api_Endpoint()
    {
        HttpRequestMessage? captured = null;
        var handler = new FakeHttpMessageHandler(req =>
        {
            captured = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"value\":[]}")
            };
        });
        var client = new HttpClient(handler);
        var storage = new FakeLocalStorageService();
        var configService = new DevOpsConfigService(storage);
        await configService.SaveAsync(new DevOpsConfig { Organization = "Org", Project = "Proj", PatToken = "token" });
        var service = new DevOpsApiService(client, configService, new DeploymentConfigService(new HttpClient()));

        var results = await service.GetWikisAsync();

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Get, captured!.Method);
        Assert.NotNull(captured.RequestUri);
        Assert.Equal("https://dev.azure.com/Org/Proj/_apis/wiki/wikis?api-version=7.1-preview.1",
            captured.RequestUri.ToString());
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetWikiPageTreeAsync_Uses_Api_Endpoint()
    {
        HttpRequestMessage? captured = null;
        var handler = new FakeHttpMessageHandler(req =>
        {
            captured = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"path\":\"/\",\"order\":0,\"isParentPage\":true,\"gitItemPath\":\"/index.md\",\"subPages\":[{\"path\":\"/Child\",\"order\":1,\"gitItemPath\":\"/Child.md\",\"subPages\":[]}]}")
            };
        });
        var client = new HttpClient(handler);
        var storage = new FakeLocalStorageService();
        var configService = new DevOpsConfigService(storage);
        await configService.SaveAsync(new DevOpsConfig { Organization = "Org", Project = "Proj", PatToken = "token" });
        var service = new DevOpsApiService(client, configService, new DeploymentConfigService(new HttpClient()));

        var result = await service.GetWikiPageTreeAsync("1");

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Get, captured!.Method);
        Assert.NotNull(captured.RequestUri);
        Assert.Equal("https://dev.azure.com/Org/Proj/_apis/wiki/wikis/1/pages?recursionLevel=Full&api-version=7.1-preview.1",
            captured.RequestUri.ToString());
        Assert.NotNull(result);
        Assert.Equal("/", result!.Path);
        Assert.Single(result.Children);
        Assert.Equal("/Child", result.Children[0].Path);
    }

    [Fact]
    public async Task GetRepositoriesAsync_Uses_Api_Endpoint()
    {
        HttpRequestMessage? captured = null;
        var handler = new FakeHttpMessageHandler(req =>
        {
            captured = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"value\":[]}")
            };
        });
        var client = new HttpClient(handler);
        var storage = new FakeLocalStorageService();
        var configService = new DevOpsConfigService(storage);
        await configService.SaveAsync(new DevOpsConfig { Organization = "Org", Project = "Proj", PatToken = "token" });
        var service = new DevOpsApiService(client, configService, new DeploymentConfigService(new HttpClient()));

        var results = await service.GetRepositoriesAsync();

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Get, captured!.Method);
        Assert.NotNull(captured.RequestUri);
        Assert.Equal("https://dev.azure.com/Org/Proj/_apis/git/repositories?api-version=7.1", captured.RequestUri.ToString());
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetBranchesAsync_Uses_Api_Endpoint()
    {
        HttpRequestMessage? captured = null;
        var handler = new FakeHttpMessageHandler(req =>
        {
            captured = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"value\":[{\"name\":\"refs/heads/feature\",\"aheadCount\":1,\"behindCount\":2,\"commit\":{\"committer\":{\"date\":\"2024-01-01T00:00:00Z\"}}}]}")
            };
        });
        var client = new HttpClient(handler);
        var storage = new FakeLocalStorageService();
        var configService = new DevOpsConfigService(storage);
        await configService.SaveAsync(new DevOpsConfig { Organization = "Org", Project = "Proj", PatToken = "token" });
        var service = new DevOpsApiService(client, configService, new DeploymentConfigService(new HttpClient()));

        var results = await service.GetBranchesAsync("1", " main ");

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Get, captured!.Method);
        Assert.NotNull(captured.RequestUri);
        Assert.Equal("https://dev.azure.com/Org/Proj/_apis/git/repositories/1/stats/branches?api-version=7.1&baseVersion=GBmain", captured.RequestUri.ToString());
        Assert.Single(results);
        Assert.Equal("feature", results[0].Name);
        Assert.Equal(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), results[0].CommitDate);
        Assert.Equal(1, results[0].Ahead);
        Assert.Equal(2, results[0].Behind);
    }
}
