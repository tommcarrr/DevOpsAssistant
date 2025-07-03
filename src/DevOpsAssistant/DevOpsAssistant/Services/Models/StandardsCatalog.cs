using GeneratedPrompts;

namespace DevOpsAssistant.Services.Models;

public record StandardOption(string Group, string Id, string Name, string Description);

public static class StandardsCatalog
{
    public static readonly StandardOption[] Options =
    [
        new("requirements_documentation", "ISO29148", "ISO/IEC/IEEE 29148:2018", GeneratedPrompts.Standard_ISO29148Prompt.Value),
        new("requirements_documentation", "Volere", "Volere Template", GeneratedPrompts.Standard_VolerePrompt.Value),
        new("requirements_documentation", "BABOK", "BABOK (Business Analysis Body of Knowledge)", GeneratedPrompts.Standard_BABOKPrompt.Value),
        new("requirements_documentation", "ISO25010", "ISO/IEC 25010", GeneratedPrompts.Standard_ISO25010Prompt.Value),

        new("user_story_description", "ScrumUserStory", "Scrum User Story", GeneratedPrompts.Standard_ScrumUserStoryPrompt.Value),
        new("user_story_description", "JobStory", "Job Story", GeneratedPrompts.Standard_JobStoryPrompt.Value),

        new("user_story_acceptance_criteria", "Gherkin", "Gherkin / BDD", GeneratedPrompts.Standard_GherkinPrompt.Value),
        new("user_story_acceptance_criteria", "BulletPoints", "Bullet Points", GeneratedPrompts.Standard_BulletPointsPrompt.Value),
        new("user_story_acceptance_criteria", "SAFeStyle", "SAFe Style", GeneratedPrompts.Standard_SAFeStylePrompt.Value),

        new("user_story_quality", "INVEST", "INVEST", GeneratedPrompts.Standard_INVESTPrompt.Value),
        new("user_story_quality", "SAFe", "SAFe", GeneratedPrompts.Standard_SAFePrompt.Value),
        new("user_story_quality", "AgileAlliance", "Agile Alliance", GeneratedPrompts.Standard_AgileAlliancePrompt.Value),

        new("bug_reporting", "AzureDevOpsBug", "Azure DevOps Bug", GeneratedPrompts.Standard_AzureDevOpsBugPrompt.Value),
        new("bug_reporting", "ISTQBDefect", "ISTQB Defect Report", GeneratedPrompts.Standard_ISTQBDefectPrompt.Value)
    ];

    public static readonly Dictionary<string, string[]> Incompatibilities = new()
    {
        ["ScrumUserStory"] = ["JobStory", "ISO29148"],
        ["JobStory"] = ["ScrumUserStory", "ISO29148"],
        ["Gherkin"] = ["BulletPoints", "SAFeStyle"],
        ["BulletPoints"] = ["Gherkin"],
        ["SAFeStyle"] = ["Gherkin"],
        ["ISO29148"] = ["ScrumUserStory", "JobStory"]
    };

    public static string GetName(string id) => Options.FirstOrDefault(o => o.Id == id)?.Name ?? id;
}
