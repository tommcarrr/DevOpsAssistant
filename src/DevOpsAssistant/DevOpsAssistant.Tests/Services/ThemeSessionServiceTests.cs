using Bunit;
using DevOpsAssistant.Services;
using Xunit;

namespace DevOpsAssistant.Tests.Services;

public class ThemeSessionServiceTests
{
    [Fact]
    public async Task ToggleDoom_Toggles_State_And_Fires_Event()
    {
        var js = new BunitJSInterop();
        js.SetupVoid("themeShortcut.setDoom", _ => true);
        var service = new ThemeSessionService(js.JSRuntime);
        var fired = false;
        service.ThemeChanged += () => fired = true;

        await service.ToggleDoom();

        Assert.True(service.IsDoom);
        Assert.True(fired);
        js.VerifyInvoke("themeShortcut.setDoom");
    }
}
