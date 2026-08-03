namespace Rfid.ReferenceModel;

public enum SnapshotApplyResult
{
    Applied,
    Stale
}

public sealed class TagSnapshotStore
{
    private IReadOnlyDictionary<string, TagEntitlement> _tags =
        new Dictionary<string, TagEntitlement>(StringComparer.Ordinal);

    public long Version { get; private set; }

    public IReadOnlyDictionary<string, TagEntitlement> Tags => _tags;

    public SnapshotApplyResult ApplyFull(TagSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (snapshot.Version <= Version)
        {
            return SnapshotApplyResult.Stale;
        }

        var replacement = new Dictionary<string, TagEntitlement>(StringComparer.Ordinal);
        foreach (var tag in snapshot.Tags)
        {
            if (!replacement.TryAdd(tag.TagId, tag))
            {
                throw new InvalidOperationException($"Duplicate tag in full snapshot: {tag.TagId}");
            }
        }

        _tags = replacement;
        Version = snapshot.Version;
        return SnapshotApplyResult.Applied;
    }
}
