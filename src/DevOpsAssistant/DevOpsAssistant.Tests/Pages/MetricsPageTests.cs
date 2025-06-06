using Bunit;
using System;
using System.Reflection;
using System.Collections.Generic;
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
        Assert.Contains("Export CSV", cut.Markup);
    }

    [Fact]
    public void BuildCsv_Produces_Csv_String()
    {
        SetupServices();

        var method = typeof(Metrics).GetMethod("BuildCsv", BindingFlags.NonPublic | BindingFlags.Static)!;
        var periodType = typeof(Metrics).GetNestedType("PeriodMetrics", BindingFlags.NonPublic)!;
        var array = Array.CreateInstance(periodType, 1);
        var period = Activator.CreateInstance(periodType)!;
        periodType.GetProperty("End")!.SetValue(period, new DateTime(2024,1,1));
        periodType.GetProperty("AvgLeadTime")!.SetValue(period, 1.0);
        periodType.GetProperty("AvgCycleTime")!.SetValue(period, 2.0);
        periodType.GetProperty("Throughput")!.SetValue(period, 3);
        periodType.GetProperty("Velocity")!.SetValue(period, 4.0);
        array.SetValue(period, 0);

        var csv = (string)method.Invoke(null, new object?[] { array })!;

        Assert.Contains("Velocity", csv);
        Assert.Contains("4.0", csv);
    }

    private class TestMetrics : Metrics
    {
        protected override Task OnInitializedAsync() => Task.CompletedTask;
    }
}
