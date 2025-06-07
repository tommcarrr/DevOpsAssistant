using System.Reflection;
using System.Net;
using System.Threading;
using System.Linq;
using Bunit;
using DevOpsAssistant.Pages;
using DevOpsAssistant.Services;
using DevOpsAssistant.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

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

    [Fact]
    public async Task SearchStories_Filters_Selected_Items()
    {
        var config = SetupServices();
        await config.SaveAsync(new DevOpsConfig { Organization = "Org", Project = "Proj", PatToken = "token" });

        var wiqlJson = "{\"workItems\":[{\"id\":1},{\"id\":2}]}";
        var itemsJson =
            "{\"value\":[{\"id\":1,\"fields\":{\"System.Title\":\"Story 1\",\"System.State\":\"New\",\"System.WorkItemType\":\"User Story\"}},{\"id\":2,\"fields\":{\"System.Title\":\"Story 2\",\"System.State\":\"New\",\"System.WorkItemType\":\"User Story\"}}]}";
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
        Services.AddSingleton(new DevOpsApiService(client, config));

        var page = RenderWithProvider<TestPage>();
        var setField = typeof(ReleaseNotes).GetField("_selectedStories", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var set = (HashSet<WorkItemInfo>)setField.GetValue(page.Instance)!;
        set.Add(new WorkItemInfo { Id = 1, Title = "Story 1" });

        var method = typeof(ReleaseNotes).GetMethod("SearchStories", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var task = (Task<IEnumerable<WorkItemInfo>>)method.Invoke(page.Instance, ["Story", CancellationToken.None])!;
        var result = (await task).ToList();

        Assert.Single(result);
        Assert.Equal(2, result[0].Id);
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
