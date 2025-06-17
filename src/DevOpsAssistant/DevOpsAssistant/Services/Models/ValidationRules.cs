namespace DevOpsAssistant.Services;

public class ValidationRules
{
    public EpicRules Epic { get; set; } = new();
    public FeatureRules Feature { get; set; } = new();
    public StoryRules Story { get; set; } = new();
    public BugRules Bug { get; set; } = new();
}
