using Bunit;
using DevOpsAssistant.Components;
using DevOpsAssistant.Tests.Utils;
using MudBlazor;

namespace DevOpsAssistant.Tests.Components;

public class ChartDialogTests : ComponentTestBase
{
    private class FakeDialog : IMudDialogInstance
    {
        public string Id => "1";
        public string ElementId => "1";
        public DialogOptions Options => new();
        public string Title { get; set; } = string.Empty;
        public Task SetOptionsAsync(DialogOptions options) => Task.CompletedTask;
        public Task SetTitleAsync(string title) { Title = title; return Task.CompletedTask; }
        public void Close() { }
        public void Close(DialogResult result) { }
        public void Close<T>(T returnValue) { }
        public void Cancel() { }
        public void CancelAll() { }
        public void StateHasChanged() { }
    }

    [Fact]
    public void Dialog_Shows_Title_And_Close_Button()
    {
        SetupServices();
        var fake = new FakeDialog();
        var cut = RenderComponent<ChartDialog>(parameters => parameters
            .AddCascadingValue(fake)
            .Add(p => p.Title, "Test Chart")
            .Add(p => p.ChartType, ChartType.Line)
            .Add(p => p.ChartSeries, new List<ChartSeries>())
            .Add(p => p.XAxisLabels, Array.Empty<string>())
            .Add(p => p.AxisChartOptions, new AxisChartOptions())
        );

        cut.Markup.Contains("Test Chart");
        cut.Markup.Contains("Close");
    }
}
