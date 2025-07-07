namespace DevOpsAssistant.Services.Models;

public class StoryMetric
{
    /// <summary>
    /// Special value used for <see cref="ClosedDate"/> when the work item has
    /// not yet been closed. This allows metrics to include active stories for
    /// WIP calculations without affecting throughput or cycle/lead time
    /// metrics.
    /// </summary>
    public static readonly DateTime OpenClosedDate = DateTime.MaxValue;

    public int Id { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ActivatedDate { get; set; }
    public DateTime ClosedDate { get; set; }
    public double StoryPoints { get; set; }
    public double OriginalEstimate { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
}
