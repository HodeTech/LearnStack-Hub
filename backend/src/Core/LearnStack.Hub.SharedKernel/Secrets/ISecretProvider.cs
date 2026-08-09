namespace LearnStack.Hub.SharedKernel.Secrets;

/// <summary>
/// Composition-root-resolved secret provider. Every secret-bearing value
/// (Sentry DSN, provider API keys, HMAC shared secrets) is read through this
/// contract — modules never call <c>Environment.GetEnvironmentVariable</c> or
/// hand-roll their own Vault clients.
/// </summary>
public interface ISecretProvider
{
    /// <summary>
    /// Resolves the secret identified by <paramref name="key"/>. Returns
    /// <c>null</c> when the secret is not configured — callers decide whether
    /// to fail fast or fall back.
    /// </summary>
    string? GetSecret(string key);
}
