namespace DevOpsAssistant.Services;

public class ValidationRules
{
    public bool EpicHasDescription { get; set; }
    public bool FeatureHasDescription { get; set; }
    public bool FeatureHasParent { get; set; }
    public bool StoryHasDescription { get; set; }
    public bool StoryHasParent { get; set; }
    public bool StoryHasStoryPoints { get; set; }
    public bool StoryHasAcceptanceCriteria { get; set; }
    public bool StoryHasAssignee { get; set; }
}
