using System.Reflection;
using Bunit;
using DevOpsAssistant.Pages;
using DevOpsAssistant.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace DevOpsAssistant.Tests;

public class ReleaseNotesPageTests : TestContext
{
    [Fact]
    public void ReleaseNotes_Renders_With_PopoverProvider()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(new DevOpsConfigService(new FakeLocalStorageService()));
        Services.AddSingleton<DevOpsApiService>(sp =>
            new DevOpsApiService(new HttpClient(), sp.GetRequiredService<DevOpsConfigService>()));

        var exception = Record.Exception(() => RenderComponent<Wrapper>());
        Assert.Null(exception);
    }

    [Fact]
    public void ReleaseNotes_Shows_Copy_Button_When_Prompt_Set()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(new DevOpsConfigService(new FakeLocalStorageService()));
        Services.AddSingleton<DevOpsApiService>(sp =>
            new DevOpsApiService(new HttpClient(), sp.GetRequiredService<DevOpsConfigService>()));

        var cut = RenderComponent<Wrapper>();
        var page = cut.FindComponent<TestPage>();
        var field = typeof(ReleaseNotes).GetField("_prompt", BindingFlags.NonPublic | BindingFlags.Instance)!;
        field.SetValue(page.Instance, "text");
        page.Render();

        Assert.Contains("Copy", page.Markup);
    }

    [Fact]
    public void OnStorySelected_Adds_To_SelectedStories()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(new DevOpsConfigService(new FakeLocalStorageService()));
        Services.AddSingleton<DevOpsApiService>(sp =>
            new DevOpsApiService(new HttpClient(), sp.GetRequiredService<DevOpsConfigService>()));

        var cut = RenderComponent<Wrapper>();
        var page = cut.FindComponent<TestPage>();
        var method = typeof(ReleaseNotes).GetMethod("OnStorySelected", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var setField =
            typeof(ReleaseNotes).GetField("_selectedStories", BindingFlags.NonPublic | BindingFlags.Instance)!;

        var item = new WorkItemInfo { Id = 1, Title = "Test" };
        page.InvokeAsync(() => method.Invoke(page.Instance, new object?[] { item }));
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

    private class Wrapper : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<TestPage>(1);
            builder.CloseComponent();
        }
    }
}