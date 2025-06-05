using Bunit;
using DevOpsAssistant.Layout;
using MudBlazor.Services;
using DevOpsAssistant.Services;
using DevOpsAssistant.Tests;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DevOpsAssistant.Tests;

public class MainLayoutTests : TestContext
{
    [Fact]
    public void Layout_Has_PopoverProvider()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(new DevOpsConfigService(new FakeLocalStorageService()));

        var cut = RenderComponent<MainLayout>();

        // Verify that the MudPopoverProvider is present in the rendered markup
        cut.Markup.Contains("mud-popover-provider");
    }

    [Fact]
    public async Task Layout_Uses_DarkMode_From_Config()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        var storage = new FakeLocalStorageService();
        var configService = new DevOpsConfigService(storage);
        await configService.SaveAsync(new DevOpsConfig { DarkMode = true });
        Services.AddSingleton(configService);

        var cut = RenderComponent<MainLayout>();

        Assert.Contains("--mud-native-html-color-scheme: dark", cut.Markup);
    }
}
