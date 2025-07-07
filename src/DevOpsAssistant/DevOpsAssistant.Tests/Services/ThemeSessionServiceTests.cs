using DevOpsAssistant.Services;
using DevOpsAssistant.Tests.Utils;

namespace DevOpsAssistant.Tests.Services;

public class ThemeSessionServiceTests
{
    [Fact(Skip = "Causes hang in test runner")]
    public async Task ToggleDoom_Toggles_State_And_Fires_Event()
    {
        var js = new FakeJSRuntime();
        var service = new ThemeSessionService(js);
        var fired = false;
        service.ThemeChanged += () => fired = true;

        await service.ToggleDoom();

        Assert.True(service.IsDoom);
        Assert.True(fired);
        Assert.Contains("themeShortcut.setDoom", js.InvokedIdentifiers);
    }
}
