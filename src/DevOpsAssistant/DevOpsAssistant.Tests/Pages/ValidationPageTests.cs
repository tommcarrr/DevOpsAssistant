using Bunit;
using DevOpsAssistant.Pages;
using DevOpsAssistant.Services;
using DevOpsAssistant.Tests.Utils;

namespace DevOpsAssistant.Tests.Pages;

public class ValidationPageTests : ComponentTestBase
{
    [Fact]
    public void Validation_Renders_With_PopoverProvider()
    {
        SetupServices(includeApi: true);

        var exception = Record.Exception(() => RenderWithProvider<TestPage>());
        Assert.Null(exception);
    }

    private class TestPage : Validation
    {
        protected override Task OnInitializedAsync()
        {
            return Task.CompletedTask;
        }
    }

    // Rendering uses ComponentTestBase.RenderWithProvider
}
