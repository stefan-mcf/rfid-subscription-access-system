namespace Rfid.ReferenceModel;

public enum OutputMode
{
    DryRun,
    Live
}

public enum OutputIntentStatus
{
    Pending,
    Suppressed
}

public sealed record OutputIntent(
    string IntentId,
    string CorrelationId,
    int? WashProgram,
    OutputMode Mode,
    bool FieldCommissioned,
    bool ActuationPermitted,
    OutputIntentStatus Status,
    string Reason,
    DateTimeOffset RequestedAt,
    DateTimeOffset ExpiresAt);

public static class OutputIntentPlanner
{
    public static OutputIntent Plan(
        AccessDecision decision,
        OutputMode mode,
        bool fieldCommissioned,
        DateTimeOffset requestedAt)
    {
        if (decision.Decision != AccessOutcome.Allow ||
            decision.WashProgram is null ||
            decision.ValidUntil is null)
        {
            return Suppressed(decision, mode, fieldCommissioned, requestedAt, "decision_not_allowed");
        }

        if (decision.ValidUntil <= requestedAt)
        {
            return Suppressed(decision, mode, fieldCommissioned, requestedAt, "decision_expired");
        }

        if (mode == OutputMode.DryRun)
        {
            return Suppressed(decision, mode, fieldCommissioned, requestedAt, "dry_run_only");
        }

        if (!fieldCommissioned)
        {
            return Suppressed(decision, mode, fieldCommissioned, requestedAt, "field_not_commissioned");
        }

        return new OutputIntent(
            $"demo-intent-{decision.CorrelationId}",
            decision.CorrelationId,
            decision.WashProgram.Value,
            mode,
            fieldCommissioned,
            true,
            OutputIntentStatus.Pending,
            "eligible",
            requestedAt,
            decision.ValidUntil.Value);
    }

    private static OutputIntent Suppressed(
        AccessDecision decision,
        OutputMode mode,
        bool fieldCommissioned,
        DateTimeOffset requestedAt,
        string reason) =>
        new(
            $"demo-intent-{decision.CorrelationId}",
            decision.CorrelationId,
            decision.WashProgram,
            mode,
            fieldCommissioned,
            false,
            OutputIntentStatus.Suppressed,
            reason,
            requestedAt,
            decision.ValidUntil ?? requestedAt);
}
