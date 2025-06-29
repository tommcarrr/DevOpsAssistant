using Bunit;
using DevOpsAssistant.Components.Apex;
using DevOpsAssistant.Components;
using DevOpsAssistant.Tests.Utils;
using ApexCharts;

namespace DevOpsAssistant.Tests.Components;

public class SimpleApexChartTests : ComponentTestBase
{
    [Fact]
    public void Wrapper_Renders_Class()
    {
        SetupServices();
        List<ApexSeries> series = [
            new() { Name = "Test", Points = [ new() { Label = "A", Value = 1 } ] }
        ];

        var cut = RenderComponent<SimpleApexChart>(p => p
            .Add(c => c.Series, series)
            .Add(c => c.Class, "test-class"));

        cut.Find("div.test-class");
    }
}
