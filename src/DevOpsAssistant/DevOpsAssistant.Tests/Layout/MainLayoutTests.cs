using Bunit;
using DevOpsAssistant.Layout;
using DevOpsAssistant.Services;
using DevOpsAssistant.Tests.Utils;
using System.Threading.Tasks;

namespace DevOpsAssistant.Tests.Layout;

public class MainLayoutTests : ComponentTestBase
{
    [Fact]
    public void Layout_Has_PopoverProvider()
    {
        SetupServices();
        var cut = RenderComponent<MainLayout>();

        // Verify that the MudPopoverProvider is present in the rendered markup
        cut.Markup.Contains("mud-popover-provider");
    }

    [Fact]
    public async Task Layout_Uses_DarkMode_From_Config()
    {
        var config = SetupServices();
        await config.SaveAsync(new DevOpsConfig { DarkMode = true });

        var cut = RenderComponent<MainLayout>();

        Assert.Contains("--mud-native-html-color-scheme: dark", cut.Markup);
    }

    [Fact]
    public void Layout_Shows_Splash_When_Config_Incomplete()
    {
        SetupServices();

        var cut = RenderComponent<MainLayout>();

        Assert.Contains("Create Your First Project", cut.Markup);
        Assert.Contains("mud-nav-link-disabled", cut.Markup);
    }

    [Fact]
    public async Task Layout_Hides_Splash_When_Config_Complete()
    {
        var config = SetupServices();
        await config.SaveAsync(new DevOpsConfig { Organization = "Org", Project = "Proj", PatToken = "token" });

        var cut = RenderComponent<MainLayout>();

        Assert.DoesNotContain("Create Your First Project", cut.Markup);
        Assert.DoesNotContain("mud-nav-link-disabled", cut.Markup);
    }

    [Fact]
    public async Task SignOut_Clears_Config()
    {
        var config = SetupServices();
        await config.SaveAsync(new DevOpsConfig { Organization = "Org", Project = "Proj", PatToken = "token" });

        var cut = RenderComponent<MainLayout>();
        cut.Find("button[title='Sign Out']").Click();

        Assert.Contains("Create Your First Project", cut.Markup);
    }

    [Fact]
    public void Footer_Shows_Version()
    {
        SetupServices();

        var cut = RenderComponent<MainLayout>();

        cut.WaitForAssertion(() => Assert.Contains("Version 1.0", cut.Markup));
    }

    [Fact]
    public async Task Project_Select_Changes_Current_Project()
    {
        var config = SetupServices();
        await config.AddProjectAsync("One");
        await config.AddProjectAsync("Two");
        await config.SelectProjectAsync("One");
        var cut = RenderComponent<MainLayout>();
        var method = typeof(MainLayout).GetMethod("ChangeProject", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var task = cut.InvokeAsync(() => (Task)method.Invoke(cut.Instance, new object[] { "Two" })!);
        var dialog = cut.WaitForElement("div.mud-dialog");
        dialog.GetElementsByTagName("button")[0].Click();
        await task;

        Assert.Equal("Two", config.CurrentProject.Name);
    }
}
