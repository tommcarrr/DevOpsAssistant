using Bunit;
using DevOpsAssistant.Pages;
using DevOpsAssistant.Tests.Utils;

namespace DevOpsAssistant.Tests.Pages;

public class HomePageTests : ComponentTestBase
{
    [Fact]
    public void Home_Shows_Description()
    {
        SetupServices();
        var cut = RenderComponent<Home>();

        Assert.Contains("DevOpsAssistant provides", cut.Markup);
    }
}
