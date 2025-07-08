using DevOpsAssistant.Services;
using DevOpsAssistant.Services.Models;

namespace DevOpsAssistant.Tests.Services;

public class PromptServiceTests
{
    [Fact]
    public void BuildRequirementsPlannerPrompt_Includes_Acceptance_Criteria_Standards()
    {
        var pages = new List<(string Name, string Text)> { ("Req", "text") };
        var cfg = new DevOpsConfig
        {
            Standards = new PromptStandards
            {
                UserStoryAcceptanceCriteria = ["BulletPoints"]
            }
        };

        var result = PromptService.BuildRequirementsPlannerPrompt(pages, false, false, cfg);

        Assert.Contains("Bullet Points", result);
    }

    [Fact]
    public void BuildRequirementsPlannerPrompt_Uses_Custom_Prompt_When_Replacing()
    {
        var pages = new List<(string Name, string Text)> { ("Req", "text") };
        var cfg = new DevOpsConfig
        {
            RequirementsPrompt = "Custom",
            RequirementsPromptMode = PromptMode.Replace
        };

        var result = PromptService.BuildRequirementsPlannerPrompt(pages, false, false, cfg);

        Assert.StartsWith("Custom", result.Trim());
    }

    [Fact]
    public void BuildRequirementsGathererPrompt_Includes_Document_When_Pages_Provided()
    {
        var pages = new List<(string Name, string Text)> { ("Doc", "content") };
        var cfg = new DevOpsConfig
        {
            RequirementsPrompt = "Custom",
            RequirementsPromptMode = PromptMode.Replace
        };

        var result = PromptService.BuildRequirementsGathererPrompt(pages, cfg);

        Assert.Contains("Agile Business Analyst", result);
        Assert.Contains("Document:", result);
        Assert.Contains("## Doc", result);
    }
}
