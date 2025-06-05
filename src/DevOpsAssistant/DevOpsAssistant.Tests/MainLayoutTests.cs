using Bunit;
using DevOpsAssistant.Layout;
using MudBlazor.Services;
using Xunit;

namespace DevOpsAssistant.Tests;

public class MainLayoutTests : TestContext
{
    [Fact]
    public void Layout_Has_PopoverProvider()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = RenderComponent<MainLayout>();

        // Verify that the MudPopoverProvider is present in the rendered markup
        cut.Markup.Contains("mud-popover-provider");
    }
}
