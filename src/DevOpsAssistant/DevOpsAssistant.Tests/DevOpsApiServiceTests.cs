using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using DevOpsAssistant.Services;
using Xunit;

namespace DevOpsAssistant.Tests;

public class DevOpsApiServiceTests
{
    private static string InvokeBuildWiql(string area, string? state, string? tags)
    {
        var method = typeof(DevOpsApiService).GetMethod("BuildWiql", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (string)method.Invoke(null, new object?[] { area, state, tags })!;
    }

    private static void InvokeComputeStatus(WorkItemNode node)
    {
        var method = typeof(DevOpsApiService).GetMethod("ComputeStatus", BindingFlags.NonPublic | BindingFlags.Static)!;
        method.Invoke(null, new object?[] { node });
    }

    [Fact]
    public void BuildWiql_Includes_State_And_Tags()
    {
        var query = InvokeBuildWiql("Area", "Active", "tag1, tag2");

        Assert.Contains("[System.State] = 'Active'", query);
        Assert.Contains("[System.AreaPath] UNDER 'Area'", query);
        Assert.Contains("[System.Tags] CONTAINS 'tag1'", query);
        Assert.Contains("[System.Tags] CONTAINS 'tag2'", query);
    }

    [Fact]
    public void BuildWiql_Multiple_States()
    {
        var query = InvokeBuildWiql("Area", "New, Done", null);

        Assert.Contains("[System.State] in ('New','Done')", query);
    }

    [Fact]
    public void BuildWiql_Includes_LinkType()
    {
        var query = InvokeBuildWiql("Area", null, null);

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
    public async Task GetWorkItemHierarchyAsync_Throws_When_Config_Incomplete()
    {
        var configService = new DevOpsConfigService(new FakeLocalStorageService());
        var service = new DevOpsApiService(new HttpClient(), configService);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetWorkItemHierarchyAsync("Area"));
    }
}
