namespace DevOpsAssistant.Services.Models;

public class StoryMetric
{
    public int Id { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ActivatedDate { get; set; }
    public DateTime ClosedDate { get; set; }
    public double StoryPoints { get; set; }
    public double OriginalEstimate { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
}
