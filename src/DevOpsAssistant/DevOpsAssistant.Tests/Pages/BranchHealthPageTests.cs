using Bunit;
using DevOpsAssistant.Pages;
using DevOpsAssistant.Tests.Utils;

namespace DevOpsAssistant.Tests.Pages;

public class BranchHealthPageTests : ComponentTestBase
{
    [Fact]
    public void BranchHealth_Renders_With_PopoverProvider()
    {
        SetupServices(includeApi: true);

        var exception = Record.Exception(() => RenderWithProvider<TestPage>());
        Assert.Null(exception);
    }

    private class TestPage : BranchHealth
    {
        protected override Task OnInitializedAsync() => Task.CompletedTask;
    }
}
