using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Blazored.LocalStorage;

namespace DevOpsAssistant.Tests;

public class FakeLocalStorageService : ILocalStorageService
{
    private readonly Dictionary<string, string?> _store = new();

    public event EventHandler<ChangingEventArgs>? Changing;
    public event EventHandler<ChangedEventArgs>? Changed;

    public ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        _store.Clear();
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> ContainKeyAsync(string key, CancellationToken cancellationToken = default)
        => new(_store.ContainsKey(key));

    public ValueTask<T> GetItemAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (_store.TryGetValue(key, out var json))
        {
            var value = System.Text.Json.JsonSerializer.Deserialize<T>(json!);
            return new ValueTask<T>(value!);
        }
        return new ValueTask<T>(default(T)!);
    }

    public ValueTask<string> GetItemAsStringAsync(string key, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(key, out var value);
        return new ValueTask<string>(value!);
    }

    public ValueTask<string> KeyAsync(int index, CancellationToken cancellationToken = default)
        => new(_store.Keys.ElementAt(index));

    public ValueTask<IEnumerable<string>> KeysAsync(CancellationToken cancellationToken = default)
        => new(_store.Keys.AsEnumerable());

    public ValueTask<int> LengthAsync(CancellationToken cancellationToken = default)
        => new(_store.Count);

    public ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default)
    {
        _store.Remove(key);
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveItemsAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        foreach (var key in keys)
        {
            _store.Remove(key);
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask SetItemAsync<T>(string key, T data, CancellationToken cancellationToken = default)
    {
        _store[key] = System.Text.Json.JsonSerializer.Serialize(data);
        return ValueTask.CompletedTask;
    }

    public ValueTask SetItemAsStringAsync(string key, string data, CancellationToken cancellationToken = default)
    {
        _store[key] = data;
        return ValueTask.CompletedTask;
    }
}
