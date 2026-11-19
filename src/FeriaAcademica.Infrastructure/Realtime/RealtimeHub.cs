using System.Threading.Channels;
using FeriaAcademica.Application.Abstractions;

namespace FeriaAcademica.Infrastructure.Realtime;

public sealed record RealtimeMessage(string Event, object Data);

/// <summary>
/// In-memory fan-out of messages to the open SSE connections, one channel per connection, keyed by user or by event.
/// It only works with a single API instance: with several, move it to Redis pub/sub or Postgres LISTEN/NOTIFY.
/// </summary>
public sealed class RealtimeHub : IRealtimePublisher
{
    private const int MaxPendingPerConnection = 64;

    private readonly object _gate = new();
    private readonly Dictionary<string, List<Subscription>> _subscriptions = [];

    /// <summary>How often an idle connection gets a comment line, so proxies and browsers do not consider it dead.</summary>
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(15);

    public RealtimeSubscription SubscribeToUser(Guid userId) => Subscribe(UserKey(userId));
    public RealtimeSubscription SubscribeToEvent(Guid eventId) => Subscribe(EventKey(eventId));

    public void ToUser(Guid userId, string eventName, object data) => Publish(UserKey(userId), new RealtimeMessage(eventName, data));
    public void ToEvent(Guid eventId, string eventName, object data) => Publish(EventKey(eventId), new RealtimeMessage(eventName, data));

    /// <summary>Open connections that follow the user's notifications (for diagnostics and tests).</summary>
    public int SubscriberCountForUser(Guid userId) => Count(UserKey(userId));
    public int SubscriberCountForEvent(Guid eventId) => Count(EventKey(eventId));

    private static string UserKey(Guid id) => $"user:{id}";
    private static string EventKey(Guid id) => $"event:{id}";

    private RealtimeSubscription Subscribe(string key)
    {
        // A slow client loses its oldest messages instead of making the server hold them: it reloads the state over REST anyway.
        var channel = Channel.CreateBounded<RealtimeMessage>(new BoundedChannelOptions(MaxPendingPerConnection)
        {
            FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true,
        });
        var subscription = new Subscription(channel);
        lock (_gate)
        {
            if (!_subscriptions.TryGetValue(key, out var list)) _subscriptions[key] = list = [];
            list.Add(subscription);
        }
        return new RealtimeSubscription(channel.Reader, () => Unsubscribe(key, subscription));
    }

    private void Unsubscribe(string key, Subscription subscription)
    {
        lock (_gate)
        {
            if (!_subscriptions.TryGetValue(key, out var list)) return;
            list.Remove(subscription);
            if (list.Count == 0) _subscriptions.Remove(key);
        }
        subscription.Channel.Writer.TryComplete();
    }

    private void Publish(string key, RealtimeMessage message)
    {
        Subscription[] targets;
        lock (_gate) targets = _subscriptions.TryGetValue(key, out var list) ? [.. list] : [];
        foreach (var target in targets) target.Channel.Writer.TryWrite(message);
    }

    private int Count(string key)
    {
        lock (_gate) return _subscriptions.TryGetValue(key, out var list) ? list.Count : 0;
    }

    private sealed record Subscription(Channel<RealtimeMessage> Channel);
}

/// <summary>One open connection: read its messages and dispose it when the client goes away.</summary>
public sealed class RealtimeSubscription(ChannelReader<RealtimeMessage> reader, Action onDispose) : IDisposable
{
    public ChannelReader<RealtimeMessage> Reader { get; } = reader;
    public void Dispose() => onDispose();
}
