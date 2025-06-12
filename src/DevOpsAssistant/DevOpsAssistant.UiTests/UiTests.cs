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
        var item = await page.WaitForSelectorAsync("text=Home");
        Assert.NotNull(item);
    }

    [Fact]
    public async Task RequirementsPlanner_Shows_Tree_With_Alt_Wiki_Format()
    {
        if (string.IsNullOrEmpty(_baseUrl))
            return;

        var altJson = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "wiki-tree-alt.json"));

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        await page.RouteAsync("**/wiki-tree.json", route => route.FulfillAsync(new RouteFulfillOptions
        {
            Body = altJson,
            ContentType = "application/json"
        }));
        await page.GotoAsync(_baseUrl);
        await page.EvaluateAsync("localStorage.setItem('devops-config', JSON.stringify({ Organization: 'Org', Project: 'Proj', PatToken: 'Token' }))");
        await page.ReloadAsync();
        await page.GotoAsync(_baseUrl.TrimEnd('/') + "/requirements-planner");
        var item = await page.WaitForSelectorAsync("text=Home");
        Assert.NotNull(item);
    }
}
