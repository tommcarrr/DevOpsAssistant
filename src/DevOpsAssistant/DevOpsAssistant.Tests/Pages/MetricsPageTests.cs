using Bunit;
using System;
using System.Reflection;
using System.Collections.Generic;
using DevOpsAssistant.Pages;
using DevOpsAssistant.Services.Models;
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
        periodType.GetProperty("End")!.SetValue(period, new DateTime(2024, 1, 1));
        periodType.GetProperty("AvgLeadTime")!.SetValue(period, 1.0);
        periodType.GetProperty("AvgCycleTime")!.SetValue(period, 2.0);
        periodType.GetProperty("Throughput")!.SetValue(period, 3);
        periodType.GetProperty("Velocity")!.SetValue(period, 4.0);
        array.SetValue(period, 0);

        var csv = (string)method.Invoke(null, new object?[] { array })!;

        Assert.Contains("Velocity", csv);
        Assert.Contains("4.0", csv);
    }

    [Fact]
    public void Fortnight_Mode_Uses_Two_Week_Period()
    {
        SetupServices();

        var metrics = new TestMetrics();
        var type = typeof(Metrics);
        var compute = type.GetMethod("ComputePeriods", BindingFlags.NonPublic | BindingFlags.Instance)!;
        type.GetField("_mode", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(metrics, AggregateMode.Fortnight);
        type.GetField("_startDate", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(metrics, DateTime.Today);
        compute.Invoke(metrics, new object?[] { new List<StoryMetric>() });

        var periodsField = type.GetField("_periods", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var list = (System.Collections.IList)periodsField.GetValue(metrics)!;
        Assert.True(list.Count > 0);
        var first = list[0];
        var start = (DateTime)first.GetType().GetProperty("Start")!.GetValue(first)!;
        var end = (DateTime)first.GetType().GetProperty("End")!.GetValue(first)!;
        Assert.Equal(13, (end - start).TotalDays);
    }

    private class TestMetrics : Metrics
    {
        protected override Task OnInitializedAsync() => Task.CompletedTask;
    }
}
