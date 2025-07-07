using System.Reflection;
using System.Text.Json;
using DevOpsAssistant.Pages;
using DevOpsAssistant.Services.Models;
using DevOpsAssistant.Services;
using DevOpsAssistant.Tests.Utils;
using DevOpsAssistant.Utils;
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
        var array = new[]
        {
            new PeriodMetrics
            {
                End = new DateTime(2024,1,1),
                AvgLeadTime = 1.0,
                AvgCycleTime = 2.0,
                Throughput = 3,
                Velocity = 4.0
            }
        };

        var csv = (string)method.Invoke(null, [array])!;

        Assert.Contains("Velocity", csv);
        Assert.Contains("4.0", csv);
    }

    [Fact]
    public void BuildPrompt_Includes_Metrics()
    {
        SetupServices();

        var period = new PeriodMetrics
        {
            End = new DateTime(2024, 1, 1),
            AvgLeadTime = 1.2,
            AvgCycleTime = 2.3,
            Throughput = 3,
            Velocity = 4.5,
            AvgWip = 1.5,
            SprintEfficiency = 80.0
        };
        var method = typeof(Metrics).GetMethod("BuildPromptData", BindingFlags.NonPublic | BindingFlags.Static)!;
        var json = (string)method.Invoke(null, [new[] { period }])!;
        var svc = new PromptService();
        var prompt = PromptService.BuildMetricsPrompt(json, OutputFormat.Markdown);

        Assert.Contains("Agile Delivery Metrics Report Template", prompt);
        Assert.Contains("\"end\":\"2024-01-01\"", prompt);
        Assert.Contains("\"leadTime\":1.2", prompt);
        Assert.Contains("\"velocity\":4.5", prompt);
        Assert.Contains("\"avgWip\":1.5", prompt);
        Assert.Contains("\"sprintEfficiency\":80", prompt);
    }

    [Fact]
    public void BuildPromptData_Computes_Correct_Averages()
    {
        SetupServices();

        var periods = new[]
        {
            new PeriodMetrics
            {
                End = new DateTime(2024, 1, 1),
                AvgLeadTime = 1,
                AvgCycleTime = 2,
                Throughput = 3,
                Velocity = 4,
                AvgWip = 5,
                SprintEfficiency = 60
            },
            new PeriodMetrics
            {
                End = new DateTime(2024, 1, 2),
                AvgLeadTime = 3,
                AvgCycleTime = 4,
                Throughput = 5,
                Velocity = 6,
                AvgWip = 7,
                SprintEfficiency = 80
            }
        };
        var method = typeof(Metrics).GetMethod("BuildPromptData", BindingFlags.NonPublic | BindingFlags.Static)!;
        var json = (string)method.Invoke(null, [periods])!;
        using var doc = JsonDocument.Parse(json);
        var summary = doc.RootElement.GetProperty("summary");

        Assert.Equal(2m, summary.GetProperty("avgLeadTime").GetDecimal());
        Assert.Equal(3m, summary.GetProperty("avgCycleTime").GetDecimal());
        Assert.Equal(4m, summary.GetProperty("avgThroughput").GetDecimal());
        Assert.Equal(5m, summary.GetProperty("avgVelocity").GetDecimal());
        Assert.Equal(6m, summary.GetProperty("avgWip").GetDecimal());
        Assert.Equal(70m, summary.GetProperty("avgSprintEfficiency").GetDecimal());
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
        type.GetField("_startDate", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(metrics, items.Min(i => i.ClosedDate));
        compute.Invoke(metrics, [items]);

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
        type.GetField("_startDate", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(metrics, items.Min(i => i.ClosedDate));
        compute.Invoke(metrics, [items]);

        var series = (List<ApexSeries>)seriesField.GetValue(metrics)!;
        Assert.Contains(series, s => s.Name == "Trend");
        var options = (ApexChartOptions<ChartPoint>)optionsField.GetValue(metrics)!;
        var dashes = (List<double>)options.Stroke!.DashArray!;
        Assert.True(dashes[series.FindIndex(s => s.Name == "Trend")] > 0);
    }

    [Fact]
    public void ComputeBurnUp_ProjectionLines_Are_Dashed()
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
        type.GetField("_startDate", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(metrics, items.Min(i => i.ClosedDate));
        compute.Invoke(metrics, [items]);

        var series = (List<ApexSeries>)seriesField.GetValue(metrics)!;
        var options = (ApexChartOptions<ChartPoint>)optionsField.GetValue(metrics)!;
        var dashes = (List<double>)options.Stroke!.DashArray!;

        foreach (var name in new[] { "Projection Min", "Projection Max" })
        {
            var idx = series.FindIndex(s => s.Name == name);
            Assert.True(idx >= 0);
            Assert.True(dashes[idx] > 0);
        }
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
        type.GetField("_startDate", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(metrics, items.Min(i => i.ClosedDate));
        compute.Invoke(metrics, [items]);

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
        type.GetField("_startDate", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(metrics, items.Min(i => i.ClosedDate));
        compute.Invoke(metrics, [items]);

        var series = (List<ApexSeries>)seriesField.GetValue(metrics)!;
        var trend = series.First(s => s.Name == "Trend").Points;

        Assert.Equal(0, trend[0].Value);
        Assert.Equal(5, trend[1].Value);
    }

    [Fact]
    public void ComputeBurnUp_ProjectionMax_Stops_At_Target()
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
        compute.Invoke(metrics, [items]);

        var series = (List<ApexSeries>)seriesField.GetValue(metrics)!;
        var target = series.First(s => s.Name == "Target").Points.First().Value!.Value;
        var max = series.First(s => s.Name == "Projection Max").Points;

        var lastValueIdx = max.FindLastIndex(p => p.Value.HasValue);
        Assert.Equal(target, max[lastValueIdx].Value);
        Assert.All(max.Skip(lastValueIdx + 1), p => Assert.Null(p.Value));
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
        compute.Invoke(metrics, [items]);

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
        compute.Invoke(metrics, [items]);

        var labels = (string[])labelsField.GetValue(metrics)!;
        Assert.Equal(6, labels.Length);
        Assert.Equal(DateTime.Today.AddDays(-5).ToLocalDateString(), labels[0]);
        Assert.Equal(DateTime.Today.ToLocalDateString(), labels[^1]);
    }

    [Fact]
    public void FilterItems_Removes_Ignored_Tags()
    {
        SetupServices();

        var metrics = new TestMetrics();
        var type = typeof(Metrics);
        type.GetField("_ignoredTags", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(metrics, new HashSet<string> { "Skip" });
        var filter = type.GetMethod("FilterItems", BindingFlags.NonPublic | BindingFlags.Instance)!;
        List<StoryMetric> items = [
            new() { CreatedDate = DateTime.Today, ActivatedDate = DateTime.Today, ClosedDate = DateTime.Today, Tags = ["Skip"] },
            new() { CreatedDate = DateTime.Today, ActivatedDate = DateTime.Today, ClosedDate = DateTime.Today, Tags = ["Keep"] }
        ];
        var result = (IEnumerable<StoryMetric>)filter.Invoke(metrics, [items])!;
        var list = result.ToList();

        Assert.Single(list);
        Assert.Contains("Keep", list[0].Tags);
    }


    private class TestMetrics : Metrics
    {
        protected override Task OnInitializedAsync() => Task.CompletedTask;
    }
}
