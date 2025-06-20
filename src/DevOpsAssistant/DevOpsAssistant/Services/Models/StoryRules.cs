namespace DevOpsAssistant.Services.Models;

public class StoryRules
{
    public bool HasDescription { get; set; }
    public bool HasParent { get; set; }
    public bool HasStoryPoints { get; set; }
    public bool HasAcceptanceCriteria { get; set; }
    public bool HasAssignee { get; set; }
}
