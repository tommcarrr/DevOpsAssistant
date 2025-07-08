using System.Text;
using GeneratedPrompts;
using DevOpsAssistant.Services.Models;
using DocumentFormat.OpenXml.Drawing.Diagrams;

namespace DevOpsAssistant.Services;

public class PromptService
{
    public static string BuildMetricsPrompt(string json, OutputFormat format)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Metrics_MainPrompt.Value);
        sb.AppendLine();
        sb.AppendLine(format == OutputFormat.Inline
            ? FormatInstructions_MetricsInlinePrompt.Value
            : string.Format(FormatInstructions_MetricsConvertPrompt.Value, format));
        sb.AppendLine();
        sb.AppendLine(string.Format(Metrics_DataIntroPrompt.Value, json));
        sb.AppendLine();
        return sb.ToString();
    }

    public static string BuildReleaseNotesPrompt(string json, DevOpsConfig config)
    {
        var sb = new StringBuilder();
        if (string.IsNullOrWhiteSpace(config.ReleaseNotesPrompt) || config.ReleaseNotesPromptMode == PromptMode.Append)
            sb.AppendLine(ReleaseNotes_MainPrompt.Value);
        if (!string.IsNullOrWhiteSpace(config.ReleaseNotesPrompt))
            sb.AppendLine(config.ReleaseNotesPrompt.Trim());
        sb.AppendLine(config.OutputFormat == OutputFormat.Inline
            ? FormatInstructions_ReleaseNotesInlinePrompt.Value
            : string.Format(FormatInstructions_ReleaseNotesConvertPrompt.Value, config.OutputFormat));
        sb.AppendLine();
        sb.AppendLine(ReleaseNotes_WorkItemsIntroPrompt.Value);
        sb.AppendLine(json);
        sb.AppendLine();
        return sb.ToString();
    }

    public static string BuildRequirementsGathererPrompt(IEnumerable<(string Name, string Text)> pages, DevOpsConfig config)
    {
        var sb = new StringBuilder();
        var pagesArray = pages as (string Name, string Text)[] ?? pages.ToArray();
        sb.AppendFormat(RequirementsGatherer_MainPrompt.Value,
            GetRequirementsGathererTemplate(config),
            BuildRequirementsDocument(pagesArray));

        return sb.ToString();
    }

    private static string GetRequirementsGathererTemplate(DevOpsConfig config)
    {
        if (config.Standards.RequirementsDocumentation.Count == 0) return RequirementsGatherer_Template_GeneralPrompt.Value;

        var sb = new StringBuilder();
        foreach (var text in config.Standards.RequirementsDocumentation.Select(standard => standard switch
                 {
                     "ISO29148" => RequirementsGatherer_Template_ISO_IEC_IEEE_29148_2018Prompt.Value,
                     "Volare" => RequirementsGatherer_Template_VolarePrompt.Value,
                     "BABOK" => RequirementsGatherer_Template_BABOKPrompt.Value,
                     "ISO25010" => RequirementsGatherer_Template_ISO_IEC_25010Prompt.Value,
                     _ => RequirementsGatherer_Template_GeneralPrompt.Value
                 }))
        {
            sb.AppendLine(text);
        }
        return sb.ToString();
    }

    public static string BuildRequirementsQualityPrompt(IEnumerable<(string Name, string Text)> pages, DevOpsConfig config)
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

    public static string BuildRequirementsPlannerPrompt(
        IEnumerable<(string Name, string Text)> pages,
        bool storiesOnly,
        bool clarify,
        DevOpsConfig config)
    {
        var sb = new StringBuilder();
        var requirementsDocument = BuildRequirementsDocument(pages);

        if (string.IsNullOrWhiteSpace(config.RequirementsPrompt) ||
            config.RequirementsPromptMode == PromptMode.Append)
        {
            sb.AppendFormat(
                RequirementsPlanner_MainPrompt.Value,
                storiesOnly
                    ? RequirementsPlanner_StoriesOnlyPrompt.Value
                    : RequirementsPlanner_EpicsFeaturesStoriesPrompt.Value,
                config.WorkItemGranularity.ToString(),
                BuildWorkItemStandards(config),
                BuildWorkItemDescriptionStandards(config),
                BuildWorkItemAcStandards(config),
                BuildNfrs(config),
                clarify ? RequirementsPlanner_ClarifyRequirementsPrompt.Value : RequirementsPlanner_ClarifyRequirementsNonePrompt.Value,
                ShouldAppendPrompt(config) ? config.RequirementsPrompt : string.Empty,
                requirementsDocument
                );
        }
        else
        {
            sb.AppendLine(config.RequirementsPrompt);
            sb.AppendLine(requirementsDocument);
        }

        return sb.ToString();
    }

    private static bool ShouldAppendPrompt(DevOpsConfig config) =>
        !string.IsNullOrWhiteSpace(config.RequirementsPrompt) &&
        config.RequirementsPromptMode == PromptMode.Append;

    private static string BuildRequirementsDocument(IEnumerable<(string Name, string Text)> pages)
    {
        var sb = new StringBuilder();
        foreach (var page in pages)
        {
            sb.AppendLine($"## {page.Name}");
            sb.AppendLine(page.Text);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string BuildNfrs(DevOpsConfig config)
    {
        if (config.Nfrs.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine(RequirementsPlanner_NonFunctionalIntroPrompt.Value);
        foreach (var nfr in config.Nfrs)
            sb.AppendLine($"- {nfr}");
        return sb.ToString();
    }

    private static string BuildWorkItemAcStandards(DevOpsConfig config)
    {
        if (config.Standards.UserStoryAcceptanceCriteria.Count <= 0) return RequirementsPlanner_WorkItemACStandardsNonePrompt.Value;

        var sb = new StringBuilder();
        sb.AppendLine(RequirementsPlanner_WorkItemACStandardsPrompt.Value);
        foreach (var standardText in config.Standards.UserStoryAcceptanceCriteria.Select(s => s switch
                 {
                     "Gherkin" => RequirementsPlanner_WorkItemACStandards_GherkinPrompt.Value,
                     "BulletPoints" => RequirementsPlanner_WorkItemACStandards_BulletPointsPrompt.Value,
                     "SAFeStyle" => RequirementsPlanner_WorkItemACStandards_SAFePrompt.Value,
                     _ => string.Empty
                 }))
        {
            sb.AppendLine(standardText);
        }

        return sb.ToString();
    }

    private static string BuildWorkItemDescriptionStandards(DevOpsConfig config)
    {
        if (config.Standards.UserStoryDescription.Count <= 0) return RequirementsPlanner_WorkItemDescriptionStandardsNonePrompt.Value;

        var sb = new StringBuilder();
        sb.AppendLine(RequirementsPlanner_WorkItemDescriptionStandardsPrompt.Value);
        foreach (var standardText in config.Standards.UserStoryDescription.Select(s => s switch
                 {
                     "ScrumUserStory" => RequirementsPlanner_WorkItemDescriptionStandards_ScrumUserStoryPrompt.Value,
                     "JobStory" => RequirementsPlanner_WorkItemDescriptionStandards_JobStoryPrompt.Value,
                     _ => string.Empty
                 }))
        {
            sb.AppendLine(standardText);
        }

        return sb.ToString();
    }

    private static string BuildWorkItemStandards(DevOpsConfig config)
    {
        if (config.Standards.UserStoryQuality.Count <= 0) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine(RequirementsPlanner_WorkItemStandardsPrompt.Value);
        foreach (var standardText in config.Standards.UserStoryQuality.Select(s => s switch
                 {
                     "INVEST" => RequirementsPlanner_WorkItemStandards_INVESTPrompt.Value,
                     "SAFe" => RequirementsPlanner_WorkItemStandards_SAFePrompt.Value,
                     "AgileAlliance" => RequirementsPlanner_WorkItemStandards_AgileAlliancePrompt.Value,
                     _ => string.Empty
                 }))
        {
            sb.AppendLine(standardText);
        }

        return sb.ToString();
    }

    public static string BuildWorkItemQualityPrompt(string json, DevOpsConfig config)
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
        sb.AppendLine(config.OutputFormat == OutputFormat.Inline
            ? FormatInstructions_WorkItemAnalysisInlinePrompt.Value
            : string.Format(FormatInstructions_WorkItemAnalysisConvertPrompt.Value, config.OutputFormat));
        sb.AppendLine();
        sb.AppendLine(WorkItemQuality_WorkItemsIntroPrompt.Value);
        sb.AppendLine(json);
        sb.AppendLine();
        return sb.ToString();
    }
}