using Microsoft.JSInterop;

namespace DevOpsAssistant.Tests.Utils;

public class FakeJSRuntime : IJSRuntime
{
    public readonly List<string> InvokedIdentifiers = new();

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        InvokedIdentifiers.Add(identifier);
        return ValueTask.FromResult(default(TValue)!);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        return InvokeAsync<TValue>(identifier, args);
    }
}
