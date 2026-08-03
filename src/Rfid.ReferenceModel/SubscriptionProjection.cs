namespace Rfid.ReferenceModel;

public enum SubscriptionState
{
    PendingCheckout,
    Active,
    PastDue,
    Paused,
    Cancelled,
    Incomplete
}

public enum EventApplyResult
{
    Applied,
    Duplicate,
    OutOfOrder
}

public sealed record SubscriptionEvent(
    string EventId,
    long Sequence,
    SubscriptionState State,
    DateTimeOffset EffectiveAt);

public sealed class SubscriptionProjection
{
    private readonly HashSet<string> _processedEventIds = new(StringComparer.Ordinal);

    public long LastSequence { get; private set; }

    public SubscriptionState? CurrentState { get; private set; }

    public EventApplyResult Apply(SubscriptionEvent subscriptionEvent)
    {
        ArgumentNullException.ThrowIfNull(subscriptionEvent);

        if (_processedEventIds.Contains(subscriptionEvent.EventId))
        {
            return EventApplyResult.Duplicate;
        }

        if (subscriptionEvent.Sequence <= LastSequence)
        {
            return EventApplyResult.OutOfOrder;
        }

        _processedEventIds.Add(subscriptionEvent.EventId);
        LastSequence = subscriptionEvent.Sequence;
        CurrentState = subscriptionEvent.State;
        return EventApplyResult.Applied;
    }
}
