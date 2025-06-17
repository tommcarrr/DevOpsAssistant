using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;

namespace DevOpsAssistant.UiTests;

public class UiTests
{
    private readonly string? _baseUrl = Environment.GetEnvironmentVariable("STAGING_URL");

    [Fact]
    public async Task Home_Shows_OpenSettings_Button()
    {
        if (string.IsNullOrEmpty(_baseUrl))
            return;

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        await page.GotoAsync(_baseUrl);
        var button = await page.QuerySelectorAsync("text=Open Settings");
        Assert.NotNull(button);
    }

    [Fact]
    public async Task SettingsDialog_Can_Open()
    {
        if (string.IsNullOrEmpty(_baseUrl))
            return;

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        await page.GotoAsync(_baseUrl);
        await page.ClickAsync("text=Open Settings");
        var dialog = await page.QuerySelectorAsync("text=Save");
        Assert.NotNull(dialog);
    }

    [Theory]
    [InlineData("/", "Welcome to DevOpsAssistant")]
    [InlineData("/metrics", "weekly throughput")]
    [InlineData("/release-notes", "release notes")]
    [InlineData("/requirements-planner", "break requirements")]
    [InlineData("/story-review", "reviewing user stories")]
    [InlineData("/validation", "validate work items")]
    [InlineData("/epics-features", "epics and features")]
    [InlineData("/help", "Help & Instructions")]
    public async Task Page_Loads_With_Expected_Content(string path, string text)
    {
        if (string.IsNullOrEmpty(_baseUrl))
            return;

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        await page.GotoAsync(_baseUrl.TrimEnd('/') + path);
        var element = await page.QuerySelectorAsync($"text={text}");
        Assert.NotNull(element);
    }

