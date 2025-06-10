using Bunit;
using DevOpsAssistant.Pages;
using DevOpsAssistant.Services;
using DevOpsAssistant.Tests.Utils;
using System.Collections.Generic;
using System.Linq;

namespace DevOpsAssistant.Tests.Pages;

public class WorkItemsPageTests : ComponentTestBase
{
    [Fact]
    public void WorkItems_Renders_With_PopoverProvider()
    {
        SetupServices(includeApi: true);

        var exception = Record.Exception(() => RenderWithProvider<TestWorkItems>());
        Assert.Null(exception);
    }

    [Fact]
    public void UpdateAll_Button_Shown_When_Items_Loaded()
    {
        SetupServices(includeApi: true);

        var cut = RenderWithProvider<LoadedWorkItems>();

        var button = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Update All"));
        Assert.NotNull(button);
    }

    private class TestWorkItems : WorkItems
    {
        protected override Task OnInitializedAsync()
        {
            return Task.CompletedTask;
        }
    }

    private class LoadedWorkItems : WorkItems
    {
        protected override Task OnInitializedAsync()
        {
            var root = new WorkItemNode
            {
                Info = new WorkItemInfo { Id = 1, Title = "Epic", State = "New", WorkItemType = "Epic" },
                ExpectedState = "Active",
                StatusValid = false
            };

            var backlogsField = typeof(WorkItems).GetField("_backlogs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            backlogsField.SetValue(this, new[] { "Area" });
            var pathField = typeof(WorkItems).GetField("_path", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            pathField.SetValue(this, "Area");
            var rootsField = typeof(WorkItems).GetField("_roots", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            rootsField.SetValue(this, new List<WorkItemNode> { root });

            return Task.CompletedTask;
        }
    }

    // Rendering uses ComponentTestBase.RenderWithProvider
}
