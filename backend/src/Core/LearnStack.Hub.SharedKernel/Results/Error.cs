using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using LearnStack.Hub.SharedKernel.Localization;

namespace LearnStack.Hub.SharedKernel.Results;

/// <summary>
/// Result-pattern error payload used by <see cref="Result{T}"/>.
/// <see cref="Message"/> is the localised payload the frontend resolves;
/// <see cref="Code"/> is the stable machine-readable identifier API consumers
/// route on. <see cref="Code"/> is derived from <c>Message.Key</c> by
/// stripping the invariant <see cref="LocalizedMessage.RequiredPrefix"/>, so
/// the two contracts stay in sync by construction.
/// </summary>
[SuppressMessage(
    "Naming",
    "CA1716:Identifiers should not match keywords",
    Justification = "Result+Error pattern — C#-only codebase; no VB consumer affected.")]
public sealed record Error
{
    public Error(
        LocalizedMessage message,
        IReadOnlyDictionary<string, IReadOnlyList<LocalizedMessage>>? details = null)
    {
        ArgumentNullException.ThrowIfNull(message);
        Message = message;
        Details = SnapshotDetails(details);
    }

    public LocalizedMessage Message { get; }

    public IReadOnlyDictionary<string, IReadOnlyList<LocalizedMessage>>? Details { get; }

    /// <summary>
    /// Stable machine-readable code derived from <see cref="Message"/>'s
    /// <c>Key</c> with the <see cref="LocalizedMessage.RequiredPrefix"/>
    /// stripped. Used as the RFC 7807 <c>code</c> field — never localized.
    /// </summary>
    public string Code => Message.Key[LocalizedMessage.RequiredPrefix.Length..];

    public bool Equals(Error? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Message.Equals(other.Message) && DetailsEqual(Details, other.Details);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Message);

        if (Details is not null)
        {
            var detailsHash = 0;
            foreach (var (key, list) in Details)
            {
                var listHash = 0;
                foreach (var msg in list)
                {
                    listHash ^= msg.GetHashCode();
                }

                detailsHash ^= HashCode.Combine(key, listHash);
            }

            hash.Add(detailsHash);
        }

        return hash.ToHashCode();
    }

    private static ReadOnlyDictionary<string, IReadOnlyList<LocalizedMessage>>? SnapshotDetails(
        IReadOnlyDictionary<string, IReadOnlyList<LocalizedMessage>>? source)
    {
        if (source is not { Count: > 0 })
        {
            return null;
        }

        var snapshot = new Dictionary<string, IReadOnlyList<LocalizedMessage>>(source.Count);
        foreach (var (key, list) in source)
        {
            ArgumentNullException.ThrowIfNull(list);

            var copy = new LocalizedMessage[list.Count];
            for (var i = 0; i < list.Count; i++)
            {
                copy[i] = list[i] ?? throw new ArgumentException(
                    $"Error.Details['{key}'][{i}] is null. Every field-level entry must be a non-null LocalizedMessage.",
                    nameof(source));
            }

            snapshot[key] = new ReadOnlyCollection<LocalizedMessage>(copy);
        }

        return new ReadOnlyDictionary<string, IReadOnlyList<LocalizedMessage>>(snapshot);
    }

    private static bool DetailsEqual(
        IReadOnlyDictionary<string, IReadOnlyList<LocalizedMessage>>? a,
        IReadOnlyDictionary<string, IReadOnlyList<LocalizedMessage>>? b)
    {
        if (a is null && b is null)
        {
            return true;
        }

        if (a is null || b is null || a.Count != b.Count)
        {
            return false;
        }

        foreach (var (key, listA) in a)
        {
            if (!b.TryGetValue(key, out var listB) || listA.Count != listB.Count)
            {
                return false;
            }

            for (var i = 0; i < listA.Count; i++)
            {
                if (!listA[i].Equals(listB[i]))
                {
                    return false;
                }
            }
        }

        return true;
    }
}
