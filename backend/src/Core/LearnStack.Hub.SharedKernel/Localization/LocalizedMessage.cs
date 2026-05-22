using System.Collections.ObjectModel;

namespace LearnStack.Hub.SharedKernel.Localization;

/// <summary>
/// Localization-key carrier for every user-facing message Hub returns to the
/// operator portal. The backend never returns raw English text; it returns a
/// <see cref="LocalizedMessage"/> whose <see cref="Key"/> resolves to a
/// translation on the client. The <c>lockey_</c> prefix invariant is enforced
/// at the constructor — mis-prefixed keys fail loud at construction.
/// </summary>
public sealed record LocalizedMessage
{
    /// <summary>The required prefix for every localization key.</summary>
    public const string RequiredPrefix = "lockey_";

    public LocalizedMessage(string key, IReadOnlyDictionary<string, string>? @params = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (!key.StartsWith(RequiredPrefix, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Localization key must start with '{RequiredPrefix}'. Got: '{key}'.",
                nameof(key));
        }

        Key = key;
        Params = @params is { Count: > 0 }
            ? new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(@params))
            : null;
    }

    /// <summary>
    /// The localization key (always begins with <see cref="RequiredPrefix"/>).
    /// <c>Error.Code</c> projects from this key with the prefix stripped.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Optional ICU MessageFormat parameter set the frontend interpolates as
    /// plain text. <c>null</c> when the message takes no parameters.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Params { get; }

    /// <summary>Convenience factory equivalent to <c>new LocalizedMessage(key, params)</c>.</summary>
    public static LocalizedMessage Of(
        string key,
        IReadOnlyDictionary<string, string>? @params = null) =>
        new(key, @params);

    public bool Equals(LocalizedMessage? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (!string.Equals(Key, other.Key, StringComparison.Ordinal))
        {
            return false;
        }

        if (Params is null && other.Params is null)
        {
            return true;
        }

        if (Params is null || other.Params is null || Params.Count != other.Params.Count)
        {
            return false;
        }

        foreach (var (k, v) in Params)
        {
            if (!other.Params.TryGetValue(k, out var otherValue) ||
                !string.Equals(v, otherValue, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Key, StringComparer.Ordinal);
        if (Params is not null)
        {
            var paramsHash = 0;
            foreach (var (k, v) in Params)
            {
                paramsHash ^= HashCode.Combine(k, v);
            }

            hash.Add(paramsHash);
        }

        return hash.ToHashCode();
    }
}
