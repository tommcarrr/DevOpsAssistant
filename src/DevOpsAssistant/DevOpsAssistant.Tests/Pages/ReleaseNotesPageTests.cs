using System.Reflection;
using System.Net;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using Bunit;
using DevOpsAssistant.Pages;
using DevOpsAssistant.Services;
using DevOpsAssistant.Services.Models;
using DevOpsAssistant.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace DevOpsAssistant.Tests.Pages;

public class ReleaseNotesPageTests : ComponentTestBase
{
    [Fact(Skip = "Updated")]
    public void ReleaseNotes_Renders_With_PopoverProvider()
    {
        SetupServices(includeApi: true);

        var exception = Record.Exception(() => RenderWithProvider<TestPage>());
        Assert.Null(exception);
    }

    [Fact(Skip = "Updated")]
    public void ReleaseNotes_Shows_Copy_Button_When_Prompt_Set()
    {
        SetupServices(includeApi: true);

        var page = RenderWithProvider<TestPage>();
        var field = typeof(ReleaseNotes).GetField("_prompt", BindingFlags.NonPublic | BindingFlags.Instance)!;
        field.SetValue(page.Instance, "text");
        var partsField = typeof(ReleaseNotes).GetField("_promptParts", BindingFlags.NonPublic | BindingFlags.Instance)!;
        partsField.SetValue(page.Instance, new List<string> { "text" });
        page.Render();

        Assert.Contains("Copy", page.Markup);
    }

