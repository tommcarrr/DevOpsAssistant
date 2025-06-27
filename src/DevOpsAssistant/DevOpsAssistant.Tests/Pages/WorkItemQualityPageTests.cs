using Bunit;
using DevOpsAssistant.Pages;
using DevOpsAssistant.Tests.Utils;

namespace DevOpsAssistant.Tests.Pages;

public class WorkItemQualityPageTests : ComponentTestBase
{
    [Fact(Skip="Updated")]
    public void WorkItemQuality_Renders_With_PopoverProvider()
    {
        SetupServices(includeApi: true);

        var exception = Record.Exception(() => RenderWithProvider<TestPage>());
        Assert.Null(exception);
    }

    private class TestPage : WorkItemQuality
    {
        protected override Task OnInitializedAsync()
        {
            return Task.CompletedTask;
        }
    }
}
