namespace DevOpsAssistant.Services.Models;

public class BugRules
{
    public bool IncludeReproSteps { get; set; } = true;
    public bool IncludeSystemInfo { get; set; } = true;
    public bool HasStoryPoints { get; set; } = true;
}
