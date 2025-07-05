using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GeneratedPrompts;
using DevOpsAssistant.Services.Models;
using DevOpsAssistant.Utils;
using System.Collections.Generic;

namespace DevOpsAssistant.Services;

public class PromptService
{
    public string BuildMetricsPrompt(string json, OutputFormat format)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Metrics_MainPrompt.Value);
        sb.AppendLine();
        if (format == OutputFormat.Inline)
            sb.AppendLine(FormatInstructions_MetricsInlinePrompt.Value);
        else
            sb.AppendLine(string.Format(FormatInstructions_MetricsConvertPrompt.Value, format));
        sb.AppendLine();
        sb.AppendLine(string.Format(Metrics_DataIntroPrompt.Value, json));
        sb.AppendLine();
        return sb.ToString();
    }

    public string BuildReleaseNotesPrompt(string json, DevOpsConfig config)
    {
        var sb = new StringBuilder();
        if (string.IsNullOrWhiteSpace(config.ReleaseNotesPrompt) || config.ReleaseNotesPromptMode == PromptMode.Append)
            sb.AppendLine(ReleaseNotes_MainPrompt.Value);
        if (!string.IsNullOrWhiteSpace(config.ReleaseNotesPrompt))
            sb.AppendLine(config.ReleaseNotesPrompt.Trim());
        if (config.OutputFormat == OutputFormat.Inline)
            sb.AppendLine(FormatInstructions_ReleaseNotesInlinePrompt.Value);
        else
            sb.AppendLine(string.Format(FormatInstructions_ReleaseNotesConvertPrompt.Value, config.OutputFormat));
        sb.AppendLine();
        sb.AppendLine(ReleaseNotes_WorkItemsIntroPrompt.Value);
        sb.AppendLine(json);
        sb.AppendLine();
        return sb.ToString();
    }

    public string BuildRequirementsQualityPrompt(IEnumerable<(string Name, string Text)> pages, DevOpsConfig config)
    {
        var sb = new StringBuilder();
        sb.AppendLine(RequirementsQuality_MainPrompt.Value);
        if (config.Standards.RequirementsDocumentation.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine(RequirementsDocumentationStandardsIntroPrompt.Value);
            foreach (var s in config.Standards.RequirementsDocumentation)
                sb.AppendLine($"- {StandardsCatalog.GetName(s)}");
        }
        sb.AppendLine();
        sb.AppendLine(RequirementsQuality_DocumentIntroPrompt.Value);
        foreach (var page in pages)
        {
            sb.AppendLine($"## {page.Name}");
            sb.AppendLine(page.Text);
            sb.AppendLine();
        }
        return sb.ToString();
    }

    public string BuildRequirementsPlannerPrompt(IEnumerable<(string Name, string Text)> pages, bool storiesOnly, bool clarify, DevOpsConfig config)
    {
        var sb = new StringBuilder();
        if (string.IsNullOrWhiteSpace(config.RequirementsPrompt) || config.RequirementsPromptMode == PromptMode.Append)
        {
            if (storiesOnly)
                sb.AppendLine(RequirementsPlanner_UserStoriesBlockPrompt.Value);
            else
                sb.AppendLine(RequirementsPlanner_EpicsBlockPrompt.Value);
            if (config.Standards.UserStoryDescription.Contains("ScrumUserStory"))
                sb.AppendLine(RequirementsPlanner_Description_ScrumPrompt.Value);
            else if (config.Standards.UserStoryDescription.Contains("JobStory"))
                sb.AppendLine(RequirementsPlanner_Description_JobPrompt.Value);
            else
                sb.AppendLine(RequirementsPlanner_Description_GenericPrompt.Value);
            if (config.Standards.UserStoryAcceptanceCriteria.Contains("Gherkin"))
                sb.AppendLine(RequirementsPlanner_AcceptanceCriteria_GherkinPrompt.Value);
            else if (config.Standards.UserStoryAcceptanceCriteria.Contains("BulletPoints"))
                sb.AppendLine(RequirementsPlanner_AcceptanceCriteria_BulletPointsPrompt.Value);
            else if (config.Standards.UserStoryAcceptanceCriteria.Contains("SAFeStyle"))
                sb.AppendLine(RequirementsPlanner_AcceptanceCriteria_SAFeStylePrompt.Value);
            sb.AppendLine(RequirementsPlanner_TagsAndHtmlAdvicePrompt.Value);
            sb.AppendLine(string.Format(RequirementsPlanner_WorkItemGranularityPrompt.Value, config.WorkItemGranularity));
            if (config.Standards.UserStoryQuality.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine(RequirementsPlanner_StoryQualityStandardsIntroPrompt.Value);
                foreach (var s in config.Standards.UserStoryQuality)
                    sb.AppendLine($"- {StandardsCatalog.GetName(s)}");
            }
            if (config.Standards.RequirementsDocumentation.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine(RequirementsDocumentationStandardsIntroPrompt.Value);
                foreach (var s in config.Standards.RequirementsDocumentation)
                    sb.AppendLine($"- {StandardsCatalog.GetName(s)}");
            }
            if (storiesOnly)
            {
                sb.AppendLine();
                sb.AppendLine(RequirementsPlanner_StoriesOnlyBlockPrompt.Value);
            }
            else
            {
                sb.AppendLine();
                sb.AppendLine(RequirementsPlanner_EpicsJsonBlockPrompt.Value);
            }
            if (clarify)
            {
                sb.AppendLine();
                sb.AppendLine(RequirementsPlanner_ClarifyBlockPrompt.Value);
            }
        }
        if (!string.IsNullOrWhiteSpace(config.RequirementsPrompt))
            sb.AppendLine(config.RequirementsPrompt.Trim());
        sb.AppendLine();
        sb.AppendLine(RequirementsPlanner_DocumentIntroPrompt.Value);
        foreach (var page in pages)
        {
            sb.AppendLine($"## {page.Name}");
            sb.AppendLine(page.Text);
            sb.AppendLine();
        }
        sb.AppendLine();
        return sb.ToString();
    }

    public string BuildWorkItemQualityPrompt(string json, DevOpsConfig config)
    {
        var sb = new StringBuilder();
        if (string.IsNullOrWhiteSpace(config.StoryQualityPrompt) || config.StoryQualityPromptMode == PromptMode.Append)
        {
            sb.AppendLine(WorkItemQuality_MainPrompt.Value);
            if (config.Standards.UserStoryQuality.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine(WorkItemQuality_StoryQualityStandardsIntroPrompt.Value);
                foreach (var s in config.Standards.UserStoryQuality)
                    sb.AppendLine($"- {StandardsCatalog.GetName(s)}");
            }
            if (config.Standards.BugReporting.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine(WorkItemQuality_BugReportingStandardsIntroPrompt.Value);
                foreach (var s in config.Standards.BugReporting)
                    sb.AppendLine($"- {StandardsCatalog.GetName(s)}");
            }
            if (!string.IsNullOrWhiteSpace(config.DefinitionOfReady))
            {
                sb.AppendLine();
                sb.AppendLine(WorkItemQuality_DefinitionOfReadyIntroPrompt.Value);
                sb.AppendLine(config.DefinitionOfReady);
            }
        }
        if (!string.IsNullOrWhiteSpace(config.StoryQualityPrompt))
            sb.AppendLine(config.StoryQualityPrompt.Trim());
        if (config.OutputFormat == OutputFormat.Inline)
            sb.AppendLine(FormatInstructions_WorkItemAnalysisInlinePrompt.Value);
        else
            sb.AppendLine(string.Format(FormatInstructions_WorkItemAnalysisConvertPrompt.Value, config.OutputFormat));
        sb.AppendLine();
        sb.AppendLine(WorkItemQuality_WorkItemsIntroPrompt.Value);
        sb.AppendLine(json);
        sb.AppendLine();
        return sb.ToString();
    }
}
