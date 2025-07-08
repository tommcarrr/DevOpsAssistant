using GeneratedPrompts;

namespace DevOpsAssistant.Services.Models;

public record StandardOption(string Group, string Id, string Name, string Description, string Link);

public static class StandardsCatalog
{
    public static readonly StandardOption[] Options =
    [
        new("requirements_documentation", StandardIds.ISO29148, "ISO/IEC/IEEE 29148:2018", Standard_ISO29148Prompt.Value, "https://www.iso.org/standard/72030.html"),
        new("requirements_documentation", StandardIds.Volere, "Volere Template", Standard_VolerePrompt.Value, "https://www.volere.org"),
        new("requirements_documentation", StandardIds.BABOK, "BABOK (Business Analysis Body of Knowledge)", Standard_BABOKPrompt.Value, "https://www.iiba.org/babok-guide/"),
        new("requirements_documentation", StandardIds.ISO25010, "ISO/IEC 25010", Standard_ISO25010Prompt.Value, "https://iso25000.com/index.php/en/iso-25000-standards/iso-25010"),

        new("user_story_description", StandardIds.ScrumUserStory, "Scrum User Story", Standard_ScrumUserStoryPrompt.Value, "https://www.scrum.org/resources/user-stories"),
        new("user_story_description", StandardIds.JobStory, "Job Story", Standard_JobStoryPrompt.Value, "https://jtbd.info/replacing-the-user-story-with-the-job-story-af7cdee10c27"),

        new("user_story_acceptance_criteria", StandardIds.Gherkin, "Gherkin / BDD", Standard_GherkinPrompt.Value, "https://cucumber.io/docs/gherkin/"),
        new("user_story_acceptance_criteria", StandardIds.BulletPoints, "Bullet Points", Standard_BulletPointsPrompt.Value, "https://www.markdownguide.org/basic-syntax/#lists"),
        new("user_story_acceptance_criteria", StandardIds.SAFeStyle, "SAFe Style", Standard_SAFeStylePrompt.Value, "https://scaledagileframework.com/"),

        new("user_story_quality", StandardIds.INVEST, "INVEST", Standard_INVESTPrompt.Value, "https://www.agilealliance.org/glossary/invest"),
        new("user_story_quality", StandardIds.SAFe, "SAFe", Standard_SAFePrompt.Value, "https://scaledagileframework.com/"),
        new("user_story_quality", StandardIds.AgileAlliance, "Agile Alliance", Standard_AgileAlliancePrompt.Value, "https://www.agilealliance.org"),

        new("bug_reporting", StandardIds.AzureDevOpsBug, "Azure DevOps Bug", Standard_AzureDevOpsBugPrompt.Value, "https://learn.microsoft.com/azure/devops/boards/backlogs/about-bugs"),
        new("bug_reporting", StandardIds.ISTQBDefect, "ISTQB Defect Report", Standard_ISTQBDefectPrompt.Value, "https://glossary.istqb.org/en/term/defect-report")
    ];

    public static readonly Dictionary<string, string[]> Incompatibilities = new()
    {
        [StandardIds.ScrumUserStory] = [StandardIds.JobStory, StandardIds.ISO29148],
        [StandardIds.JobStory] = [StandardIds.ScrumUserStory, StandardIds.ISO29148],
        [StandardIds.Gherkin] = [StandardIds.BulletPoints, StandardIds.SAFeStyle],
        [StandardIds.BulletPoints] = [StandardIds.Gherkin],
        [StandardIds.SAFeStyle] = [StandardIds.Gherkin],
        [StandardIds.ISO29148] = [StandardIds.ScrumUserStory, StandardIds.JobStory, StandardIds.Volere, StandardIds.BABOK, StandardIds.ISO25010],
        [StandardIds.Volere] = [StandardIds.ISO29148, StandardIds.BABOK, StandardIds.ISO25010],
        [StandardIds.BABOK] = [StandardIds.ISO29148, StandardIds.Volere, StandardIds.ISO25010],
        [StandardIds.ISO25010] = [StandardIds.ISO29148, StandardIds.Volere, StandardIds.BABOK]
    };

    public static string GetName(string id) => Options.FirstOrDefault(o => o.Id == id)?.Name ?? id;
}
