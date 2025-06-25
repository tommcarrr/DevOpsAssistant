using DevOpsAssistant.Services.Models;

namespace DevOpsAssistant.Services;

public class DevOpsConfig
{
    public string Organization { get; set; } = string.Empty;
    public string Project { get; set; } = string.Empty;
    public string PatToken { get; set; } = string.Empty;
    public string MainBranch { get; set; } = string.Empty;
    public string DefinitionOfReady { get; set; } = string.Empty;
    public string StoryQualityPrompt { get; set; } = string.Empty;
    public string ReleaseNotesPrompt { get; set; } = string.Empty;
    public string RequirementsPrompt { get; set; } = string.Empty;
    public int PromptCharacterLimit { get; set; }
    public OutputFormat OutputFormat { get; set; }
    public ValidationRules Rules { get; set; } = new();
}