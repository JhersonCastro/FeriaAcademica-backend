using System.Text.Json;
using FeriaAcademica.Infrastructure.Realtime;
using Microsoft.AspNetCore.Http.Features;

namespace FeriaAcademica.Api.Realtime;

/// <summary>Writes an open connection to the client as Server-Sent Events until it disconnects.</summary>
internal static class SseResponse
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static async Task WriteAsync(HttpContext http, RealtimeSubscription subscription, TimeSpan heartbeat)
    {
        var ct = http.RequestAborted;
        var response = http.Response;
        response.ContentType = "text/event-stream";
        response.Headers.CacheControl = "no-cache";
        response.Headers["X-Accel-Buffering"] = "no";   // tells nginx not to hold the events back
        http.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

        try
        {
            await response.WriteAsync(": connected\n\n", ct);   // sends the headers right away, so the browser sees the stream open
            await response.Body.FlushAsync(ct);

            while (!ct.IsCancellationRequested)
            {
                using var idle = CancellationTokenSource.CreateLinkedTokenSource(ct);
                idle.CancelAfter(heartbeat);
                try
                {
                    if (!await subscription.Reader.WaitToReadAsync(idle.Token)) return;
                    while (subscription.Reader.TryRead(out var message))
                        await response.WriteAsync($"event: {message.Event}\ndata: {JsonSerializer.Serialize(message.Data, Json)}\n\n", ct);
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    await response.WriteAsync(": ping\n\n", ct);   // nothing happened for a while
                }
                await response.Body.FlushAsync(ct);
            }
        }
        catch (OperationCanceledException)
        {
            // The client closed the connection: nothing to report.
        }
    }
}
