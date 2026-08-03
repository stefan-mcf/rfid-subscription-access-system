using Rfid.ReferenceModel;

namespace Rfid.ReferenceModel.Tests;

public sealed class OutputIntentPlannerTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-07-30T02:00:00Z");

    [Fact]
    public void Dry_run_records_intent_without_actuation_permission()
    {
        var intent = OutputIntentPlanner.Plan(AllowedDecision(), OutputMode.DryRun, false, Now.AddSeconds(1));

        Assert.Equal(OutputIntentStatus.Suppressed, intent.Status);
        Assert.False(intent.ActuationPermitted);
        Assert.Equal("dry_run_only", intent.Reason);
    }

    [Fact]
    public void Live_mode_still_requires_field_commissioning()
    {
        var intent = OutputIntentPlanner.Plan(AllowedDecision(), OutputMode.Live, false, Now.AddSeconds(1));

        Assert.Equal(OutputIntentStatus.Suppressed, intent.Status);
        Assert.False(intent.ActuationPermitted);
        Assert.Equal("field_not_commissioned", intent.Reason);
    }

    [Fact]
    public void Denied_decision_never_prepares_an_output()
    {
        var denied = AllowedDecision() with
        {
            Decision = AccessOutcome.Deny,
            Reason = AccessReasons.UnknownTag,
            WashProgram = null,
            ValidUntil = null
        };

        var intent = OutputIntentPlanner.Plan(denied, OutputMode.Live, true, Now.AddSeconds(1));

        Assert.Equal(OutputIntentStatus.Suppressed, intent.Status);
        Assert.False(intent.ActuationPermitted);
        Assert.Equal("decision_not_allowed", intent.Reason);
        Assert.Null(intent.WashProgram);
    }

    [Fact]
    public void Commissioned_live_path_can_prepare_a_pending_intent()
    {
        var intent = OutputIntentPlanner.Plan(AllowedDecision(), OutputMode.Live, true, Now.AddSeconds(1));

        Assert.Equal(OutputIntentStatus.Pending, intent.Status);
        Assert.True(intent.ActuationPermitted);
        Assert.Equal("eligible", intent.Reason);
    }

    private static AccessDecision AllowedDecision() =>
        new(
            "demo-decision-001",
            "A1B2C3D4",
            AccessOutcome.Allow,
            AccessReasons.ActiveSubscription,
            2,
            Now.AddSeconds(15),
            Now);
}
