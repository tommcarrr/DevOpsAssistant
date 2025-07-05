using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DevOpsAssistant.Tests.Utils;

public class ConcurrencyTrackingHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;
    private int _current;

    public int MaxConcurrency { get; private set; }

    public ConcurrencyTrackingHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var count = Interlocked.Increment(ref _current);
        if (count > MaxConcurrency)
            MaxConcurrency = count;
        try
        {
            return await _handler(request);
        }
        finally
        {
            Interlocked.Decrement(ref _current);
        }
    }
}
