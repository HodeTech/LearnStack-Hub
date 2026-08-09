namespace LearnStack.Hub.SharedKernel.Secrets;

/// <summary>
/// The canonical list of word-tokens whose presence as a <em>segment</em> of a
/// property name marks the value as sensitive. The air-gapped
/// <c>LocalFileErrorTracker</c> (and any future Serilog redaction enricher)
/// consume this single source of truth so redaction surfaces cannot drift.
/// Matching is on word boundaries, not raw substrings, to avoid over-redaction.
/// </summary>
public static class SensitiveTokenCatalog
{
    /// <summary>Substituted in place of any matched property value.</summary>
    public const string RedactedValue = "***REDACTED***";

    private static readonly HashSet<string> SingleWordTokens =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "apikey",
            "authorization",
            "cardnumber",
            "credential",
            "cvc",
            "cvv",
            "dsn",
            "hmac",
            "iban",
            "jwt",
            "passwd",
            "password",
            "secret",
            "ssn",
            "tckn",
            "token",
            "vkn",
        };

    private static readonly HashSet<string> TwoWordTokens =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "apikey",
            "cardnumber",
            "authheader",
        };

    /// <summary>The canonical token list (for docs / tests).</summary>
    public static IReadOnlyCollection<string> DefaultTokens => SingleWordTokens;

    /// <summary>
    /// Returns <c>true</c> when any whole segment of <paramref name="propertyName"/>
    /// (split on camelCase boundaries and <c>_ . -</c> separators) matches a
    /// sensitive token.
    /// </summary>
    public static bool IsSensitive(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            return false;
        }

        var segments = Tokenize(propertyName);

        for (var i = 0; i < segments.Count; i++)
        {
            if (SingleWordTokens.Contains(segments[i]))
            {
                return true;
            }

            if (i + 1 < segments.Count
                && TwoWordTokens.Contains(segments[i] + segments[i + 1]))
            {
                return true;
            }
        }

        return false;
    }

    private static List<string> Tokenize(string name)
    {
        var segments = new List<string>();
        var start = 0;

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            var isSeparator = !char.IsLetterOrDigit(c);

            var isCamelBoundary = i > start && char.IsUpper(c) &&
                ((char.IsLower(name[i - 1]) || char.IsDigit(name[i - 1]))
                 || (char.IsUpper(name[i - 1])
                     && i + 1 < name.Length
                     && char.IsLower(name[i + 1])));

            if (isSeparator)
            {
                if (i > start)
                {
                    segments.Add(name[start..i].ToLowerInvariant());
                }

                start = i + 1;
            }
            else if (isCamelBoundary)
            {
                segments.Add(name[start..i].ToLowerInvariant());
                start = i;
            }
        }

        if (start < name.Length)
        {
            segments.Add(name[start..].ToLowerInvariant());
        }

        return segments;
    }
}
