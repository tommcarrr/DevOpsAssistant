using Bunit;
using DevOpsAssistant.Pages;
using DevOpsAssistant.Services;
using DevOpsAssistant.Tests.Utils;

namespace DevOpsAssistant.Tests.Pages;

public class WorkItemsPageTests : ComponentTestBase
{
    [Fact]
    public void WorkItems_Renders_With_PopoverProvider()
    {
        SetupServices(includeApi: true);

        var exception = Record.Exception(() => RenderWithProvider<TestWorkItems>());
        Assert.Null(exception);
    }

    private class TestWorkItems : WorkItems
    {
        protected override Task OnInitializedAsync()
        {
            return Task.CompletedTask;
        }
    }

    // Rendering uses ComponentTestBase.RenderWithProvider
}
