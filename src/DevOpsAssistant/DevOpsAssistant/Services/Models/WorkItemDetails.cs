namespace DevOpsAssistant.Services.Models;

public class WorkItemDetails
{
    public WorkItemInfo Info { get; set; } = new();
    public bool HasDescription { get; set; }
    public bool HasParent { get; set; }
    public bool HasStoryPoints { get; set; }
    public bool HasAcceptanceCriteria { get; set; }
    public bool HasAssignee { get; set; }
    public bool HasReproSteps { get; set; }
    public bool HasSystemInfo { get; set; }
    public bool NeedsAttention { get; set; }
}