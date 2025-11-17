using System.Net;

namespace Fgvm.Cli.Http;

internal sealed class ExponentialBackoffHandler : DelegatingHandler
{
    private readonly TimeSpan _initialDelay;
    private readonly int _maxRetries;

    public ExponentialBackoffHandler(TimeSpan initialDelay, int maxRetries)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);

        _initialDelay = initialDelay;
        _maxRetries = maxRetries;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var attempt = 0;
        var delay = _initialDelay;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var response = await base.SendAsync(request, cancellationToken);

                if (!ShouldRetry(response.StatusCode) || attempt >= _maxRetries)
                {
                    return response;
                }

                response.Dispose();
            }
            catch (HttpRequestException) when (attempt < _maxRetries)
            {
                // swallowed intentionally to retry
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested && attempt < _maxRetries)
            {
                // Timeout or transient cancellation; retry
                if (ex.InnerException is OperationCanceledException && cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
            }

            attempt++;

            try
            {
                await Task.Delay(delay, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            delay = TimeSpan.FromTicks(delay.Ticks * 2);
        }
    }

    private static bool ShouldRetry(HttpStatusCode statusCode) =>
        statusCode == HttpStatusCode.RequestTimeout ||
        statusCode == (HttpStatusCode)429 || // Too Many Requests
        (int)statusCode >= 500;
}
