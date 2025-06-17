using DevOpsAssistant.Services;

namespace DevOpsAssistant.Tests;

public class PageStateServiceTests
{
    [Fact]
    public async Task Save_And_Load_State()
    {
        var storage = new FakeLocalStorageService();
        var service = new PageStateService(storage);
        await service.SaveAsync("key", new TestState { Value = 1 });
        var loaded = await service.LoadAsync<TestState>("key");
        Assert.NotNull(loaded);
        Assert.Equal(1, loaded!.Value);
    }

    private class TestState
    {
        public int Value { get; set; }
    }
}
