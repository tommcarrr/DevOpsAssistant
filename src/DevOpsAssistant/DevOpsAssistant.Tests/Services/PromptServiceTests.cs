using DevOpsAssistant.Services;
using DevOpsAssistant.Services.Models;

namespace DevOpsAssistant.Tests.Services;

public class PromptServiceTests
{
    [Fact]
    public void BuildRequirementsPlannerPrompt_Includes_Acceptance_Criteria_Standards()
    {
        var req = new List<DocumentItem> { new("Req", "text", "Req") };
        var ctx = new List<DocumentItem>();
        var cfg = new DevOpsConfig
        {
            Standards = new PromptStandards
            {
                UserStoryAcceptanceCriteria = [StandardIds.BulletPoints]
            }
        };

        var result = PromptService.BuildRequirementsPlannerPrompt(req, ctx, false, false, cfg);

        Assert.Contains("Bullet Points", result);
    }

    [Fact]
    public void BuildRequirementsPlannerPrompt_Uses_Custom_Prompt_When_Replacing()
    {
        var req = new List<DocumentItem> { new("Req", "text", "Req") };
        var ctx = new List<DocumentItem>();
        var cfg = new DevOpsConfig
        {
            RequirementsPrompt = "Custom",
            RequirementsPromptMode = PromptMode.Replace
        };

        var result = PromptService.BuildRequirementsPlannerPrompt(req, ctx, false, false, cfg);

        Assert.StartsWith("Custom", result.Trim());
    }

    [Fact]
    public void BuildRequirementsPlannerPrompt_Covers_Nfrs_When_Enabled()
    {
        var req = new List<DocumentItem> { new("Req", "text", "Req") };
        var ctx = new List<DocumentItem>();
        var cfg = new DevOpsConfig
        {
            Nfrs = ["NFR"],
            CoverNfrs = true
        };

        var result = PromptService.BuildRequirementsPlannerPrompt(req, ctx, false, false, cfg);

        Assert.Contains("address these Non-Functional Requirements", result);
    }

    [Fact]
    public void BuildRequirementsPlannerPrompt_Ignores_Nfrs_When_Disabled()
    {
        var req = new List<DocumentItem> { new("Req", "text", "Req") };
        var ctx = new List<DocumentItem>();
        var cfg = new DevOpsConfig
        {
            Nfrs = ["NFR"],
            CoverNfrs = false
        };

        var result = PromptService.BuildRequirementsPlannerPrompt(req, ctx, false, false, cfg);

        Assert.Contains("Do not create stories", result);
    }

    [Fact]
    public void BuildRequirementsGathererPrompt_Includes_Document_When_Pages_Provided()
    {
        var pages = new List<DocumentItem> { new("Doc", "content", "Doc") };
        var cfg = new DevOpsConfig
        {
            RequirementsPrompt = "Custom",
            RequirementsPromptMode = PromptMode.Replace
        };

        var result = PromptService.BuildRequirementsGathererPrompt(pages, cfg);

        Assert.Contains("Agile Business Analyst", result);
        Assert.Contains("Document:", result);
        Assert.Contains("Page Name: Doc", result);
    }

    [Fact]
    public void BuildRequirementsGathererPrompt_Inline_Format_Does_Not_Request_Conversion()
    {
        var cfg = new DevOpsConfig { OutputFormat = OutputFormat.Inline };

        var result = PromptService.BuildRequirementsGathererPrompt([], cfg);

        Assert.Contains("Reply inline", result);
        Assert.DoesNotContain("convert the content", result);
    }

    [Fact]
    public void BuildRequirementsGathererPrompt_Convert_Format_Includes_Target()
    {
        var cfg = new DevOpsConfig { OutputFormat = OutputFormat.Markdown };

        var result = PromptService.BuildRequirementsGathererPrompt([], cfg);

        Assert.Contains("convert the content to Markdown", result);
    }
}
