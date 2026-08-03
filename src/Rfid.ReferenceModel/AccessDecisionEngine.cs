namespace Rfid.ReferenceModel;

public enum AccessOutcome
{
    Allow,
    Deny
}

public static class AccessReasons
{
    public const string ActiveSubscription = "active_subscription";
    public const string UnknownTag = "unknown_tag";
    public const string InactiveSubscription = "inactive_subscription";
    public const string RepeatLockout = "repeat_lockout";
    public const string SystemBusy = "system_busy";
    public const string FieldPathUnavailable = "field_path_unavailable";
}

public sealed record RfidRead(
    string ReadId,
    string CorrelationId,
    string TagId,
    DateTimeOffset ObservedAt,
    long MonotonicSequence);

public sealed record TagEntitlement(
    string TagId,
    string VehicleRef,
    SubscriptionState SubscriptionState,
    int WashProgram,
    DateTimeOffset AccessValidUntil,
    DateTimeOffset UpdatedAt);

public sealed record TagSnapshot(
    string SnapshotId,
    long Version,
    DateTimeOffset CapturedAt,
    IReadOnlyCollection<TagEntitlement> Tags);

public sealed record DecisionContext(
    bool FieldPathAvailable,
    bool ControllerBusy,
    TimeSpan RepeatLockout,
    IReadOnlyDictionary<string, DateTimeOffset> LastAllowedReads);

public sealed record AccessDecision(
    string CorrelationId,
    string TagId,
    AccessOutcome Decision,
    string Reason,
    int? WashProgram,
    DateTimeOffset? ValidUntil,
    DateTimeOffset DecidedAt);

public static class AccessDecisionEngine
{
    public static AccessDecision Evaluate(
        RfidRead read,
        TagSnapshot? snapshot,
        DecisionContext context)
    {
        if (snapshot is null)
        {
            return Deny(read, AccessReasons.FieldPathUnavailable);
        }

        if (!context.FieldPathAvailable)
        {
            return Deny(read, AccessReasons.FieldPathUnavailable);
        }

        if (context.ControllerBusy)
        {
            return Deny(read, AccessReasons.SystemBusy);
        }

        var entitlement = snapshot.Tags.SingleOrDefault(tag =>
            string.Equals(tag.TagId, read.TagId, StringComparison.Ordinal));

        if (entitlement is null)
        {
            return Deny(read, AccessReasons.UnknownTag);
        }

        if (entitlement.SubscriptionState != SubscriptionState.Active ||
            entitlement.AccessValidUntil <= read.ObservedAt)
        {
            return Deny(read, AccessReasons.InactiveSubscription);
        }

        if (context.LastAllowedReads.TryGetValue(read.TagId, out var lastAllowedAt) &&
            read.ObservedAt - lastAllowedAt < context.RepeatLockout)
        {
            return Deny(read, AccessReasons.RepeatLockout);
        }

        return new AccessDecision(
            read.CorrelationId,
            read.TagId,
            AccessOutcome.Allow,
            AccessReasons.ActiveSubscription,
            entitlement.WashProgram,
            read.ObservedAt.AddSeconds(15),
            read.ObservedAt);
    }

    private static AccessDecision Deny(RfidRead read, string reason) =>
        new(
            read.CorrelationId,
            read.TagId,
            AccessOutcome.Deny,
            reason,
            null,
            null,
            read.ObservedAt);
}
