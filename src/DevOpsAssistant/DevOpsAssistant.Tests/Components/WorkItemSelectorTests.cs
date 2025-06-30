using System.Net;
using System.Linq;
using System.Collections.Generic;
using Bunit;
using DevOpsAssistant.Components;
using DevOpsAssistant.Services;
using DevOpsAssistant.Services.Models;
using DevOpsAssistant.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace DevOpsAssistant.Tests.Components;

public class WorkItemSelectorTests : ComponentTestBase
{
    [Fact]
    public async Task Shows_Iteration_Autocomplete_When_Enabled()
    {
        var config = SetupServices(includeApi: true);
        await config.SaveAsync(new DevOpsConfig { Organization = "Org", Project = "Proj", PatToken = "token" });

        var backlogJson = "{\"path\":\"Area\"}";
        var statesJson = "{\"value\":[{\"name\":\"New\"}]}";
        var iterationsJson = "{\"children\":[{\"name\":\"Sprint\",\"path\":\"Proj\\Sprint\",\"attributes\":{\"startDate\":\"2024-01-01T00:00:00Z\",\"finishDate\":\"2024-01-15T00:00:00Z\"}}]}";
        var handler = new FakeHttpMessageHandler(req =>
        {
            var url = req.RequestUri!.AbsoluteUri;
            if (url.Contains("classificationnodes/areas"))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(backlogJson) };
            if (url.Contains("classificationnodes/iterations"))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(iterationsJson) };
            if (url.Contains("states"))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(statesJson) };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        });
        var client = new HttpClient(handler);
        Services.AddSingleton(new DeploymentConfigService(new HttpClient()));
        Services.AddSingleton(sp => new DevOpsApiService(
            client,
            config,
            sp.GetRequiredService<DeploymentConfigService>(),
            sp.GetRequiredService<IStringLocalizer<DevOpsApiService>>()));

        var cut = RenderWithProvider<WorkItemSelector>();
        cut.SetParametersAndRender(parameters => parameters.Add(c => c.UseIteration, true));
        cut.WaitForAssertion(() => Assert.Contains("Iteration", cut.Markup));
    }

    [Fact]
    public async Task Backlog_Loads_Items_Into_Table()
    {
        var config = SetupServices(includeApi: true);
        await config.SaveAsync(new DevOpsConfig { Organization = "Org", Project = "Proj", PatToken = "token" });

        var backlogJson = "{\"path\":\"Area\"}";
        var statesJson = "{\"value\":[{\"name\":\"New\"}]}";
        var wiqlJson = "{\"workItems\":[{\"id\":1}]}";
        var itemsJson = "{\"value\":[{\"id\":1,\"fields\":{\"System.Title\":\"Story 1\",\"System.State\":\"New\",\"System.WorkItemType\":\"User Story\"}}]}";

        var handler = new FakeHttpMessageHandler(req =>
        {
            var url = req.RequestUri!.AbsoluteUri;
            if (url.Contains("classificationnodes/areas"))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(backlogJson) };
            if (url.Contains("states"))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(statesJson) };
            if (url.Contains("wiql"))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(wiqlJson) };
            if (url.Contains("workitems"))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(itemsJson) };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        });
        var client = new HttpClient(handler);
        Services.AddSingleton(new DeploymentConfigService(new HttpClient()));
        Services.AddSingleton(sp => new DevOpsApiService(
            client,
            config,
            sp.GetRequiredService<DeploymentConfigService>(),
            sp.GetRequiredService<IStringLocalizer<DevOpsApiService>>()));

        var cut = RenderWithProvider<WorkItemSelector>();
        var loadButton = cut.FindAll("button").First(b => b.TextContent.Contains("Load"));
        loadButton.Click();

        cut.WaitForAssertion(() => Assert.Contains("Story 1", cut.Markup));
        Assert.Contains("table", cut.Markup);
    }

    [Fact]
    public async Task Tag_Loads_Items_Into_Table()
    {
        var config = SetupServices(includeApi: true);
        await config.SaveAsync(new DevOpsConfig { Organization = "Org", Project = "Proj", PatToken = "token" });

        var backlogJson = "{\"path\":\"Area\"}";
        var statesJson = "{\"value\":[{\"name\":\"New\"}]}";
        var tagsJson = "{\"value\":[{\"name\":\"UI\"}]}";
        var wiqlJson = "{\"workItems\":[{\"id\":5}]}";
        var itemsJson = "{\"value\":[{\"id\":5,\"fields\":{\"System.Title\":\"Item\",\"System.State\":\"Active\",\"System.WorkItemType\":\"User Story\"}}]}";

        var handler = new FakeHttpMessageHandler(req =>
        {
            var url = req.RequestUri!.AbsoluteUri;
            if (url.Contains("classificationnodes/areas"))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(backlogJson) };
            if (url.Contains("states"))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(statesJson) };
            if (url.Contains("tags"))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(tagsJson) };
            if (url.Contains("wiql"))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(wiqlJson) };
            if (url.Contains("workitems"))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(itemsJson) };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        });
        var client = new HttpClient(handler);
        Services.AddSingleton(new DeploymentConfigService(new HttpClient()));
        Services.AddSingleton(sp => new DevOpsApiService(
            client,
            config,
            sp.GetRequiredService<DeploymentConfigService>(),
            sp.GetRequiredService<IStringLocalizer<DevOpsApiService>>()));

        var cut = RenderWithProvider<WorkItemSelector>();
        var field = cut.Instance.GetType().GetField("_tag", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field!.SetValue(cut.Instance, "UI");
        var method = cut.Instance.GetType().GetMethod("LoadTag", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await cut.InvokeAsync(async () => await (Task)method!.Invoke(cut.Instance, null)!);
        var setField = cut.Instance.GetType().GetField("_tagSelected", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var selected = (System.Collections.Generic.HashSet<DevOpsAssistant.Services.Models.WorkItemInfo>)setField!.GetValue(cut.Instance)!;

        Assert.Single(selected);
        Assert.Equal(5, selected.First().Id);
    }
}
