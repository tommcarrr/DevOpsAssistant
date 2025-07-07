using System.Net;
using System.Reflection;
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
        cut.SetParametersAndRender(parameters => parameters.Add(c => c.UseIteration, true)
                                                 .Add(c => c.StateKey, "test"));
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
        cut.SetParametersAndRender(p => p.Add(c => c.StateKey, "test"));
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
        var field = cut.Instance.GetType().GetField("_tag", BindingFlags.NonPublic | BindingFlags.Instance);
        field!.SetValue(cut.Instance, "UI");
        var method = cut.Instance.GetType().GetMethod("LoadTag", BindingFlags.NonPublic | BindingFlags.Instance);
        await cut.InvokeAsync(async () => await (Task)method!.Invoke(cut.Instance, null)!);
        var setField = cut.Instance.GetType().GetField("_tagSelected", BindingFlags.NonPublic | BindingFlags.Instance);
        var selected = (HashSet<WorkItemInfo>)setField!.GetValue(cut.Instance)!;

        Assert.Single(selected);
        Assert.Equal(5, selected.First().Id);
    }

    [Fact]
    public async Task Query_Loads_Items_Into_Table()
    {
        var config = SetupServices(includeApi: true);
        await config.SaveAsync(new DevOpsConfig { Organization = "Org", Project = "Proj", PatToken = "token" });

        var backlogJson = "{\"path\":\"Area\"}";
        var statesJson = "{\"value\":[{\"name\":\"New\"}]}";
        var queriesJson = "{\"value\":[{\"id\":\"1\",\"name\":\"MyQuery\",\"path\":\"Shared Queries/MyQuery\",\"isFolder\":false}]}";
        var wiqlJson = "{\"workItems\":[{\"id\":10}]}";
        var itemsJson = "{\"value\":[{\"id\":10,\"fields\":{\"System.Title\":\"Q Item\",\"System.State\":\"Active\",\"System.WorkItemType\":\"User Story\"}}]}";

        var handler = new FakeHttpMessageHandler(req =>
        {
            var url = req.RequestUri!.AbsoluteUri;
            if (url.Contains("classificationnodes/areas"))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(backlogJson) };
            if (url.Contains("states"))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(statesJson) };
            if (url.Contains("queries"))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(queriesJson) };
            if (url.Contains("wiql/1"))
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
        var queryField = cut.Instance.GetType().GetField("_query", BindingFlags.NonPublic | BindingFlags.Instance);
        queryField!.SetValue(cut.Instance, new QueryInfo { Id = "1", Name = "MyQuery", Path = "Shared Queries/MyQuery" });
        var method = cut.Instance.GetType().GetMethod("LoadQuery", BindingFlags.NonPublic | BindingFlags.Instance);
        await cut.InvokeAsync(async () => await (Task)method!.Invoke(cut.Instance, null)!);
        var setField = cut.Instance.GetType().GetField("_querySelected", BindingFlags.NonPublic | BindingFlags.Instance);
        var selected = (HashSet<WorkItemInfo>)setField!.GetValue(cut.Instance)!;

        Assert.Single(selected);
        Assert.Equal(10, selected.First().Id);
    }

    [Fact]
    public async Task Shows_Loading_Indicator_When_Loading()
    {
        var config = SetupServices(includeApi: true);
        await config.SaveAsync(new DevOpsConfig { Organization = "Org", Project = "Proj", PatToken = "token" });

        var backlogJson = "{\"path\":\"Area\"}";
        var statesJson = "{\"value\":[{\"name\":\"New\"}]}";
        var tagsJson = "{\"value\":[{\"name\":\"UI\"}]}";
        var handler = new FakeHttpMessageHandler(req =>
        {
            var url = req.RequestUri!.AbsoluteUri;
            if (url.Contains("classificationnodes/areas"))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(backlogJson) };
            if (url.Contains("states"))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(statesJson) };
            if (url.Contains("tags"))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(tagsJson) };
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
        var loadingField = cut.Instance.GetType().GetField("_loading", BindingFlags.NonPublic | BindingFlags.Instance);
        loadingField!.SetValue(cut.Instance, true);
        cut.Render();

        Assert.Contains("mud-progress-circular", cut.Markup);
    }
}
