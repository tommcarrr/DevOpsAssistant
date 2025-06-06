using Bunit;
using DevOpsAssistant.Pages;
using MudBlazor.Services;

namespace DevOpsAssistant.Tests;

public class HomePageTests : TestContext
{
    [Fact]
    public void Home_Has_WorkItems_Link()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = RenderComponent<Home>();

        Assert.Contains("href=\"epics-features\"", cut.Markup);
        Assert.Contains("href=\"validation\"", cut.Markup);
    }
}