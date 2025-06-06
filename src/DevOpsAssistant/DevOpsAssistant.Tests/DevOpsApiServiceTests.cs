using System.Net;
using System.Reflection;
using System.Text.Json;
using DevOpsAssistant.Services;

namespace DevOpsAssistant.Tests;

public class DevOpsApiServiceTests
{
    private static string InvokeBuildEpicsWiql(string area)
    {
        var method = typeof(DevOpsApiService).GetMethod("BuildEpicsWiql", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (string)method.Invoke(null, [area])!;
    }

    private static string InvokeBuildValidationWiql(string area)
    {
        var method =
            typeof(DevOpsApiService).GetMethod("BuildValidationWiql", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (string)method.Invoke(null, [area])!;
    }

    private static void InvokeComputeStatus(WorkItemNode node)
    {
        var method = typeof(DevOpsApiService).GetMethod("ComputeStatus", BindingFlags.NonPublic | BindingFlags.Static)!;
        method.Invoke(null, [node]);
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

    private static string InvokeBuildMetricsWiql(string area, DateTime start)
    {
        var method =
            typeof(DevOpsApiService).GetMethod("BuildMetricsWiql", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (string)method.Invoke(null, [area, start])!;
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
        var query = InvokeBuildValidationWiql("Area");

        Assert.Contains("'Epic'", query);
        Assert.Contains("'Feature'", query);
        Assert.Contains("'User Story'", query);
        Assert.DoesNotContain("Task", query);
    }

    [Fact]
    public void ComputeStatus_Leaf_Node_ExpectedState_Equals_Current()
    {
        var node = new WorkItemNode { Info = new WorkItemInfo { State = "New" } };

        InvokeComputeStatus(node);

        Assert.True(node.StatusValid);
        Assert.Equal("New", node.ExpectedState);
    }

    [Fact]
    public void ComputeStatus_Invalid_When_Child_Not_New()
    {
        var root = new WorkItemNode { Info = new WorkItemInfo { State = "New" } };
        root.Children.Add(new WorkItemNode { Info = new WorkItemInfo { State = "New" } });
        root.Children.Add(new WorkItemNode { Info = new WorkItemInfo { State = "Active" } });

        InvokeComputeStatus(root);

        Assert.False(root.StatusValid);
        Assert.Equal("Active", root.ExpectedState);
    }

    [Fact]
    public void ComputeStatus_Valid_When_All_Children_Closed()
    {
        var root = new WorkItemNode { Info = new WorkItemInfo { State = "Closed" } };
        root.Children.Add(new WorkItemNode { Info = new WorkItemInfo { State = "Closed" } });
        root.Children.Add(new WorkItemNode { Info = new WorkItemInfo { State = "Removed" } });

        InvokeComputeStatus(root);

        Assert.True(root.StatusValid);
        Assert.Equal("Closed", root.ExpectedState);
    }

    [Fact]
    public void FilterClosedEpics_Removes_Closed_Epics()
    {
        var closed = new WorkItemNode { Info = new WorkItemInfo { WorkItemType = "Epic", State = "Closed" } };
        var open = new WorkItemNode { Info = new WorkItemInfo { WorkItemType = "Epic", State = "New" } };
        var list = new List<WorkItemNode> { closed, open };

        var result = InvokeFilterClosedEpics(list);

        Assert.DoesNotContain(closed, result);
        Assert.Contains(open, result);
    }

    [Fact]
    public async Task GetWorkItemHierarchyAsync_Throws_When_Config_Incomplete()
    {
        var configService = new DevOpsConfigService(new FakeLocalStorageService());
        var service = new DevOpsApiService(new HttpClient(), configService);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetWorkItemHierarchyAsync("Area"));
    }

    [Fact]
    public async Task GetValidationItemsAsync_Throws_When_Config_Incomplete()
    {
        var configService = new DevOpsConfigService(new FakeLocalStorageService());
        var service = new DevOpsApiService(new HttpClient(), configService);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetValidationItemsAsync("Area"));
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
        var list = new List<string>();

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
        var service = new DevOpsApiService(client, configService);

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
        var service = new DevOpsApiService(client, configService);

        await service.UpdateWorkItemStateAsync(42, "Active");

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Patch, captured!.Method);
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
        var service = new DevOpsApiService(client, configService);

        var result = await service.GetValidationItemsAsync("Area");

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
    public async Task GetStoryHierarchyDetailsAsync_Throws_When_Config_Incomplete()
    {
        var configService = new DevOpsConfigService(new FakeLocalStorageService());
        var service = new DevOpsApiService(new HttpClient(), configService);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetStoryHierarchyDetailsAsync([1]));
    }

    [Fact]
    public void BuildMetricsWiql_Uses_Start_Date()
    {
        var query = InvokeBuildMetricsWiql("Area", new DateTime(2024, 1, 1));

        Assert.Contains("2024-01-01", query);
    }
}