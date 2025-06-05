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
    public void BuildWiql_Ignores_State_And_Tags()
    {
        var query = InvokeBuildWiql("Area");

        Assert.DoesNotContain("System.State", query);
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
    public void BuildWiql_Includes_LinkType()
    {
        var query = InvokeBuildWiql("Area");

        Assert.Contains("[System.Links.LinkType] = 'System.LinkTypes.Hierarchy-Forward'", query);
    }

    [Fact]
    public void ComputeStatus_Leaf_Node_Always_Valid()
    {
        var node = new WorkItemNode { Info = new WorkItemInfo { State = "Done" } };

        InvokeComputeStatus(node);

        Assert.True(node.StatusValid);
    }

    [Fact]
    public void ComputeStatus_Done_Node_With_Incomplete_Children_Invalid()
    {
        var root = new WorkItemNode { Info = new WorkItemInfo { State = "Done" } };
        root.Children.Add(new WorkItemNode { Info = new WorkItemInfo { State = "Done" } });
        root.Children.Add(new WorkItemNode { Info = new WorkItemInfo { State = "Active" } });

        InvokeComputeStatus(root);

        Assert.False(root.StatusValid);
    }

    [Fact]
    public void ComputeStatus_NotDone_Node_With_Mixed_Children_Valid()
    {
        var root = new WorkItemNode { Info = new WorkItemInfo { State = "Active" } };
        root.Children.Add(new WorkItemNode { Info = new WorkItemInfo { State = "Done" } });
        root.Children.Add(new WorkItemNode { Info = new WorkItemInfo { State = "Active" } });

        InvokeComputeStatus(root);

        Assert.True(root.StatusValid);
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
