namespace Rfid.ReferenceModel;

public enum OutboxState
{
    Pending,
    Acknowledged
}

public enum OutboxAcknowledgeResult
{
    Acknowledged,
    AlreadyAcknowledged,
    NotFound
}

public sealed record OutboxMessage(
    string MessageId,
    string AggregateRef,
    long Sequence,
    DateTimeOffset CreatedAt,
    OutboxState State,
    DateTimeOffset? AcknowledgedAt);

public sealed class LocalOutbox
{
    private readonly Dictionary<string, OutboxMessage> _messages = new(StringComparer.Ordinal);

    public IReadOnlyCollection<OutboxMessage> Messages => _messages.Values;

    public bool Enqueue(OutboxMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return _messages.TryAdd(message.MessageId, message);
    }

    public OutboxAcknowledgeResult Acknowledge(string messageId, DateTimeOffset acknowledgedAt)
    {
        if (!_messages.TryGetValue(messageId, out var message))
        {
            return OutboxAcknowledgeResult.NotFound;
        }

        if (message.State == OutboxState.Acknowledged)
        {
            return OutboxAcknowledgeResult.AlreadyAcknowledged;
        }

        _messages[messageId] = message with
        {
            State = OutboxState.Acknowledged,
            AcknowledgedAt = acknowledgedAt
        };

        return OutboxAcknowledgeResult.Acknowledged;
    }
}
