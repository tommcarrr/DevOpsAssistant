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
        sb.AppendFormat(
            Metrics_MainPrompt.Value,
            format == OutputFormat.Inline
                ? FormatInstructions_MetricsInlinePrompt.Value
                : string.Format(FormatInstructions_MetricsConvertPrompt.Value, format),
            json);
        sb.AppendLine();
        return sb.ToString();
    }

    public static string BuildReleaseNotesPrompt(string json, DevOpsConfig config)
    {
        var sb = new StringBuilder();
        var format = config.OutputFormat == OutputFormat.Inline
            ? FormatInstructions_ReleaseNotesInlinePrompt.Value
            : string.Format(FormatInstructions_ReleaseNotesConvertPrompt.Value, config.OutputFormat);

        var workItems = BuildReleaseNotesWorkItems(json);

        if (string.IsNullOrWhiteSpace(config.ReleaseNotesPrompt) || config.ReleaseNotesPromptMode == PromptMode.Append)
        {
            sb.AppendFormat(ReleaseNotes_MainPrompt.Value, format, workItems);
            if (!string.IsNullOrWhiteSpace(config.ReleaseNotesPrompt) && config.ReleaseNotesPromptMode == PromptMode.Append)
            {
                sb.AppendLine();
                sb.AppendLine(config.ReleaseNotesPrompt.Trim());
            }
        }
        else
        {
            sb.AppendLine(config.ReleaseNotesPrompt.Trim());
            sb.AppendLine();
            sb.AppendLine(format);
            sb.Append(workItems);
        }

        return sb.ToString();
    }

    public static string BuildRequirementsGathererPrompt(IEnumerable<DocumentItem> pages, DevOpsConfig config)
    {
        var sb = new StringBuilder();
        var pagesArray = pages as DocumentItem[] ?? pages.ToArray();
        sb.AppendFormat(
            RequirementsGatherer_MainPrompt.Value,
            GetRequirementsGathererTemplate(config),
            BuildRequirementsDocument(pagesArray),
            config.OutputFormat == OutputFormat.Inline
                ? FormatInstructions_RequirementsGathererInlinePrompt.Value
                : string.Format(FormatInstructions_RequirementsGathererConvertPrompt.Value, config.OutputFormat));
        sb.AppendLine();
        return sb.ToString();
    }

    private static string GetRequirementsGathererTemplate(DevOpsConfig config)
    {
        if (config.Standards.RequirementsDocumentation.Count == 0) return RequirementsGatherer_Template_GeneralPrompt.Value;

        var sb = new StringBuilder();
        foreach (var text in config.Standards.RequirementsDocumentation.Select(standard => standard switch
                 {
                     StandardIds.ISO29148 => RequirementsGatherer_Template_ISO_IEC_IEEE_29148_2018Prompt.Value,
                     StandardIds.Volere => RequirementsGatherer_Template_VolerePrompt.Value,
                     StandardIds.BABOK => RequirementsGatherer_Template_BABOKPrompt.Value,
                     StandardIds.ISO25010 => RequirementsGatherer_Template_ISO_IEC_25010Prompt.Value,
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
        sb.AppendFormat(
            RequirementsQuality_MainPrompt.Value,
            BuildRequirementsDocumentationStandards(config),
            BuildQualityDocument(pages));
        return sb.ToString();
    }

    public static string BuildRequirementsPlannerPrompt(
        IEnumerable<DocumentItem> requirementPages,
        IEnumerable<DocumentItem> contextPages,
        bool storiesOnly,
        bool clarify,
        DevOpsConfig config)
    {
        var sb = new StringBuilder();
        var contextDocument = BuildRequirementsDocument(contextPages);
        var requirementsDocument = BuildRequirementsDocument(requirementPages);

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
                contextDocument,
                requirementsDocument
                );
        }
        else
        {
            sb.AppendLine(config.RequirementsPrompt);
            if (!string.IsNullOrWhiteSpace(contextDocument))
                sb.AppendLine(contextDocument);
            sb.AppendLine(requirementsDocument);
        }

        return sb.ToString();
    }

    private static bool ShouldAppendPrompt(DevOpsConfig config) =>
        !string.IsNullOrWhiteSpace(config.RequirementsPrompt) &&
        config.RequirementsPromptMode == PromptMode.Append;

    private static string BuildRequirementsDocument(IEnumerable<DocumentItem> pages)
    {
        var pagesArray = pages as DocumentItem[] ?? pages.ToArray();
        if (pagesArray.Length == 0) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine(RequirementsGatherer_DocumentIntroPrompt.Value);

        foreach (var page in pagesArray)
        {
            if (!string.IsNullOrWhiteSpace(page.Path))
                sb.AppendLine($"Wiki Path: {page.Path}");
            sb.AppendLine($"Page Name: {page.Name}");
            sb.AppendLine("Contents:");
            sb.AppendLine("```markdown");
            sb.AppendLine(page.Text);
            sb.AppendLine("```");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string BuildNfrs(DevOpsConfig config)
    {
        if (config.Nfrs.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine(config.CoverNfrs
            ? RequirementsPlanner_NonFunctionalCoverPrompt.Value
            : RequirementsPlanner_NonFunctionalIgnorePrompt.Value);
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
                     StandardIds.Gherkin => RequirementsPlanner_WorkItemACStandards_GherkinPrompt.Value,
                     StandardIds.BulletPoints => RequirementsPlanner_WorkItemACStandards_BulletPointsPrompt.Value,
                     StandardIds.SAFeStyle => RequirementsPlanner_WorkItemACStandards_SAFePrompt.Value,
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
                     StandardIds.ScrumUserStory => RequirementsPlanner_WorkItemDescriptionStandards_ScrumUserStoryPrompt.Value,
                     StandardIds.JobStory => RequirementsPlanner_WorkItemDescriptionStandards_JobStoryPrompt.Value,
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
                     StandardIds.INVEST => RequirementsPlanner_WorkItemStandards_INVESTPrompt.Value,
                     StandardIds.SAFe => RequirementsPlanner_WorkItemStandards_SAFePrompt.Value,
                     StandardIds.AgileAlliance => RequirementsPlanner_WorkItemStandards_AgileAlliancePrompt.Value,
                     _ => string.Empty
                 }))
        {
            sb.AppendLine(standardText);
        }

        return sb.ToString();
    }

    private static string BuildReleaseNotesWorkItems(string json)
    {
        var sb = new StringBuilder();
        sb.AppendLine(ReleaseNotes_WorkItemsIntroPrompt.Value);
        sb.AppendLine(json);
        sb.AppendLine();
        return sb.ToString();
    }

    private static string BuildRequirementsDocumentationStandards(DevOpsConfig config)
    {
        if (config.Standards.RequirementsDocumentation.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine(RequirementsDocumentationStandardsIntroPrompt.Value);
        foreach (var text in config.Standards.RequirementsDocumentation.Select(s => s switch
                 {
                     StandardIds.ISO29148 => RequirementsDocumentationStandards_ISO29148Prompt.Value,
                     StandardIds.Volere => RequirementsDocumentationStandards_VolerePrompt.Value,
                     StandardIds.BABOK => RequirementsDocumentationStandards_BABOKPrompt.Value,
                     StandardIds.ISO25010 => RequirementsDocumentationStandards_ISO25010Prompt.Value,
                     _ => string.Empty
                 }))
        {
            sb.AppendLine(text);
        }
        return sb.ToString();
    }

    private static string BuildQualityDocument(IEnumerable<(string Name, string Text)> pages)
    {
        var sb = new StringBuilder();
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

    private static string BuildBugReportingStandards(DevOpsConfig config)
    {
        if (config.Standards.BugReporting.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine(WorkItemQuality_BugReportingStandardsIntroPrompt.Value);
        foreach (var text in config.Standards.BugReporting.Select(s => s switch
                 {
                     StandardIds.AzureDevOpsBug => WorkItemQuality_BugReportingStandards_AzureDevOpsBugPrompt.Value,
                     StandardIds.ISTQBDefect => WorkItemQuality_BugReportingStandards_ISTQBDefectPrompt.Value,
                     _ => string.Empty
                 }))
        {
            sb.AppendLine(text);
        }
        return sb.ToString();
    }

    private static string BuildDefinitionOfReady(DevOpsConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.DefinitionOfReady)) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine(WorkItemQuality_DefinitionOfReadyIntroPrompt.Value);
        sb.AppendLine(config.DefinitionOfReady);
        return sb.ToString();
    }

    private static string BuildStoryQualityStandards(DevOpsConfig config)
    {
        if (config.Standards.UserStoryQuality.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine(WorkItemQuality_StoryQualityStandardsIntroPrompt.Value);
        foreach (var text in config.Standards.UserStoryQuality.Select(s => s switch
                 {
                     StandardIds.INVEST => WorkItemQuality_StoryQualityStandards_INVESTPrompt.Value,
                     StandardIds.SAFe => WorkItemQuality_StoryQualityStandards_SAFePrompt.Value,
                     StandardIds.AgileAlliance => WorkItemQuality_StoryQualityStandards_AgileAlliancePrompt.Value,
                     _ => string.Empty
                 }))
        {
            sb.AppendLine(text);
        }
        return sb.ToString();
    }

    private static string BuildWorkItemQualityWorkItems(string json)
    {
        var sb = new StringBuilder();
        sb.AppendLine(WorkItemQuality_WorkItemsIntroPrompt.Value);
        sb.AppendLine(json);
        sb.AppendLine();
        return sb.ToString();
    }

    public static string BuildWorkItemQualityPrompt(string json, DevOpsConfig config)
    {
        var sb = new StringBuilder();
        var format = config.OutputFormat == OutputFormat.Inline
            ? FormatInstructions_WorkItemAnalysisInlinePrompt.Value
            : string.Format(FormatInstructions_WorkItemAnalysisConvertPrompt.Value, config.OutputFormat);

        var workItems = BuildWorkItemQualityWorkItems(json);

        if (string.IsNullOrWhiteSpace(config.StoryQualityPrompt) || config.StoryQualityPromptMode == PromptMode.Append)
        {
            sb.AppendFormat(
                WorkItemQuality_MainPrompt.Value,
                BuildBugReportingStandards(config),
                BuildDefinitionOfReady(config),
                BuildStoryQualityStandards(config),
                format,
                workItems);

            if (!string.IsNullOrWhiteSpace(config.StoryQualityPrompt) && config.StoryQualityPromptMode == PromptMode.Append)
            {
                sb.AppendLine();
                sb.AppendLine(config.StoryQualityPrompt.Trim());
            }
        }
        else
        {
            sb.AppendLine(config.StoryQualityPrompt.Trim());
            sb.AppendLine();
            sb.AppendLine(format);
            sb.Append(workItems);
        }

        return sb.ToString();
    }
}