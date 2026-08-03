using Rfid.ReferenceModel;

namespace Rfid.ReferenceModel.Tests;

public sealed class StateControlTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-07-30T02:00:00Z");

    [Fact]
    public void Subscription_events_are_ordered_and_idempotent()
    {
        var projection = new SubscriptionProjection();
        var active = new SubscriptionEvent("demo-sub-event-002", 2, SubscriptionState.Active, Now);

        Assert.Equal(EventApplyResult.Applied, projection.Apply(active));
        Assert.Equal(EventApplyResult.Duplicate, projection.Apply(active));
        Assert.Equal(
            EventApplyResult.OutOfOrder,
            projection.Apply(new SubscriptionEvent("demo-sub-event-001", 1, SubscriptionState.PendingCheckout, Now.AddMinutes(-1))));
        Assert.Equal(SubscriptionState.Active, projection.CurrentState);
        Assert.Equal(2, projection.LastSequence);
    }

    [Fact]
    public void Full_snapshot_replaces_tags_missing_from_the_next_version()
    {
        var store = new TagSnapshotStore();
        store.ApplyFull(Snapshot(41, "A1B2C3D4", "11223344"));

        Assert.Equal(SnapshotApplyResult.Applied, store.ApplyFull(Snapshot(42, "A1B2C3D4")));
        Assert.Single(store.Tags);
        Assert.False(store.Tags.ContainsKey("11223344"));
        Assert.Equal(42, store.Version);
    }

    [Fact]
    public void Stale_snapshot_does_not_replace_current_state()
    {
        var store = new TagSnapshotStore();
        store.ApplyFull(Snapshot(42, "A1B2C3D4"));

        Assert.Equal(SnapshotApplyResult.Stale, store.ApplyFull(Snapshot(41, "11223344")));
        Assert.True(store.Tags.ContainsKey("A1B2C3D4"));
    }

    [Fact]
    public void Invalid_full_snapshot_is_rejected_before_state_changes()
    {
        var store = new TagSnapshotStore();
        store.ApplyFull(Snapshot(41, "A1B2C3D4"));
        var duplicateTags = Snapshot(42, "11223344", "11223344");

        Assert.Throws<InvalidOperationException>(() => store.ApplyFull(duplicateTags));
        Assert.Equal(41, store.Version);
        Assert.True(store.Tags.ContainsKey("A1B2C3D4"));
    }

    [Fact]
    public void Outbox_acknowledgement_is_durable_and_repeat_safe()
    {
        var outbox = new LocalOutbox();
        var message = new OutboxMessage(
            "demo-outbox-001",
            "demo-decision-001",
            101,
            Now,
            OutboxState.Pending,
            null);

        Assert.True(outbox.Enqueue(message));
        Assert.False(outbox.Enqueue(message));
        Assert.Equal(OutboxAcknowledgeResult.Acknowledged, outbox.Acknowledge(message.MessageId, Now.AddSeconds(2)));
        Assert.Equal(OutboxAcknowledgeResult.AlreadyAcknowledged, outbox.Acknowledge(message.MessageId, Now.AddSeconds(3)));
        Assert.Equal(OutboxAcknowledgeResult.NotFound, outbox.Acknowledge("demo-outbox-missing", Now));
        Assert.Equal(OutboxState.Acknowledged, Assert.Single(outbox.Messages).State);
    }

    private static TagSnapshot Snapshot(long version, params string[] tagIds) =>
        new(
            $"demo-snapshot-{version}",
            version,
            Now,
            tagIds.Select(tagId =>
                new TagEntitlement(tagId, "vehicle-demo-hatch", SubscriptionState.Active, 2, Now.AddDays(30), Now)).ToArray());
}
