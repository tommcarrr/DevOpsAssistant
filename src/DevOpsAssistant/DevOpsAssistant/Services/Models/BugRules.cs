namespace DevOpsAssistant.Services.Models;

public class BugRules
{
    public bool IncludeReproSteps { get; set; } = false;
    public bool IncludeSystemInfo { get; set; } = false;
    public bool HasStoryPoints { get; set; } = false;
}
