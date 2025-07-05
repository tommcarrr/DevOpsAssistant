using Bunit;
using DevOpsAssistant.Pages;
using DevOpsAssistant.Services.Models;
using DevOpsAssistant.Tests.Utils;
using DevOpsAssistant.Services;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace DevOpsAssistant.Tests.Pages;

public class ProjectSettingsPageTests : ComponentTestBase
{
    [Fact]
    public async Task Disabled_Standard_Tooltip_Lists_Incompatibilities()
    {
        var config = SetupServices();
        await config.AddProjectAsync("Demo");
        await config.SaveCurrentAsync("Demo", new DevOpsConfig(), "");

        var cut = RenderComponent<ProjectSettings>(p => p.Add(c => c.ProjectName, "Demo"));
        var selectedField = cut.Instance.GetType().GetField("_selectedStandards", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        selectedField.SetValue(cut.Instance, new HashSet<string> { "ScrumUserStory" });
        var method = cut.Instance.GetType().GetMethod("GetStandardTooltip", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var option = StandardsCatalog.Options.First(o => o.Id == "JobStory");
        var text = (string)method.Invoke(cut.Instance, [option])!;

        Assert.Contains("Incompatible", text);
        Assert.Contains("Scrum User Story", text);
    }
}
