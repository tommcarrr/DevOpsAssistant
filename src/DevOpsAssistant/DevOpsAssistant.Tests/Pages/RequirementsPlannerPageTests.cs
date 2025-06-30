using System.Reflection;
using Bunit;
using System.Net;
using System.Collections.Generic;
using DevOpsAssistant.Pages;
using DevOpsAssistant.Tests.Utils;
using DevOpsAssistant.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace DevOpsAssistant.Tests.Pages;

public class RequirementsPlannerPageTests : ComponentTestBase
{
    [Fact]
    public void Planner_Renders_With_PopoverProvider()
    {
        SetupServices(includeApi: true);
        var exception = Record.Exception(() => RenderWithProvider<TestPage>());
        Assert.Null(exception);
    }

    [Fact]
    public void ImportPlan_Parses_Json()
    {
        SetupServices(includeApi: true);
        var cut = RenderWithProvider<TestPage>();
        var responseField = typeof(RequirementsPlanner).GetField("_responseText", BindingFlags.NonPublic | BindingFlags.Instance)!;
        responseField.SetValue(
            cut.Instance,
            "{\"Epics\":[{\"Title\":\"E\",\"Description\":\"D\",\"Features\":[{\"Title\":\"F\",\"Description\":\"FD\",\"Stories\":[{\"Title\":\"S\",\"Description\":\"SD\",\"AcceptanceCriteria\":\"AC\"}]}]}]}"
                .Replace("Epics", "epics")
                .Replace("Title", "title")
                .Replace("Description", "description")
                .Replace("Features", "features")
                .Replace("Stories", "stories")
                .Replace("AcceptanceCriteria", "acceptanceCriteria")
        );
        var method = typeof(RequirementsPlanner).GetMethod("ImportPlan", BindingFlags.NonPublic | BindingFlags.Instance)!;
        cut.InvokeAsync(() => method.Invoke(cut.Instance, null));
        var planField = typeof(RequirementsPlanner).GetField("_plan", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var plan = planField.GetValue(cut.Instance);
        Assert.NotNull(plan);
        var epics = (System.Collections.ICollection?)plan!.GetType().GetProperty("Epics")!.GetValue(plan);
        Assert.Equal(1, epics?.Count);
    }

    [Fact]
    public void ImportPlan_Parses_Json_With_CodeFence()
    {
        SetupServices(includeApi: true);
        var cut = RenderWithProvider<TestPage>();
        var responseField = typeof(RequirementsPlanner).GetField("_responseText", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var json = "{\"epics\":[{\"title\":\"E\",\"description\":\"D\",\"features\":[{\"title\":\"F\",\"description\":\"FD\",\"stories\":[{\"title\":\"S\",\"description\":\"SD\",\"acceptanceCriteria\":\"AC\"}]}]}]}";
        responseField.SetValue(cut.Instance, $"```json\n{json}\n```\nExtra");
        var method = typeof(RequirementsPlanner).GetMethod("ImportPlan", BindingFlags.NonPublic | BindingFlags.Instance)!;
        cut.InvokeAsync(() => method.Invoke(cut.Instance, null));
        var planField = typeof(RequirementsPlanner).GetField("_plan", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var plan = planField.GetValue(cut.Instance);
        Assert.NotNull(plan);
        var epics = (System.Collections.ICollection?)plan!.GetType().GetProperty("Epics")!.GetValue(plan);
        Assert.Equal(1, epics?.Count);
    }

    [Fact]
    public async Task DeleteCreatedItems_Removes_All_In_Reverse_Order()
    {
        var config = SetupServices(includeApi: true);
        await config.SaveAsync(new DevOpsConfig { Organization = "Org", Project = "Proj", PatToken = "token" });
        var deleted = new List<int>();
        var handler = new FakeHttpMessageHandler(req =>
        {
            var idPart = req.RequestUri!.Segments.Last();
            deleted.Add(int.Parse(idPart.Split('?')[0]));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        });
        var client = new HttpClient(handler);
        Services.AddSingleton(new DeploymentConfigService(new HttpClient()));
        Services.AddSingleton(sp => new DevOpsApiService(
            client,
            config,
            sp.GetRequiredService<DeploymentConfigService>(),
            sp.GetRequiredService<IStringLocalizer<DevOpsApiService>>()));

        var cut = RenderWithProvider<TestPage>();
        var field = typeof(RequirementsPlanner).GetField("_createdItems", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var list = new List<int> { 1, 2, 3 };
        field.SetValue(cut.Instance, list);

        var method = typeof(RequirementsPlanner).GetMethod("DeleteCreatedItems", BindingFlags.NonPublic | BindingFlags.Instance)!;
        await cut.InvokeAsync(() => (Task)method.Invoke(cut.Instance, null)!);

        Assert.Empty(list);
        Assert.Equal(new[] { 3, 2, 1 }, deleted);
    }


    private class TestPage : RequirementsPlanner
    {
        protected override Task OnInitializedAsync() => Task.CompletedTask;
    }
}
