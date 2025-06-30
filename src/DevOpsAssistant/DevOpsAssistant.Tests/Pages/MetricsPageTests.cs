using Bunit;
using System;
using System.Reflection;
using System.Collections.Generic;
using DevOpsAssistant.Pages;
using DevOpsAssistant.Services.Models;
using DevOpsAssistant.Tests.Utils;
using DevOpsAssistant.Utils;
using MudBlazor;
using DevOpsAssistant.Components.Apex;
using ApexCharts;

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
        periodType.GetProperty("AvgWip")!.SetValue(period, 1.5);
        periodType.GetProperty("SprintEfficiency")!.SetValue(period, 80.0);
        array.SetValue(period, 0);

        var prompt = (string)method.Invoke(null, new object?[] { array, OutputFormat.Markdown })!;

        Assert.Contains("Agile Delivery Metrics Report Template", prompt);
        Assert.Contains("\"end\":\"2024-01-01\"", prompt);
        Assert.Contains("\"leadTime\":1.2", prompt);
        Assert.Contains("\"velocity\":4.5", prompt);
        Assert.Contains("\"avgWip\":1.5", prompt);
        Assert.Contains("\"sprintEfficiency\":80", prompt);
    }

    [Fact]
    public void ComputeBurnUp_DoesNot_Aggregate_Labels()
    {
        SetupServices();

        var metrics = new TestMetrics();
        var type = typeof(Metrics);
        type.GetField("_additionalPoints", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(metrics, (double?)10);
        type.GetField("_efficiency", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(metrics, (double?)80);
        type.GetField("_errorRange", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(metrics, (double?)10);
        var compute = type.GetMethod("ComputeBurnUp", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var labelsField = type.GetField("_burnLabels", BindingFlags.NonPublic | BindingFlags.Instance)!;
        List<StoryMetric> items = [
            new() { CreatedDate = DateTime.Today.AddDays(-3), ActivatedDate = DateTime.Today.AddDays(-2), ClosedDate = DateTime.Today.AddDays(-1), StoryPoints = 3 },
            new() { CreatedDate = DateTime.Today.AddDays(-2), ActivatedDate = DateTime.Today.AddDays(-1), ClosedDate = DateTime.Today, StoryPoints = 2 }
        ];
        compute.Invoke(metrics, new object?[] { items });

        var labels = (string[])labelsField.GetValue(metrics)!;
        Assert.All(labels, l => Assert.False(string.IsNullOrEmpty(l)));
    }

    [Fact]
    public void ComputeBurnUp_Adds_Trendline()
    {
        SetupServices();

        var metrics = new TestMetrics();
        var type = typeof(Metrics);
        type.GetField("_additionalPoints", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(metrics, (double?)10);
        type.GetField("_efficiency", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(metrics, (double?)80);
        type.GetField("_errorRange", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(metrics, (double?)10);
        var compute = type.GetMethod("ComputeBurnUp", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var seriesField = type.GetField("_burnApex", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var optionsField = type.GetField("_burnOptions", BindingFlags.NonPublic | BindingFlags.Instance)!;
        List<StoryMetric> items = [
            new() { CreatedDate = DateTime.Today.AddDays(-3), ActivatedDate = DateTime.Today.AddDays(-2), ClosedDate = DateTime.Today.AddDays(-1), StoryPoints = 3 },
            new() { CreatedDate = DateTime.Today.AddDays(-2), ActivatedDate = DateTime.Today.AddDays(-1), ClosedDate = DateTime.Today, StoryPoints = 2 }
        ];
        compute.Invoke(metrics, new object?[] { items });

        var series = (List<ApexSeries>)seriesField.GetValue(metrics)!;
        Assert.Contains(series, s => s.Name == "Trend");
        var options = (ApexChartOptions<ChartPoint>)optionsField.GetValue(metrics)!;
        var dashes = (List<double>)options.Stroke!.DashArray!;
        Assert.True(dashes[series.FindIndex(s => s.Name == "Trend")] > 0);
    }

    [Fact]
    public void ComputeBurnUp_Trendline_Stops_With_Actual_Data()
    {
        SetupServices();

        var metrics = new TestMetrics();
        var type = typeof(Metrics);
        type.GetField("_additionalPoints", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(metrics, (double?)10);
        type.GetField("_efficiency", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(metrics, (double?)80);
        type.GetField("_errorRange", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(metrics, (double?)10);
        var compute = type.GetMethod("ComputeBurnUp", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var seriesField = type.GetField("_burnApex", BindingFlags.NonPublic | BindingFlags.Instance)!;
        List<StoryMetric> items = [
            new() { CreatedDate = DateTime.Today.AddDays(-3), ActivatedDate = DateTime.Today.AddDays(-2), ClosedDate = DateTime.Today.AddDays(-1), StoryPoints = 3 },
            new() { CreatedDate = DateTime.Today.AddDays(-2), ActivatedDate = DateTime.Today.AddDays(-1), ClosedDate = DateTime.Today, StoryPoints = 2 }
        ];
        compute.Invoke(metrics, new object?[] { items });

        var series = (List<ApexSeries>)seriesField.GetValue(metrics)!;
        var trend = series.First(s => s.Name == "Trend").Points;

        Assert.Equal(2, trend.Take(2).Count(p => p.Value.HasValue));
        Assert.All(trend.Skip(2), p => Assert.Null(p.Value));
    }

    [Fact]
    public void ComputeBurnUp_Trendline_Starts_At_Zero_And_Ends_With_Total()
    {
        SetupServices();

        var metrics = new TestMetrics();
        var type = typeof(Metrics);
        var compute = type.GetMethod("ComputeBurnUp", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var seriesField = type.GetField("_burnApex", BindingFlags.NonPublic | BindingFlags.Instance)!;
        List<StoryMetric> items = [
            new() { CreatedDate = DateTime.Today.AddDays(-2), ActivatedDate = DateTime.Today.AddDays(-1), ClosedDate = DateTime.Today.AddDays(-1), StoryPoints = 3 },
            new() { CreatedDate = DateTime.Today.AddDays(-1), ActivatedDate = DateTime.Today, ClosedDate = DateTime.Today, StoryPoints = 2 }
        ];
        compute.Invoke(metrics, new object?[] { items });

        var series = (List<ApexSeries>)seriesField.GetValue(metrics)!;
        var trend = series.First(s => s.Name == "Trend").Points;

        Assert.Equal(0, trend[0].Value);
        Assert.Equal(5, trend[1].Value);
    }

    [Fact]
    public void ComputeFlow_DoesNot_Aggregate_Labels()
    {
        SetupServices();

        var metrics = new TestMetrics();
        var type = typeof(Metrics);
        var compute = type.GetMethod("ComputeFlow", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var labelsField = type.GetField("_flowLabels", BindingFlags.NonPublic | BindingFlags.Instance)!;
        List<StoryMetric> items = [
            new() { CreatedDate = DateTime.Today.AddDays(-3), ActivatedDate = DateTime.Today.AddDays(-2), ClosedDate = DateTime.Today.AddDays(-1) },
            new() { CreatedDate = DateTime.Today.AddDays(-2), ActivatedDate = DateTime.Today.AddDays(-1), ClosedDate = DateTime.Today }
        ];
        compute.Invoke(metrics, new object?[] { items });

        var labels = (string[])labelsField.GetValue(metrics)!;
        Assert.All(labels, l => Assert.False(string.IsNullOrEmpty(l)));
    }

    [Fact]
    public void ComputeFlow_Respects_DateRange()
    {
        SetupServices();

        var metrics = new TestMetrics();
        var type = typeof(Metrics);
        var compute = type.GetMethod("ComputeFlow", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var labelsField = type.GetField("_flowLabels", BindingFlags.NonPublic | BindingFlags.Instance)!;
        type.GetField("_startDate", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(metrics, DateTime.Today.AddDays(-5));
        type.GetField("_endDate", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(metrics, DateTime.Today);
        List<StoryMetric> items = [
            new() { CreatedDate = DateTime.Today.AddDays(-7), ActivatedDate = DateTime.Today.AddDays(-5), ClosedDate = DateTime.Today.AddDays(-3) },
            new() { CreatedDate = DateTime.Today.AddDays(-4), ActivatedDate = DateTime.Today.AddDays(-4), ClosedDate = DateTime.Today.AddDays(-1) }
        ];
        compute.Invoke(metrics, new object?[] { items });

        var labels = (string[])labelsField.GetValue(metrics)!;
        Assert.Equal(6, labels.Length);
        Assert.Equal(DateTime.Today.AddDays(-5).ToLocalDateString(), labels[0]);
        Assert.Equal(DateTime.Today.ToLocalDateString(), labels[^1]);
    }


    private class TestMetrics : Metrics
    {
        protected override Task OnInitializedAsync() => Task.CompletedTask;
    }
}
