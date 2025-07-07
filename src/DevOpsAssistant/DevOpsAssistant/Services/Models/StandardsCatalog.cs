using GeneratedPrompts;

namespace DevOpsAssistant.Services.Models;

public record StandardOption(string Group, string Id, string Name, string Description);

public static class StandardsCatalog
{
    public static readonly StandardOption[] Options =
    [
        new("requirements_documentation", "ISO29148", "ISO/IEC/IEEE 29148:2018", Standard_ISO29148Prompt.Value),
        new("requirements_documentation", "Volere", "Volere Template", Standard_VolerePrompt.Value),
        new("requirements_documentation", "BABOK", "BABOK (Business Analysis Body of Knowledge)", Standard_BABOKPrompt.Value),
        new("requirements_documentation", "ISO25010", "ISO/IEC 25010", Standard_ISO25010Prompt.Value),

        new("user_story_description", "ScrumUserStory", "Scrum User Story", Standard_ScrumUserStoryPrompt.Value),
        new("user_story_description", "JobStory", "Job Story", Standard_JobStoryPrompt.Value),

        new("user_story_acceptance_criteria", "Gherkin", "Gherkin / BDD", Standard_GherkinPrompt.Value),
        new("user_story_acceptance_criteria", "BulletPoints", "Bullet Points", Standard_BulletPointsPrompt.Value),
        new("user_story_acceptance_criteria", "SAFeStyle", "SAFe Style", Standard_SAFeStylePrompt.Value),

        new("user_story_quality", "INVEST", "INVEST", Standard_INVESTPrompt.Value),
        new("user_story_quality", "SAFe", "SAFe", Standard_SAFePrompt.Value),
        new("user_story_quality", "AgileAlliance", "Agile Alliance", Standard_AgileAlliancePrompt.Value),

        new("bug_reporting", "AzureDevOpsBug", "Azure DevOps Bug", Standard_AzureDevOpsBugPrompt.Value),
        new("bug_reporting", "ISTQBDefect", "ISTQB Defect Report", Standard_ISTQBDefectPrompt.Value)
    ];

    public static readonly Dictionary<string, string[]> Incompatibilities = new()
    {
        ["ScrumUserStory"] = ["JobStory", "ISO29148"],
        ["JobStory"] = ["ScrumUserStory", "ISO29148"],
        ["Gherkin"] = ["BulletPoints", "SAFeStyle"],
        ["BulletPoints"] = ["Gherkin"],
        ["SAFeStyle"] = ["Gherkin"],
        ["ISO29148"] = ["ScrumUserStory", "JobStory", "Volere", "BABOK", "ISO25010"],
        ["Volere"] = ["ISO29148", "BABOK", "ISO25010"],
        ["BABOK"] = ["ISO29148", "Volere", "ISO25010"],
        ["ISO25010"] = ["ISO29148", "Volere", "BABOK"]
    };

    public static string GetName(string id) => Options.FirstOrDefault(o => o.Id == id)?.Name ?? id;
}
