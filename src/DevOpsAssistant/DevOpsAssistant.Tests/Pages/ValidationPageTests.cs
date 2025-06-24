using Bunit;
using DevOpsAssistant.Pages;
using DevOpsAssistant.Services;
using DevOpsAssistant.Tests.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Net;
using System.Net.Http;
using DevOpsAssistant.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace DevOpsAssistant.Tests.Pages;

public class ValidationPageTests : ComponentTestBase
{
    [Fact]
    public void Validation_Renders_With_PopoverProvider()
    {
        SetupServices(includeApi: true);

        var exception = Record.Exception(() => RenderWithProvider<TestPage>());
        Assert.Null(exception);
    }

    [Fact]
    public void Results_Show_State_When_Set()
    {
        SetupServices(includeApi: true);

        var cut = RenderWithProvider<TestPage>();
        var type = typeof(Validation);
        var nested = type.GetNestedType("ResultItem", BindingFlags.NonPublic)!;
        var listType = typeof(List<>).MakeGenericType(nested);
        var list = (IList)Activator.CreateInstance(listType)!;
        var item = Activator.CreateInstance(nested)!;
        nested.GetProperty("Info")!.SetValue(item, new WorkItemInfo
        {
            Id = 1,
            Title = "Story",
            State = "Active",
            WorkItemType = "User Story",
            Url = "http://localhost"
        });
        nested.GetProperty("Violations")!.SetValue(item, new List<string> { "V" });
        list.Add(item);
        var field = type.GetField("_results", BindingFlags.NonPublic | BindingFlags.Instance)!;
        field.SetValue(cut.Instance, list);
        cut.Render();

        Assert.Contains("Active", cut.Markup);
    }

    [Fact]
    public async Task Rules_Are_Collapsed_By_Default()
    {
        var config = SetupServices(includeApi: true);
        await config.SaveAsync(new DevOpsConfig
        {
            Rules = new ValidationRules { Story = new StoryRules { HasDescription = true } }
        });

        var cut = RenderWithProvider<TestPage>();
        var method = typeof(Validation).GetMethod("ComputeRules", BindingFlags.NonPublic | BindingFlags.Instance)!;
        await cut.InvokeAsync(() => method.Invoke(cut.Instance, null));
        cut.Render();

        var expandedField = typeof(Validation).GetField("_rulesExpanded", BindingFlags.NonPublic | BindingFlags.Instance)!;
        Assert.False((bool)expandedField.GetValue(cut.Instance)!);

        cut.Find("button").Click();
        cut.WaitForAssertion(() => Assert.True((bool)expandedField.GetValue(cut.Instance)!));
    }

    [Fact]
    public async Task OnInitialized_Loads_Types_From_State()
    {
        var config = SetupServices(includeApi: true);
        await config.SaveAsync(new DevOpsConfig
        {
            Organization = "Org",
            Project = "Proj",
            PatToken = "token"
        });

        var handler = new FakeHttpMessageHandler(req =>
        {
            if (req.RequestUri!.AbsoluteUri.Contains("classificationnodes"))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"path\":\"Area\"}") };
            if (req.RequestUri!.AbsoluteUri.Contains("states"))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"value\":[{\"name\":\"New\"}]}") };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        });
        var client = new HttpClient(handler);
        Services.AddSingleton(new DeploymentConfigService(new HttpClient()));
        Services.AddSingleton<DevOpsApiService>(sp => new DevOpsApiService(
            client,
            config,
            sp.GetRequiredService<DeploymentConfigService>(),
            sp.GetRequiredService<IStringLocalizer<DevOpsApiService>>()));

        var stateService = Services.GetRequiredService<PageStateService>();
        await stateService.SaveAsync("validation", new TestState
        {
            Path = "Area",
            States = new[] { "New" },
            Types = new[] { "Bug" }
        });

        var cut = RenderWithProvider<Validation>();

        var prop = typeof(Validation).GetProperty("SelectedTypes", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var set = (HashSet<string>)prop.GetValue(cut.Instance)!;
        Assert.Single(set);
        Assert.Contains("Bug", set);
    }

    private class TestState
    {
        public string Path { get; set; } = string.Empty;
        public string[]? States { get; set; }
        public string[]? Types { get; set; }
    }

    private class TestPage : Validation
    {
        protected override Task OnInitializedAsync()
        {
            return Task.CompletedTask;
        }
    }

    // Rendering uses ComponentTestBase.RenderWithProvider
}
