using DevOpsAssistant.Services.Models;
using System.Collections.Generic;

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
    public List<string> Nfrs { get; set; } = new();
    public bool CoverNfrs { get; set; }
    public PromptMode StoryQualityPromptMode { get; set; }
    public PromptMode ReleaseNotesPromptMode { get; set; }
    public PromptMode RequirementsPromptMode { get; set; }
    public int PromptCharacterLimit { get; set; }
    public int WorkItemGranularity { get; set; } = 5;
    public OutputFormat OutputFormat { get; set; }
    public PromptStandards Standards { get; set; } = new();
    public ValidationRules Rules { get; set; } = new();
}