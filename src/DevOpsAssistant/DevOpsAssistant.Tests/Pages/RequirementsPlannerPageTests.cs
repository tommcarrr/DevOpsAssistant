using System.Reflection;
using Bunit;
using DevOpsAssistant.Pages;
using DevOpsAssistant.Tests.Utils;

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
        responseField.SetValue(cut.Instance, "{\"Epics\":[{\"Title\":\"E\",\"Description\":\"D\",\"Features\":[]}]}".Replace("Epics", "epics").Replace("Title", "title").Replace("Description", "description").Replace("Features", "features"));
        var method = typeof(RequirementsPlanner).GetMethod("ImportPlan", BindingFlags.NonPublic | BindingFlags.Instance)!;
        cut.InvokeAsync(() => method.Invoke(cut.Instance, null));
        var planField = typeof(RequirementsPlanner).GetField("_plan", BindingFlags.NonPublic | BindingFlags.Instance)!;
        Assert.NotNull(planField.GetValue(cut.Instance));
    }

    private class TestPage : RequirementsPlanner
    {
        protected override Task OnInitializedAsync() => Task.CompletedTask;
    }
}
