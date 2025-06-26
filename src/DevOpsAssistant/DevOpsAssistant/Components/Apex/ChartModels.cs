namespace DevOpsAssistant.Components.Apex;

public class ChartPoint
{
    public string Label { get; set; } = string.Empty;
    public decimal? Value { get; set; }
}

public class ApexSeries
{
    public string Name { get; set; } = string.Empty;
    public List<ChartPoint> Points { get; set; } = new();
}
