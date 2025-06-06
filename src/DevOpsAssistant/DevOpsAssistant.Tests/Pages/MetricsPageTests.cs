using Bunit;
using DevOpsAssistant.Pages;
using DevOpsAssistant.Tests.Utils;

namespace DevOpsAssistant.Tests.Pages;

public class MetricsPageTests : ComponentTestBase
{
    [Fact]
    public void Metrics_Renders()
    {
        SetupServices(includeApi: true);

        var cut = RenderWithProvider<TestMetrics>();

        Assert.Contains("Load", cut.Markup);
    }

    private class TestMetrics : Metrics
    {
        protected override Task OnInitializedAsync() => Task.CompletedTask;
    }
}
