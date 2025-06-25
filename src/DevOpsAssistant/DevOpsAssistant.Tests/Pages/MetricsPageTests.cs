using Bunit;
using System;
using System.Reflection;
using System.Collections.Generic;
using DevOpsAssistant.Pages;
using DevOpsAssistant.Services.Models;
using DevOpsAssistant.Tests.Utils;
using MudBlazor;

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
    public void BuildPrompt_Includes_Metrics()
    {
        SetupServices();

        var method = typeof(Metrics).GetMethod("BuildPrompt", BindingFlags.NonPublic | BindingFlags.Static)!;
        var periodType = typeof(Metrics).GetNestedType("PeriodMetrics", BindingFlags.NonPublic)!;
        var array = Array.CreateInstance(periodType, 1);
        var period = Activator.CreateInstance(periodType)!;
        periodType.GetProperty("End")!.SetValue(period, new DateTime(2024, 1, 1));
        periodType.GetProperty("AvgLeadTime")!.SetValue(period, 1.2);
        periodType.GetProperty("AvgCycleTime")!.SetValue(period, 2.3);
        periodType.GetProperty("Throughput")!.SetValue(period, 3);
        periodType.GetProperty("Velocity")!.SetValue(period, 4.5);
        array.SetValue(period, 0);

        var prompt = (string)method.Invoke(null, new object?[] { array })!;

        Assert.Contains("\"end\":\"2024-01-01\"", prompt);
        Assert.Contains("\"leadTime\":1.2", prompt);
        Assert.Contains("\"velocity\":4.5", prompt);
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
        compute.Invoke(metrics, new object?[] { new List<StoryMetric>(), null });

        var periodsField = type.GetField("_periods", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var list = (System.Collections.IList)periodsField.GetValue(metrics)!;
        Assert.True(list.Count > 0);
        var first = list[0];
        Assert.NotNull(first);
        var start = (DateTime)first.GetType().GetProperty("Start")!.GetValue(first)!;
        var end = (DateTime)first.GetType().GetProperty("End")!.GetValue(first)!;
        Assert.Equal(13, (end - start).TotalDays);
    }

    [Fact]
    public void ComputeBurnUp_Produces_Data()
    {
        SetupServices();

        var metrics = new TestMetrics();
        var type = typeof(Metrics);
        type.GetField("_additionalPoints", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(metrics, (double?)10);
        type.GetField("_efficiency", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(metrics, (double?)80);
        type.GetField("_errorRange", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(metrics, (double?)10);
        var compute = type.GetMethod("ComputeBurnUp", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var seriesField = type.GetField("_burnSeries", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var items = new List<StoryMetric>
        {
            new() { CreatedDate = DateTime.Today.AddDays(-3), ActivatedDate = DateTime.Today.AddDays(-2), ClosedDate = DateTime.Today.AddDays(-1), StoryPoints = 3 },
            new() { CreatedDate = DateTime.Today.AddDays(-2), ActivatedDate = DateTime.Today.AddDays(-1), ClosedDate = DateTime.Today, StoryPoints = 2 }
        };
        compute.Invoke(metrics, new object?[] { items });

        var list = (IList<ChartSeries>)seriesField.GetValue(metrics)!;
        Assert.True(list.Count > 0);
    }

    [Fact]
    public void ComputeFlow_Produces_Data()
    {
        SetupServices();

        var metrics = new TestMetrics();
        var type = typeof(Metrics);
        var compute = type.GetMethod("ComputeFlow", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var seriesField = type.GetField("_flowSeries", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var labelsField = type.GetField("_flowLabels", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var items = new List<StoryMetric>
        {
            new() { CreatedDate = DateTime.Today.AddDays(-3), ActivatedDate = DateTime.Today.AddDays(-2), ClosedDate = DateTime.Today.AddDays(-1) },
            new() { CreatedDate = DateTime.Today.AddDays(-2), ActivatedDate = DateTime.Today.AddDays(-1), ClosedDate = DateTime.Today }
        };
        compute.Invoke(metrics, new object?[] { items });

        var series = (IList<ChartSeries>)seriesField.GetValue(metrics)!;
        var labels = (string[])labelsField.GetValue(metrics)!;
        Assert.True(series.Count > 0);
        Assert.True(labels.Length > 0);
    }

    private class TestMetrics : Metrics
    {
        protected override Task OnInitializedAsync() => Task.CompletedTask;
    }
}
