using System.Reflection;
using Bunit;
using DevOpsAssistant.Components;
using DevOpsAssistant.Services;
using DevOpsAssistant.Tests.Utils;

namespace DevOpsAssistant.Tests.Components;

public class SettingsDialogTests : ComponentTestBase
{
    [Fact]
    public async Task SettingsDialog_Shows_Config_Values()
    {
        var config = SetupServices();
        await config.SaveAsync(new DevOpsConfig { Organization = "Org" });

        var cut = RenderComponent<SettingsDialog>();
        var modelField = cut.Instance.GetType().GetField("_model", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var model = (DevOpsConfig)modelField.GetValue(cut.Instance)!;

        Assert.Equal("Org", model.Organization);
    }
}