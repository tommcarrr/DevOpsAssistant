namespace DevOpsAssistant.Services.Models;

public class PromptStandards
{
    public List<string> RequirementsDocumentation { get; set; } = new();
    public List<string> UserStoryDescription { get; set; } = new();
    public List<string> UserStoryAcceptanceCriteria { get; set; } = new();
    public List<string> UserStoryQuality { get; set; } = new();
    public List<string> BugReporting { get; set; } = new();
}
