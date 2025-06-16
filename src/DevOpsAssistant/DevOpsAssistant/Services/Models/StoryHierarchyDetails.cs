namespace DevOpsAssistant.Services;

public class StoryHierarchyDetails
{
    public WorkItemInfo Story { get; set; } = new();
    public string Description { get; set; } = string.Empty;
    public string AcceptanceCriteria { get; set; } = string.Empty;
    public string ReproSteps { get; set; } = string.Empty;
    public string SystemInfo { get; set; } = string.Empty;
    public WorkItemInfo? Feature { get; set; }
    public WorkItemInfo? Epic { get; set; }
    public string FeatureDescription { get; set; } = string.Empty;
    public string EpicDescription { get; set; } = string.Empty;
}