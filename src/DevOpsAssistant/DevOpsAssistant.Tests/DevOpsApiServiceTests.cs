using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using DevOpsAssistant.Services;
using Xunit;

namespace DevOpsAssistant.Tests;

public class DevOpsApiServiceTests
{
    private static string InvokeBuildWiql(string area)
    {
        var method = typeof(DevOpsApiService).GetMethod("BuildWiql", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (string)method.Invoke(null, new object?[] { area })!;
    }

    private static void InvokeComputeStatus(WorkItemNode node)
    {
        var method = typeof(DevOpsApiService).GetMethod("ComputeStatus", BindingFlags.NonPublic | BindingFlags.Static)!;
        method.Invoke(null, new object?[] { node });
    }

    private static List<WorkItemNode> InvokeFilterClosedEpics(List<WorkItemNode> nodes)
    {
        var method = typeof(DevOpsApiService).GetMethod("FilterClosedEpics", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (List<WorkItemNode>)method.Invoke(null, new object?[] { nodes })!;
    }

    [Fact]
    public void BuildWiql_Filters_Closed_Epics()
    {
        var query = InvokeBuildWiql("Area");

        Assert.Contains("[System.State] <> 'Closed'", query);
        Assert.Contains("[System.State] <> 'Removed'", query);
        Assert.Contains("[System.WorkItemType] <> 'Epic'", query);
        Assert.DoesNotContain("System.Tags", query);
        Assert.Contains("[System.AreaPath] UNDER 'Area'", query);
    }

    [Fact]
    public void BuildWiql_Trims_Leading_Backslash()
    {
        var query = InvokeBuildWiql("\\Area");

        Assert.Contains("[System.AreaPath] UNDER 'Area'", query);
    }

    [Fact]
    public void BuildWiql_Removes_Area_Prefix()
    {
        var query = InvokeBuildWiql("Project\\Area\\Development");

        Assert.Contains("[System.AreaPath] UNDER 'Project\\Development'", query);
    }

    [Fact]
    public void BuildWiql_Selects_WorkItems()
    {
        var query = InvokeBuildWiql("Area");

        Assert.DoesNotContain("WorkItemLinks", query);
        Assert.DoesNotContain("System.Links.LinkType", query);
        Assert.Contains("FROM WorkItems", query);
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
}
