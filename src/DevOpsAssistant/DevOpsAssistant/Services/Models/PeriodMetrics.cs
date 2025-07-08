namespace DevOpsAssistant.Services.Models;

public class PeriodMetrics
{
    public string Name { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public double AvgLeadTime { get; set; }
    public double AvgCycleTime { get; set; }
    public int Throughput { get; set; }
    public double Velocity { get; set; }
    public double RollingVelocity { get; set; }
    public double AvgWip { get; set; }
    public double SprintEfficiency { get; set; }
}
