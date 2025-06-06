using System.Reflection;
using Bunit;
using DevOpsAssistant.Components;
using DevOpsAssistant.Services;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace DevOpsAssistant.Tests;

public class SettingsDialogTests : TestContext
{
    [Fact]
    public async Task SettingsDialog_Shows_Config_Values()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        var storage = new FakeLocalStorageService();
        var configService = new DevOpsConfigService(storage);
        await configService.SaveAsync(new DevOpsConfig { Organization = "Org" });
        Services.AddSingleton(configService);

        var cut = RenderComponent<SettingsDialog>();
        var modelField = cut.Instance.GetType().GetField("_model", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var model = (DevOpsConfig)modelField.GetValue(cut.Instance)!;

        Assert.Equal("Org", model.Organization);
    }
}