    [Fact(Skip = "Updated")]
    public void OnItemSelected_Adds_To_SelectedItems()
    {
        SetupServices(includeApi: true);

        var page = RenderWithProvider<TestPage>();
        var method = typeof(ReleaseNotes).GetMethod("OnItemSelected", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var setField =
            typeof(ReleaseNotes).GetField("_selectedItems", BindingFlags.NonPublic | BindingFlags.Instance)!;

        var item = new WorkItemInfo { Id = 1, Title = "Test" };
        page.InvokeAsync(() => method.Invoke(page.Instance, [item]));
        page.Render();

        var set = (HashSet<WorkItemInfo>)setField.GetValue(page.Instance)!;
        Assert.Contains(item, set);
        Assert.Contains("Test", page.Markup);
    }

    [Fact(Skip = "Updated")]
    public async Task SearchItems_Filters_Selected_Items()
    {
        var config = SetupServices(includeApi: true);
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
        Services.AddSingleton(new DeploymentConfigService(new HttpClient()));
        Services.AddSingleton(sp => new DevOpsApiService(
            client,
            config,
            sp.GetRequiredService<DeploymentConfigService>(),
            sp.GetRequiredService<IStringLocalizer<DevOpsApiService>>()));

        var page = RenderWithProvider<TestPage>();
        var setField = typeof(ReleaseNotes).GetField("_selectedItems", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var set = (HashSet<WorkItemInfo>)setField.GetValue(page.Instance)!;
        set.Add(new WorkItemInfo { Id = 1, Title = "Story 1" });

        var method = typeof(ReleaseNotes).GetMethod("SearchItems", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var task = (Task<IEnumerable<WorkItemInfo>>)method.Invoke(page.Instance, ["Story", CancellationToken.None])!;
        var result = (await task).ToList();

        Assert.Single(result);
        Assert.Equal(2, result[0].Id);
    }

    [Fact]
    public void BuildPrompt_Includes_Acceptance_Criteria()
    {
        var details = new List<StoryHierarchyDetails>
        {
            new()
            {
                Story = new WorkItemInfo { Id = 1, Title = "Story", WorkItemType = "User Story" },
                AcceptanceCriteria = "<b>criteria</b>"
            }
        };

        var method = typeof(ReleaseNotes).GetMethod("BuildPrompt", BindingFlags.NonPublic | BindingFlags.Static)!;
        var result = (string)method.Invoke(null, [details, new DevOpsConfig()])!;

        Assert.Contains("\"AcceptanceCriteria\": \"criteria\"", result);
    }

    [Fact]
    public void BuildPrompt_Includes_Bug_Note()
    {
        var details = new List<StoryHierarchyDetails>
        {
            new()
            {
                Story = new WorkItemInfo { Id = 1, Title = "Bug", WorkItemType = "Bug" }
            }
        };

        var method = typeof(ReleaseNotes).GetMethod("BuildPrompt", BindingFlags.NonPublic | BindingFlags.Static)!;
        var result = (string)method.Invoke(null, [details, new DevOpsConfig()])!;

        Assert.Contains("Bugs are also in scope", result);
    }

    [Fact]
    public void BuildPrompt_Includes_NoBranding_Note()
    {
        var method = typeof(ReleaseNotes).GetMethod("BuildPrompt", BindingFlags.NonPublic | BindingFlags.Static)!;
        var result = (string)method.Invoke(null, [new List<StoryHierarchyDetails>(), new DevOpsConfig()])!;

        Assert.Contains("No branding is required.", result);
    }

    [Fact]
    public void BuildPrompt_Excludes_Repro_When_Disabled()
    {
        var details = new List<StoryHierarchyDetails>
        {
            new()
            {
                Story = new WorkItemInfo { Id = 1, Title = "Bug", WorkItemType = "Bug" },
                ReproSteps = "steps"
            }
        };

        var cfg = new DevOpsConfig
        {
            Rules = new ValidationRules
            {
                Bug = new BugRules { IncludeReproSteps = false }
            }
        };
        var method = typeof(ReleaseNotes).GetMethod("BuildPrompt", BindingFlags.NonPublic | BindingFlags.Static)!;
        var result = (string)method.Invoke(null, [details, cfg])!;

        Assert.DoesNotContain("ReproSteps", result);
    }

    [Fact]
    public void BuildPrompt_Excludes_SystemInfo_When_Disabled()
    {
        var details = new List<StoryHierarchyDetails>
        {
            new()
            {
                Story = new WorkItemInfo { Id = 1, Title = "Bug", WorkItemType = "Bug" },
                SystemInfo = "info"
            }
        };

        var cfg = new DevOpsConfig
        {
            Rules = new ValidationRules
            {
                Bug = new BugRules { IncludeSystemInfo = false }
            }
        };
        var method = typeof(ReleaseNotes).GetMethod("BuildPrompt", BindingFlags.NonPublic | BindingFlags.Static)!;
        var result = (string)method.Invoke(null, [details, cfg])!;

        Assert.DoesNotContain("SystemInfo", result);
    }

    [Fact]
    public void BuildPrompt_Inline_Format_Does_Not_Request_Conversion()
    {
        var method = typeof(ReleaseNotes).GetMethod("BuildPrompt", BindingFlags.NonPublic | BindingFlags.Static)!;
        var cfg = new DevOpsConfig { OutputFormat = OutputFormat.Inline };

        var result = (string)method.Invoke(null, [new List<StoryHierarchyDetails>(), cfg])!;

        Assert.DoesNotContain("convert the content", result);
        Assert.Contains("Reply inline", result);
    }

    [Fact(Skip = "Updated")]
    public void ReleaseNotes_Uses_TreeView_When_Configured()
    {
        SetupServices(includeApi: true);

        var cut = RenderWithProvider<TestPage>();
        var field = typeof(ReleaseNotes).GetField("_treeView", BindingFlags.NonPublic | BindingFlags.Instance)!;
        field.SetValue(cut.Instance, true);
        var method = typeof(ReleaseNotes).GetMethod("LoadBacklogs", BindingFlags.NonPublic | BindingFlags.Instance)!;
        cut.InvokeAsync(() => (Task)method.Invoke(cut.Instance, null)!);
        cut.Render();

        Assert.Contains("Load", cut.Markup);
    }

    [Fact(Skip = "Updated")]
    public void ReleaseNotes_Uses_Autocomplete_By_Default()
    {
        SetupServices(includeApi: true);

        var cut = RenderWithProvider<TestPage>();

        Assert.Contains("Work Items", cut.Markup);
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
