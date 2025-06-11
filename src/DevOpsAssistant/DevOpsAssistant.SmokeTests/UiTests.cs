using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;

namespace DevOpsAssistant.SmokeTests;

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
}
