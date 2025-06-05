namespace DevOpsAssistant.Services;

public class StoryHierarchyDetails
{
    public WorkItemInfo Story { get; set; } = new();
    public string Description { get; set; } = string.Empty;
    public WorkItemInfo? Feature { get; set; }
    public WorkItemInfo? Epic { get; set; }
}
