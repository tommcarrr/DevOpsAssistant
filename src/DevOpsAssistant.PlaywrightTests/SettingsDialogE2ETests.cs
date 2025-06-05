using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;

namespace DevOpsAssistant.PlaywrightTests;

public class SettingsDialogE2ETests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private Process? _web;

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });

        var startInfo = new ProcessStartInfo("dotnet", "run --project ../../DevOpsAssistant/DevOpsAssistant.csproj --urls http://localhost:5055")
        {
            WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "..", "DevOpsAssistant"),
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        _web = Process.Start(startInfo)!;
        // wait for server to start
        var tcs = new TaskCompletionSource();
        _web.OutputDataReceived += (s, e) =>
        {
            if (e.Data != null && e.Data.Contains("Now listening"))
                tcs.TrySetResult();
        };
        _web.BeginOutputReadLine();
        await Task.WhenAny(tcs.Task, Task.Delay(10000));
    }

    public async Task DisposeAsync()
    {
        if (_web != null && !_web.HasExited)
            _web.Kill();
        if (_browser != null)
            await _browser.CloseAsync();
        _playwright?.Dispose();
    }

    [Fact]
    public async Task SettingsDialog_Loads_Controls()
    {
        if (_browser == null) throw new InvalidOperationException();
        var page = await _browser.NewPageAsync();
        await page.GotoAsync("http://localhost:5055");

        await page.Locator("button[aria-label='Settings']").ClickAsync();
        await page.GetByLabel("Organization").IsVisibleAsync();
        await page.GetByLabel("Project").IsVisibleAsync();
        await page.GetByLabel("PAT Token").IsVisibleAsync();
        await page.GetByLabel("Dark Mode").IsVisibleAsync();
    }
}
