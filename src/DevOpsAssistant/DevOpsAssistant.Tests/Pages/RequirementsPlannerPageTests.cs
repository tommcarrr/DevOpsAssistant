using System.Reflection;
using Bunit;
using System.Net;
using DevOpsAssistant.Pages;
using DevOpsAssistant.Tests.Utils;
using DevOpsAssistant.Services;
using DevOpsAssistant.Utils;
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
    public void ImportPlan_Adds_AiGeneratedTag()
    {
        SetupServices(includeApi: true);
        var cut = RenderWithProvider<TestPage>();
        var responseField = typeof(RequirementsPlanner).GetField("_responseText", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var json = "{\"epics\":[{\"title\":\"E\",\"description\":\"D\",\"features\":[{\"title\":\"F\",\"description\":\"FD\",\"stories\":[{\"title\":\"S\",\"description\":\"SD\",\"acceptanceCriteria\":\"AC\"}]}]}]}";
        responseField.SetValue(cut.Instance, json);
        var method = typeof(RequirementsPlanner).GetMethod("ImportPlan", BindingFlags.NonPublic | BindingFlags.Instance)!;
        cut.InvokeAsync(() => method.Invoke(cut.Instance, null));

        var planField = typeof(RequirementsPlanner).GetField("_plan", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var plan = planField.GetValue(cut.Instance)!;
        var epicType = typeof(RequirementsPlanner).GetNestedType("Epic", BindingFlags.NonPublic)!;
        var featureType = typeof(RequirementsPlanner).GetNestedType("Feature", BindingFlags.NonPublic)!;
        var storyType = typeof(RequirementsPlanner).GetNestedType("Story", BindingFlags.NonPublic)!;

        var epic = ((IEnumerable<object>)plan.GetType().GetProperty("Epics")!.GetValue(plan)!).Cast<object>().First();
        var feature = ((IEnumerable<object>)epicType.GetProperty("Features")!.GetValue(epic)!).Cast<object>().First();
        var story = ((IEnumerable<object>)featureType.GetProperty("Stories")!.GetValue(feature)!).Cast<object>().First();

        Assert.Contains(AppConstants.AiGeneratedTag, ((IEnumerable<string>)epicType.GetProperty("Tags")!.GetValue(epic)!));
        Assert.Contains(AppConstants.AiGeneratedTag, ((IEnumerable<string>)featureType.GetProperty("Tags")!.GetValue(feature)!));
        Assert.Contains(AppConstants.AiGeneratedTag, ((IEnumerable<string>)storyType.GetProperty("Tags")!.GetValue(story)!));
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

    [Fact]
    public void RemoveEpic_Removes_From_List()
    {
        SetupServices(includeApi: true);
        var cut = RenderWithProvider<TestPage>();
        var planType = typeof(RequirementsPlanner).GetNestedType("Plan", BindingFlags.NonPublic)!;
        var epicType = typeof(RequirementsPlanner).GetNestedType("Epic", BindingFlags.NonPublic)!;
        var epic = Activator.CreateInstance(epicType)!;
        var plan = Activator.CreateInstance(planType)!;
        var epicsProp = planType.GetProperty("Epics")!;
        var list = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(epicType))!;
        list.Add(epic);
        epicsProp.SetValue(plan, list);
        var planField = typeof(RequirementsPlanner).GetField("_plan", BindingFlags.NonPublic | BindingFlags.Instance)!;
        planField.SetValue(cut.Instance, plan);

        var remove = typeof(RequirementsPlanner).GetMethod("RemoveEpic", BindingFlags.NonPublic | BindingFlags.Instance)!;
        cut.InvokeAsync(() => remove.Invoke(cut.Instance, [epic]));

        Assert.Empty((System.Collections.IList)epicsProp.GetValue(plan)!);
    }

    [Fact]
    public void OnDropOnEpic_Moves_Feature()
    {
        SetupServices(includeApi: true);
        var cut = RenderWithProvider<TestPage>();
        var planType = typeof(RequirementsPlanner).GetNestedType("Plan", BindingFlags.NonPublic)!;
        var epicType = typeof(RequirementsPlanner).GetNestedType("Epic", BindingFlags.NonPublic)!;
        var featureType = typeof(RequirementsPlanner).GetNestedType("Feature", BindingFlags.NonPublic)!;
        var epic1 = Activator.CreateInstance(epicType)!;
        var epic2 = Activator.CreateInstance(epicType)!;
        var feature = Activator.CreateInstance(featureType)!;
        var featuresProp = epicType.GetProperty("Features")!;
        var list1 = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(featureType))!;
        list1.Add(feature);
        featuresProp.SetValue(epic1, list1);
        var list2 = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(featureType))!;
        featuresProp.SetValue(epic2, list2);
        var plan = Activator.CreateInstance(planType)!;
        var epicsProp = planType.GetProperty("Epics")!;
        var epics = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(epicType))!;
        epics.Add(epic1);
        epics.Add(epic2);
        epicsProp.SetValue(plan, epics);
        var planField = typeof(RequirementsPlanner).GetField("_plan", BindingFlags.NonPublic | BindingFlags.Instance)!;
        planField.SetValue(cut.Instance, plan);

        var drag = typeof(RequirementsPlanner).GetMethod("OnDragStart", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var drop = typeof(RequirementsPlanner).GetMethod("OnDropOnEpic", BindingFlags.NonPublic | BindingFlags.Instance)!;
        cut.InvokeAsync(() => drag.Invoke(cut.Instance, [feature]));
        cut.InvokeAsync(() => drop.Invoke(cut.Instance, [epic2]));

        Assert.Empty((System.Collections.IList)featuresProp.GetValue(epic1)!);
        Assert.Single((System.Collections.IList)featuresProp.GetValue(epic2)!);
    }


    private class TestPage : RequirementsPlanner
    {
        protected override Task OnInitializedAsync() => Task.CompletedTask;
    }
}
