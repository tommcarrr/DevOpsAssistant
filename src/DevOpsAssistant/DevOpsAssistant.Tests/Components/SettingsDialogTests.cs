using System.Reflection;
using Bunit;
using DevOpsAssistant.Components;
using DevOpsAssistant.Services;
using DevOpsAssistant.Services.Models;
using DevOpsAssistant.Tests.Utils;

namespace DevOpsAssistant.Tests.Components;

public class SettingsDialogTests : ComponentTestBase
{
    [Fact]
    public async Task SettingsDialog_Shows_Config_Values()
    {
        var config = SetupServices();
        await config.SaveAsync(new DevOpsConfig
        {
            Organization = "Org",
            MainBranch = "main",
            DefaultStates = "Active",
            ReleaseNotesTreeView = true,
            Rules = new ValidationRules
            {
                Bug = new BugRules
                {
                    IncludeReproSteps = false,
                    IncludeSystemInfo = false,
                    HasStoryPoints = false
                }
            }
        });

        var cut = RenderComponent<SettingsDialog>();
        var modelField = cut.Instance.GetType().GetField("_model", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var model = (DevOpsConfig)modelField.GetValue(cut.Instance)!;

        Assert.Equal("Org", model.Organization);
        Assert.Equal("main", model.MainBranch);
        Assert.Equal("Active", model.DefaultStates);
        Assert.True(model.ReleaseNotesTreeView);
        Assert.False(model.Rules.Bug.IncludeReproSteps);
        Assert.False(model.Rules.Bug.IncludeSystemInfo);
        Assert.False(model.Rules.Bug.HasStoryPoints);
    }

    [Fact]
    public async Task Save_Does_Not_Change_Current_Project()
    {
        var config = SetupServices();
        await config.AddProjectAsync("One");
        await config.AddProjectAsync("Two");
        await config.SelectProjectAsync("One");

        var cut = RenderComponent<SettingsDialog>();
        var change = typeof(SettingsDialog).GetMethod("OnProjectChanged", BindingFlags.NonPublic | BindingFlags.Instance)!;
        await cut.InvokeAsync(() => change.Invoke(cut.Instance, new object[] { "Two" }));

        var save = typeof(SettingsDialog).GetMethod("Save", BindingFlags.NonPublic | BindingFlags.Instance)!;
        await cut.InvokeAsync(() => (Task)save.Invoke(cut.Instance, null)!);

        Assert.Equal("One", config.CurrentProject.Name);
    }
}