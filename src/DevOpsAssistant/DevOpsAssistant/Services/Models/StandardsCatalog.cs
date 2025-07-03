namespace DevOpsAssistant.Services.Models;

public record StandardOption(string Group, string Id, string Name, string Description);

public static class StandardsCatalog
{
    public static readonly StandardOption[] Options =
    [
        new("requirements_documentation", "ISO29148", "ISO/IEC/IEEE 29148:2018", "Formal SRS requirements standard including characteristics like complete, unambiguous, verifiable."),
        new("requirements_documentation", "Volere", "Volere Template", "Practical requirements template with detailed sections for functional, non-functional, constraints, priorities."),
        new("requirements_documentation", "BABOK", "BABOK (Business Analysis Body of Knowledge)", "Guide for good practices in writing requirements including clarity, conciseness, and testability."),
        new("requirements_documentation", "ISO25010", "ISO/IEC 25010", "Defines software quality attributes (for non-functional requirements)."),

        new("user_story_description", "ScrumUserStory", "Scrum User Story", "Use \"As a [role] I want [goal] so that [benefit]\" format."),
        new("user_story_description", "JobStory", "Job Story", "Use \"When [situation] I want to [motivation] so I can [expected outcome]\" format."),

        new("user_story_acceptance_criteria", "Gherkin", "Gherkin / BDD", "Write acceptance criteria in Given-When-Then format for automation."),
        new("user_story_acceptance_criteria", "BulletPoints", "Bullet Points", "Write acceptance criteria as plain bullet points."),
        new("user_story_acceptance_criteria", "SAFeStyle", "SAFe Style", "Write clear, unambiguous, testable acceptance criteria in any structured format (often bullet or Gherkin)."),

        new("user_story_quality", "INVEST", "INVEST", "Ensure story is Independent, Negotiable, Valuable, Estimable, Small, Testable."),
        new("user_story_quality", "SAFe", "SAFe", "Ensure story has clear description, AC, estimate, and alignment to feature/epic."),
        new("user_story_quality", "AgileAlliance", "Agile Alliance", "Ensure story focuses on business value, small enough for iteration, clear goal."),

        new("bug_reporting", "AzureDevOpsBug", "Azure DevOps Bug", "Include title, repro steps, expected result, actual result, severity, environment."),
        new("bug_reporting", "ISTQBDefect", "ISTQB Defect Report", "Include ID, title, description, steps to reproduce, expected vs actual, severity, priority, status, environment.")
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
