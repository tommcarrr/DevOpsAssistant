using Bunit;
using DevOpsAssistant.Pages;
using DevOpsAssistant.Tests.Utils;
using DevOpsAssistant.Services;
using Microsoft.AspNetCore.Components;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;

namespace DevOpsAssistant.Tests.Pages;

public class HomePageTests : ComponentTestBase
{
    [Fact]
    public async Task Home_Shows_Description_When_Project_Selected()
    {
        var config = SetupServices();
        await config.SaveAsync(new DevOpsConfig { Organization = "Org", Project = "Proj", PatToken = "token" });
        var cut = RenderComponent<Home>(p => p.Add(c => c.ProjectName, config.CurrentProject.Name));

        Assert.Contains("DevOpsAssistant provides", cut.Markup);
    }

    [Fact]
    public async Task Home_Shows_Project_List_When_No_Project_Selected()
    {
        var config = SetupServices();
        await config.SaveAsync(new DevOpsConfig { Organization = "Org", Project = "Proj", PatToken = "token" });

        var nav = Services.GetRequiredService<NavigationManager>() as FakeNavigationManager;
        var cut = RenderComponent<Home>();

        Assert.Equal("http://localhost/projects/new", nav!.Uri);
    }
}
