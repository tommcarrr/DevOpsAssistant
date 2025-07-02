using System.Reflection;
using System.Net;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using Bunit;
using DevOpsAssistant.Pages;
using DevOpsAssistant.Services;
using DevOpsAssistant.Services.Models;
using DevOpsAssistant.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace DevOpsAssistant.Tests.Pages;

public class ReleaseNotesPageTests : ComponentTestBase
{

    [Fact]
    public void BuildPrompt_Includes_Acceptance_Criteria()
    {
        var details = new List<StoryHierarchyDetails>
        {
            new()
            {
                Story = new WorkItemInfo { Id = 1, Title = "Story", WorkItemType = "User Story" },
                AcceptanceCriteria = "<b>criteria</b>"
            }
        };

        var method = typeof(ReleaseNotes).GetMethod("BuildPrompt", BindingFlags.NonPublic | BindingFlags.Static)!;
        var result = (string)method.Invoke(null, [details, new DevOpsConfig()])!;

        Assert.Contains("\"AcceptanceCriteria\": \"criteria\"", result);
    }

    [Fact]
    public void BuildPrompt_Includes_Bug_Note()
    {
        var details = new List<StoryHierarchyDetails>
        {
            new()
            {
                Story = new WorkItemInfo { Id = 1, Title = "Bug", WorkItemType = "Bug" }
            }
        };

        var method = typeof(ReleaseNotes).GetMethod("BuildPrompt", BindingFlags.NonPublic | BindingFlags.Static)!;
        var result = (string)method.Invoke(null, [details, new DevOpsConfig()])!;

        Assert.Contains("Bugs are also in scope", result);
    }

    [Fact]
    public void BuildPrompt_Includes_NoBranding_Note()
    {
        var method = typeof(ReleaseNotes).GetMethod("BuildPrompt", BindingFlags.NonPublic | BindingFlags.Static)!;
        var result = (string)method.Invoke(null, [new List<StoryHierarchyDetails>(), new DevOpsConfig()])!;

        Assert.Contains("No branding is required.", result);
    }

    [Fact]
    public void BuildPrompt_Excludes_Repro_When_Disabled()
    {
        var details = new List<StoryHierarchyDetails>
        {
            new()
            {
                Story = new WorkItemInfo { Id = 1, Title = "Bug", WorkItemType = "Bug" },
                ReproSteps = "steps"
            }
        };

        var cfg = new DevOpsConfig
        {
            Rules = new ValidationRules
            {
                Bug = new BugRules { IncludeReproSteps = false }
            }
        };
        var method = typeof(ReleaseNotes).GetMethod("BuildPrompt", BindingFlags.NonPublic | BindingFlags.Static)!;
        var result = (string)method.Invoke(null, [details, cfg])!;

        Assert.DoesNotContain("ReproSteps", result);
    }

    [Fact]
    public void BuildPrompt_Excludes_SystemInfo_When_Disabled()
    {
        var details = new List<StoryHierarchyDetails>
        {
            new()
            {
                Story = new WorkItemInfo { Id = 1, Title = "Bug", WorkItemType = "Bug" },
                SystemInfo = "info"
            }
        };

        var cfg = new DevOpsConfig
        {
            Rules = new ValidationRules
            {
                Bug = new BugRules { IncludeSystemInfo = false }
            }
        };
        var method = typeof(ReleaseNotes).GetMethod("BuildPrompt", BindingFlags.NonPublic | BindingFlags.Static)!;
        var result = (string)method.Invoke(null, [details, cfg])!;

        Assert.DoesNotContain("SystemInfo", result);
    }

    [Fact]
    public void BuildPrompt_Inline_Format_Does_Not_Request_Conversion()
    {
        var method = typeof(ReleaseNotes).GetMethod("BuildPrompt", BindingFlags.NonPublic | BindingFlags.Static)!;
        var cfg = new DevOpsConfig { OutputFormat = OutputFormat.Inline };

        var result = (string)method.Invoke(null, [new List<StoryHierarchyDetails>(), cfg])!;

        Assert.DoesNotContain("convert the content", result);
        Assert.Contains("Reply inline", result);
    }

    [Fact]
    public void BuildPrompt_Includes_Templates()
    {
        var method = typeof(ReleaseNotes).GetMethod("BuildPrompt", BindingFlags.NonPublic | BindingFlags.Static)!;

        var result = (string)method.Invoke(null, [new List<StoryHierarchyDetails>(), new DevOpsConfig()])!;

        Assert.Contains("# Release Notes", result);
        Assert.Contains("# Change Control Ticket", result);
        Assert.Contains("clarifying questions", result);
    }

    // Rendering uses ComponentTestBase.RenderWithProvider
}
