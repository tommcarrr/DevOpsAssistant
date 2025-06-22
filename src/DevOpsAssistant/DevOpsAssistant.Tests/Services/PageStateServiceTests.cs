using DevOpsAssistant.Services;
using DevOpsAssistant.Tests.Utils;

namespace DevOpsAssistant.Tests.Services;

public class PageStateServiceTests
{
    [Fact]
    public async Task Save_And_Load_State()
    {
        var storage = new FakeLocalStorageService();
        var config = new DevOpsConfigService(storage);
        await config.AddProjectAsync("proj");
        var service = new PageStateService(storage, config);
        await service.SaveAsync("key", new TestState { Value = 1 });
        var loaded = await service.LoadAsync<TestState>("key");
        Assert.NotNull(loaded);
        Assert.Equal(1, loaded!.Value);

        var stored = await storage.GetItemAsync<TestState>("proj-key");
        Assert.NotNull(stored);
    }

    private class TestState
    {
        public int Value { get; set; }
    }
}
