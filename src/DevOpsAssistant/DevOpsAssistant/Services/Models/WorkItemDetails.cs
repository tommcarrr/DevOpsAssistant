namespace DevOpsAssistant.Services;

public class WorkItemDetails
{
    public WorkItemInfo Info { get; set; } = new();
    public bool HasDescription { get; set; }
    public bool HasParent { get; set; }
    public bool HasStoryPoints { get; set; }
    public bool HasAcceptanceCriteria { get; set; }
    public bool HasAssignee { get; set; }
}
