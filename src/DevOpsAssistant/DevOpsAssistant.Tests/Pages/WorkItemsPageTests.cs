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

    [Fact]
    public void IssuesOnly_Hides_Epics_Without_Issues()
    {
        SetupServices(includeApi: true);

        var cut = RenderWithProvider<IssuesOnlyWorkItems>();

        Assert.Contains("Epic 1", cut.Markup);
        Assert.Contains("Feature OK", cut.Markup);
        Assert.DoesNotContain("Epic 2", cut.Markup);
    }

    [Fact]
    public void IssuesOnly_Disabled_Shows_All_Epics()
    {
        SetupServices(includeApi: true);

        var cut = RenderWithProvider<IssuesOnlyDisabledWorkItems>();

        Assert.Contains("Epic 1", cut.Markup);
        Assert.Contains("Epic 2", cut.Markup);
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

    private class IssuesOnlyWorkItems : WorkItems
    {
        protected override Task OnInitializedAsync()
        {
            var epic1 = new WorkItemNode
            {
                Info = new WorkItemInfo { Id = 1, Title = "Epic 1", State = "New", WorkItemType = "Epic" },
                StatusValid = true
            };
            epic1.Children.Add(new WorkItemNode
            {
                Info = new WorkItemInfo { Id = 2, Title = "Feature OK", State = "New", WorkItemType = "Feature" },
                StatusValid = true
            });
            epic1.Children.Add(new WorkItemNode
            {
                Info = new WorkItemInfo { Id = 3, Title = "Feature Issue", State = "New", WorkItemType = "Feature" },
                StatusValid = false
            });

            var epic2 = new WorkItemNode
            {
                Info = new WorkItemInfo { Id = 4, Title = "Epic 2", State = "New", WorkItemType = "Epic" },
                StatusValid = true
            };

            var backlogsField = typeof(WorkItems).GetField("_backlogs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            backlogsField.SetValue(this, new[] { "Area" });
            var pathField = typeof(WorkItems).GetField("_path", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            pathField.SetValue(this, "Area");
            var rootsField = typeof(WorkItems).GetField("_roots", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            rootsField.SetValue(this, new List<WorkItemNode> { epic1, epic2 });

            return Task.CompletedTask;
        }
    }

    private class IssuesOnlyDisabledWorkItems : IssuesOnlyWorkItems
    {
        protected override Task OnInitializedAsync()
        {
            base.OnInitializedAsync();
            var field = typeof(WorkItems).GetField("_issuesOnly", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            field.SetValue(this, false);
            return Task.CompletedTask;
        }
    }

    // Rendering uses ComponentTestBase.RenderWithProvider
}
