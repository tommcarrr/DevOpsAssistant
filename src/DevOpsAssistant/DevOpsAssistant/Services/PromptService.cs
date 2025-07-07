using System.Text;
using GeneratedPrompts;
using DevOpsAssistant.Services.Models;

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

    public string BuildRequirementsPlannerPrompt(IEnumerable<(string Name, string Text)> pages, bool storiesOnly,
        bool clarify, DevOpsConfig config)
    {
        var sb = new StringBuilder();
        if (string.IsNullOrWhiteSpace(config.RequirementsPrompt) || config.RequirementsPromptMode == PromptMode.Append)
        {
            sb.AppendFormat(RequirementsPlanner_MainPrompt.Value,
                storiesOnly
                    ? RequirementsPlanner_StoriesOnlyPrompt.Value
                    : RequirementsPlanner_EpicsFeaturesStoriesPrompt.Value,
                config.WorkItemGranularity.ToString(),
                GetWorkItemStandards(config),
                GetWorkItemDescriptionStandards(config),
                GetWorkItemAcStandards(config),
                GetRequirementsDocument(),
                clarify ? RequirementsPlanner_ClarifyRequirementsPrompt.Value : "",
                !string.IsNullOrWhiteSpace(config.RequirementsPrompt) &&
                config.RequirementsPromptMode == PromptMode.Append
                    ? config.RequirementsPrompt
                    : "");

            return sb.ToString();

            string GetWorkItemAcStandards(DevOpsConfig devOpsConfig)
            {
                if (config.Standards.UserStoryAcceptanceCriteria.Count <= 0) return "";

                var sbLocal = new StringBuilder();
                sbLocal.AppendLine(RequirementsPlanner_WorkItemACStandardsPrompt.Value);
                foreach (var standardText in devOpsConfig.Standards.UserStoryDescription.Select(s => s switch
                         {
                             "Gherkin" => RequirementsPlanner_WorkItemACStandards_GherkinPrompt.Value,
                             "BulletPoints" => RequirementsPlanner_WorkItemACStandards_BulletPointsPrompt.Value,
                             "SAFeStyle" => RequirementsPlanner_WorkItemACStandards_SAFePrompt.Value,
                             _ => ""
                         }))
                {
                    sbLocal.AppendLine(standardText);
                }

                return sbLocal.ToString();
            }

            string GetWorkItemDescriptionStandards(DevOpsConfig devOpsConfig)
            {
                if (config.Standards.UserStoryDescription.Count <= 0) return "";

                var sbLocal = new StringBuilder();
                sbLocal.AppendLine(RequirementsPlanner_WorkItemDescriptionStandardsPrompt.Value);
                foreach (var standardText in devOpsConfig.Standards.UserStoryDescription.Select(s => s switch
                         {
                             "ScrumUserStory" => RequirementsPlanner_WorkItemDescriptionStandards_ScrumUserStoryPrompt
                                 .Value,
                             "JobStory" => RequirementsPlanner_WorkItemDescriptionStandards_JobStoryPrompt.Value,
                             _ => ""
                         }))
                {
                    sbLocal.AppendLine(standardText);
                }
                
                return sbLocal.ToString();
            }

            string GetWorkItemStandards(DevOpsConfig devOpsConfig)
            {
                if (config.Standards.UserStoryQuality.Count <= 0) return "";

                var sbLocal = new StringBuilder();
                sbLocal.AppendLine(RequirementsPlanner_WorkItemStandardsPrompt.Value);
                foreach (var standardText in devOpsConfig.Standards.UserStoryQuality.Select(s => s switch
                         {
                             "INVEST" => RequirementsPlanner_WorkItemStandards_INVESTPrompt.Value,
                             "SAFe" => RequirementsPlanner_WorkItemStandards_SAFePrompt.Value,
                             "AgileAlliance" => RequirementsPlanner_WorkItemStandards_AgileAlliancePrompt.Value,
                             _ => ""
                         }))
                {
                    sbLocal.AppendLine(standardText);
                }

                return sbLocal.ToString();
            }
        }

        sb.AppendLine(config.RequirementsPrompt);
        sb.AppendLine(GetRequirementsDocument());
        return sb.ToString();

        string GetRequirementsDocument()
        {
            var sbLocal = new StringBuilder();
            foreach (var page in pages)
            {
                sbLocal.AppendLine($"## {page.Name}");
                sbLocal.AppendLine(page.Text);
                sbLocal.AppendLine();
            }

            return sbLocal.ToString();
        }
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