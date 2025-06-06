using System.Reflection;
using Bunit;
using DevOpsAssistant.Pages;
using DevOpsAssistant.Services;
using DevOpsAssistant.Tests.Utils;

namespace DevOpsAssistant.Tests.Pages;

public class ReleaseNotesPageTests : ComponentTestBase
{
    [Fact]
    public void ReleaseNotes_Renders_With_PopoverProvider()
    {
        SetupServices(includeApi: true);

        var exception = Record.Exception(() => RenderWithProvider<TestPage>());
        Assert.Null(exception);
    }

    [Fact]
    public void ReleaseNotes_Shows_Copy_Button_When_Prompt_Set()
    {
        SetupServices(includeApi: true);

        var page = RenderWithProvider<TestPage>();
        var field = typeof(ReleaseNotes).GetField("_prompt", BindingFlags.NonPublic | BindingFlags.Instance)!;
        field.SetValue(page.Instance, "text");
        page.Render();

        Assert.Contains("Copy", page.Markup);
    }

    [Fact]
    public void OnStorySelected_Adds_To_SelectedStories()
    {
        SetupServices(includeApi: true);

        var page = RenderWithProvider<TestPage>();
        var method = typeof(ReleaseNotes).GetMethod("OnStorySelected", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var setField =
            typeof(ReleaseNotes).GetField("_selectedStories", BindingFlags.NonPublic | BindingFlags.Instance)!;

        var item = new WorkItemInfo { Id = 1, Title = "Test" };
        page.InvokeAsync(() => method.Invoke(page.Instance, [item]));
        page.Render();

        var set = (HashSet<WorkItemInfo>)setField.GetValue(page.Instance)!;
        Assert.Contains(item, set);
        Assert.Contains("Test", page.Markup);
    }

    private class TestPage : ReleaseNotes
    {
        protected override Task OnInitializedAsync()
        {
            return Task.CompletedTask;
        }
    }

    // Rendering uses ComponentTestBase.RenderWithProvider
}
