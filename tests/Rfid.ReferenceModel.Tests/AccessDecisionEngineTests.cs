using Rfid.ReferenceModel;

namespace Rfid.ReferenceModel.Tests;

public sealed class AccessDecisionEngineTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-07-30T02:00:00Z");

    [Fact]
    public void Missing_snapshot_fails_closed()
    {
        var decision = AccessDecisionEngine.Evaluate(Read("A1B2C3D4"), null, Context());

        Assert.Equal(AccessOutcome.Deny, decision.Decision);
        Assert.Equal(AccessReasons.FieldPathUnavailable, decision.Reason);
        Assert.Null(decision.WashProgram);
    }

    [Fact]
    public void Unknown_tag_is_denied()
    {
        var decision = AccessDecisionEngine.Evaluate(Read("FFEEDDCC"), Snapshot(), Context());

        Assert.Equal(AccessOutcome.Deny, decision.Decision);
        Assert.Equal(AccessReasons.UnknownTag, decision.Reason);
    }

    [Fact]
    public void Inactive_subscription_is_denied()
    {
        var decision = AccessDecisionEngine.Evaluate(
            Read("11223344"),
            Snapshot(SubscriptionState.PastDue, "11223344"),
            Context());

        Assert.Equal(AccessReasons.InactiveSubscription, decision.Reason);
    }

    [Fact]
    public void Expired_entitlement_is_denied()
    {
        var decision = AccessDecisionEngine.Evaluate(
            Read("A1B2C3D4"),
            Snapshot(accessValidUntil: Now.AddSeconds(-1)),
            Context());

        Assert.Equal(AccessReasons.InactiveSubscription, decision.Reason);
    }

    [Fact]
    public void Busy_controller_is_denied_before_command_planning()
    {
        var decision = AccessDecisionEngine.Evaluate(Read("A1B2C3D4"), Snapshot(), Context(controllerBusy: true));

        Assert.Equal(AccessReasons.SystemBusy, decision.Reason);
    }

    [Fact]
    public void Repeat_read_inside_lockout_is_denied()
    {
        var lastReads = new Dictionary<string, DateTimeOffset> { ["A1B2C3D4"] = Now.AddSeconds(-30) };
        var decision = AccessDecisionEngine.Evaluate(
            Read("A1B2C3D4"),
            Snapshot(),
            Context(lastAllowedReads: lastReads));

        Assert.Equal(AccessReasons.RepeatLockout, decision.Reason);
    }

    [Fact]
    public void Active_tag_receives_short_lived_allow_decision()
    {
        var decision = AccessDecisionEngine.Evaluate(Read("A1B2C3D4"), Snapshot(), Context());

        Assert.Equal(AccessOutcome.Allow, decision.Decision);
        Assert.Equal(2, decision.WashProgram);
        Assert.Equal(Now.AddSeconds(15), decision.ValidUntil);
    }

    private static RfidRead Read(string tagId) =>
        new("demo-read-001", "demo-decision-001", tagId, Now, 4101);

    private static TagSnapshot Snapshot(
        SubscriptionState state = SubscriptionState.Active,
        string tagId = "A1B2C3D4",
        DateTimeOffset? accessValidUntil = null) =>
        new(
            "demo-snapshot-0042",
            42,
            Now.AddSeconds(-15),
            [new TagEntitlement(tagId, "vehicle-demo-hatch", state, 2, accessValidUntil ?? Now.AddDays(30), Now.AddSeconds(-30))]);

    private static DecisionContext Context(
        bool controllerBusy = false,
        IReadOnlyDictionary<string, DateTimeOffset>? lastAllowedReads = null) =>
        new(
            FieldPathAvailable: true,
            ControllerBusy: controllerBusy,
            RepeatLockout: TimeSpan.FromMinutes(2),
            LastAllowedReads: lastAllowedReads ?? new Dictionary<string, DateTimeOffset>());
}
