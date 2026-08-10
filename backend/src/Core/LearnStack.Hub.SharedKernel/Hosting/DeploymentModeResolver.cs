namespace LearnStack.Hub.SharedKernel.Hosting;

/// <summary>
/// Resolves the configured <see cref="DeploymentMode"/> at the composition root.
/// It is a pure function of the configured string and the host environment so it
/// can be unit-tested; the composition root remains the only place that reads
/// <c>Hub:DeploymentMode</c> from configuration.
/// <para>
/// It <strong>fails closed</strong>. Only the Development environment may leave
/// the value unset, and only an exact (case-insensitive) member name is accepted.
/// <c>Enum.TryParse</c> is deliberately not used: it also accepts numeric text
/// (<c>"3"</c>) and comma-separated lists (<c>"SaaS,Dedicated"</c>, which ORs to
/// <c>3</c>), both of which land on a defined member and would slip past an
/// <c>Enum.IsDefined</c> guard while meaning nothing an operator intended to write.
/// </para>
/// </summary>
public static class DeploymentModeResolver
{
    /// <summary>
    /// Resolves the mode, or throws <see cref="InvalidOperationException"/> with a
    /// message naming the key and its valid values.
    /// </summary>
    /// <param name="configuredValue">The raw <c>Hub:DeploymentMode</c> value; surrounding whitespace is ignored.</param>
    /// <param name="isDevelopmentEnvironment">Whether the host environment is Development.</param>
    public static DeploymentMode Resolve(string? configuredValue, bool isDevelopmentEnvironment)
    {
        var trimmed = configuredValue?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            if (!isDevelopmentEnvironment)
            {
                throw new InvalidOperationException(
                    $"Hub:DeploymentMode is not configured. It is required outside the Development environment. {ValidValuesSuffix()}");
            }

            return DeploymentMode.Development;
        }

        foreach (var candidate in Enum.GetValues<DeploymentMode>())
        {
            if (string.Equals(candidate.ToString(), trimmed, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException(
            $"Hub:DeploymentMode '{trimmed}' is not a valid DeploymentMode. {ValidValuesSuffix()}");
    }

    private static string ValidValuesSuffix() =>
        $"Expected exactly one of: {string.Join(", ", Enum.GetNames<DeploymentMode>())}.";
}