    [Theory]
    [InlineData("Epics & Features", "Epics and Features")]
    [InlineData("Release Notes", "Release Notes")]
    [InlineData("Story Validation", "Story Validation")]
    [InlineData("Story Quality", "Story Quality")]
    [InlineData("Metrics", "Metrics")]
    [InlineData("Requirement Planner", "Requirement Planner")]
    [InlineData("Branch Health", "Branch Health")]
    [InlineData("Help", "Help & Instructions")]
    public async Task Nav_Menu_Navigates_To_Correct_Page(string linkText, string heading)
    {
        if (string.IsNullOrEmpty(_baseUrl))
            return;

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        await page.GotoAsync(_baseUrl);
        await page.EvaluateAsync("localStorage.setItem('devops-config', JSON.stringify({ Organization: 'Org', Project: 'Proj', PatToken: 'Token' }))");
        await page.ReloadAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = linkText }).ClickAsync();
        var element = await page.QuerySelectorAsync($"text={heading}");
        Assert.NotNull(element);
    }

    [Fact]
    public async Task Settings_Can_Be_Saved()
    {
        if (string.IsNullOrEmpty(_baseUrl))
            return;

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        await page.GotoAsync(_baseUrl);
        await page.GetByText("Open Settings").ClickAsync();
        await page.GetByLabel("Organization").FillAsync("Org");
        await page.GetByLabel("Project").FillAsync("Proj");
        await page.GetByLabel("PAT Token").FillAsync("Token");
        await page.GetByText("Save").ClickAsync();
        var json = await page.EvaluateAsync<string>("() => localStorage.getItem('devops-config')") ?? string.Empty;
        Assert.Contains("Org", json);
        Assert.Contains("Proj", json);
    }

    [Fact]
    public async Task SignOut_Clears_Config()
    {
        if (string.IsNullOrEmpty(_baseUrl))
            return;

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        await page.GotoAsync(_baseUrl);
        await page.EvaluateAsync("localStorage.setItem('devops-config', JSON.stringify({ Organization: 'Org', Project: 'Proj', PatToken: 'Token' }))");
        await page.ReloadAsync();
        await page.GetByTitle("Sign Out").ClickAsync();
        var json = await page.EvaluateAsync<string>("() => localStorage.getItem('devops-config')");
        Assert.True(string.IsNullOrEmpty(json));
        var splash = await page.QuerySelectorAsync("text=Open Settings");
        Assert.NotNull(splash);
    }

    [Fact]
    public async Task RequirementsPlanner_Shows_WikiTree()
    {
        if (string.IsNullOrEmpty(_baseUrl))
            return;

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        await page.GotoAsync(_baseUrl);
        await page.EvaluateAsync("localStorage.setItem('devops-config', JSON.stringify({ Organization: 'Org', Project: 'Proj', PatToken: 'Token' }))");
        await page.ReloadAsync();
        await page.GotoAsync(_baseUrl.TrimEnd('/') + "/requirements-planner");
        var root = await page.WaitForSelectorAsync("text=Home");
        var child = await page.WaitForSelectorAsync("text=Setup");
        Assert.NotNull(root);
        Assert.NotNull(child);
    }

    [Fact]
    public async Task ReleaseNotes_Autocomplete_And_Generate()
    {
        if (string.IsNullOrEmpty(_baseUrl))
            return;

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var context = await browser.NewContextAsync();
        await context.RouteAsync("**/wiql?*", route => route.FulfillAsync(new RouteFulfillOptions
        {
            Status = 200,
            ContentType = "application/json",
            Body = "{\"workItems\":[{\"id\":1}]}"
        }));
        await context.RouteAsync("**/workitems?*", route => route.FulfillAsync(new RouteFulfillOptions
        {
            Status = 200,
            ContentType = "application/json",
            Body = "{\"value\":[{\"id\":1,\"fields\":{\"System.Title\":\"Story 1\",\"System.State\":\"New\",\"System.WorkItemType\":\"User Story\"}}]}"
        }));
        var page = await context.NewPageAsync();
        await page.GotoAsync(_baseUrl);
        await page.EvaluateAsync("localStorage.setItem('devops-config', JSON.stringify({ Organization: 'Org', Project: 'Proj', PatToken: 'Token' }))");
        await page.ReloadAsync();
        await page.GotoAsync(_baseUrl.TrimEnd('/') + "/release-notes");
        await page.FillAsync("input[aria-label='User Stories']", "Story");
        await page.WaitForSelectorAsync("div.mud-popover");
        await page.ClickAsync("text=Story 1");
        await page.WaitForSelectorAsync("text=Story 1");
        await page.ClickAsync("text=Generate Prompt");
        var prompt = await page.WaitForSelectorAsync("textarea");
        Assert.NotNull(prompt);
    }

    [Fact]
    public async Task Metrics_Load_Button_Shows_Table()
    {
        if (string.IsNullOrEmpty(_baseUrl))
            return;

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var context = await browser.NewContextAsync();
        await context.RouteAsync("**/classificationnodes/areas?*", route => route.FulfillAsync(new RouteFulfillOptions
        {
            Status = 200,
            ContentType = "application/json",
            Body = "{\"children\":[{\"path\":\"Project\\\\Area\"}]}"
        }));
        await context.RouteAsync("**/wiql?*", route => route.FulfillAsync(new RouteFulfillOptions
        {
            Status = 200,
            ContentType = "application/json",
            Body = "{\"workItems\":[{\"id\":1}]}"
        }));
        await context.RouteAsync("**/workitems?*", route => route.FulfillAsync(new RouteFulfillOptions
        {
            Status = 200,
            ContentType = "application/json",
            Body = "{\"value\":[{\"id\":1,\"fields\":{\"System.CreatedDate\":\"2024-01-01T00:00:00Z\",\"Microsoft.VSTS.Common.ActivatedDate\":\"2024-01-02T00:00:00Z\",\"Microsoft.VSTS.Common.ClosedDate\":\"2024-01-03T00:00:00Z\",\"Microsoft.VSTS.Scheduling.StoryPoints\":5,\"Microsoft.VSTS.Scheduling.OriginalEstimate\":8}}]}"
        }));
        var page = await context.NewPageAsync();
        await page.GotoAsync(_baseUrl);
        await page.EvaluateAsync("localStorage.setItem('devops-config', JSON.stringify({ Organization: 'Org', Project: 'Project', PatToken: 'Token' }))");
        await page.ReloadAsync();
        await page.GotoAsync(_baseUrl.TrimEnd('/') + "/metrics");
        await page.GetByRole(AriaRole.Button, new() { Name = "Load" }).ClickAsync();
        var table = await page.WaitForSelectorAsync("text=Period Ending");
        Assert.NotNull(table);
    }

    [Fact]
    public async Task BranchHealth_Load_Button_Shows_Table()
    {
        if (string.IsNullOrEmpty(_baseUrl))
            return;

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var context = await browser.NewContextAsync();
        await context.RouteAsync("**/git/repositories?*", route => route.FulfillAsync(new RouteFulfillOptions
        {
            Status = 200,
            ContentType = "application/json",
            Body = "{\"value\":[{\"id\":\"1\",\"name\":\"Repo\"}]}"
        }));
        await context.RouteAsync("**/stats/branches?*", route => route.FulfillAsync(new RouteFulfillOptions
        {
            Status = 200,
            ContentType = "application/json",
            Body = "{\"value\":[{\"name\":\"refs/heads/feature\",\"aheadCount\":1,\"behindCount\":2,\"commit\":{\"committer\":{\"date\":\"2024-01-01T00:00:00Z\"}}}]}"
        }));
        var page = await context.NewPageAsync();
        await page.GotoAsync(_baseUrl);
        await page.EvaluateAsync("localStorage.setItem('devops-config', JSON.stringify({ Organization: 'Org', Project: 'Proj', PatToken: 'Token', MainBranch: 'main' }))");
        await page.ReloadAsync();
        await page.GotoAsync(_baseUrl.TrimEnd('/') + "/branch-health");
        await page.GetByRole(AriaRole.Button, new() { Name = "Load" }).ClickAsync();
        var row = await page.WaitForSelectorAsync("text=feature");
        Assert.NotNull(row);
    }
}
