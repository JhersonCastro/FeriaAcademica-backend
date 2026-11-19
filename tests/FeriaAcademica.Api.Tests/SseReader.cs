using System.Text.Json;
using System.Threading.Channels;

namespace FeriaAcademica.Api.Tests;

/// <summary>Opens a Server-Sent Events endpoint and lets a test read what arrives: events with their JSON data, and comment lines (heartbeats).</summary>
internal sealed class SseReader : IAsyncDisposable
{
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);

    private readonly HttpResponseMessage _response;
    private readonly CancellationTokenSource _stop = new();
    private readonly Channel<(string Event, JsonElement Data)> _events = Channel.CreateUnbounded<(string, JsonElement)>();
    private readonly Channel<string> _comments = Channel.CreateUnbounded<string>();
    private readonly Task _pump;

    private SseReader(HttpResponseMessage response)
    {
        _response = response;
        _pump = Task.Run(PumpAsync);
    }

    /// <summary>The headers of the response, to check the transport.</summary>
    public HttpResponseMessage Response => _response;

    public static async Task<SseReader> OpenAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        return new SseReader(response);
    }

    private async Task PumpAsync()
    {
        try
        {
            using var reader = new StreamReader(await _response.Content.ReadAsStreamAsync(_stop.Token));
            string? name = null, data = null;
            while (await reader.ReadLineAsync(_stop.Token) is { } line)
            {
                if (line.Length == 0)
                {
                    if (name is not null && data is not null) _events.Writer.TryWrite((name, JsonDocument.Parse(data).RootElement.Clone()));
                    name = data = null;
                }
                else if (line.StartsWith(':')) _comments.Writer.TryWrite(line);
                else if (line.StartsWith("event: ")) name = line["event: ".Length..];
                else if (line.StartsWith("data: ")) data = line["data: ".Length..];
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or IOException or ObjectDisposedException)
        {
            // The connection was closed.
        }
        finally
        {
            _events.Writer.TryComplete();
            _comments.Writer.TryComplete();
        }
    }

    public async Task<(string Event, JsonElement Data)> NextAsync()
    {
        using var timeout = new CancellationTokenSource(Wait);
        try { return await _events.Reader.ReadAsync(timeout.Token); }
        catch (OperationCanceledException) { throw new Xunit.Sdk.XunitException("No event arrived within 5 seconds."); }
    }

    public async Task<string> NextCommentAsync()
    {
        using var timeout = new CancellationTokenSource(Wait);
        try { return await _comments.Reader.ReadAsync(timeout.Token); }
        catch (OperationCanceledException) { throw new Xunit.Sdk.XunitException("No heartbeat arrived within 5 seconds."); }
    }

    /// <summary>Asserts that nothing is pushed in the next moments.</summary>
    public async Task ExpectSilenceAsync(int milliseconds = 500)
    {
        using var timeout = new CancellationTokenSource(milliseconds);
        try
        {
            var unexpected = await _events.Reader.ReadAsync(timeout.Token);
            throw new Xunit.Sdk.XunitException($"Unexpected event '{unexpected.Event}': {unexpected.Data}");
        }
        catch (OperationCanceledException) { }
    }

    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        _response.Dispose();
        await _pump;
    }
}
