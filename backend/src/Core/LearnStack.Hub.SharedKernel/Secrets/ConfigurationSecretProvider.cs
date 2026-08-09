using Microsoft.Extensions.Configuration;

namespace LearnStack.Hub.SharedKernel.Secrets;

/// <summary>
/// Default <see cref="ISecretProvider"/> that delegates to
/// <see cref="IConfiguration"/> (which already merges environment variables,
/// user secrets, and <c>appsettings.{env}.json</c>). A Vault-backed
/// implementation lands in a later packet; the composition root branches by
/// <c>DeploymentMode</c>.
/// </summary>
public sealed class ConfigurationSecretProvider(IConfiguration configuration) : ISecretProvider
{
    private readonly IConfiguration _configuration = configuration
        ?? throw new ArgumentNullException(nameof(configuration));

    public string? GetSecret(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var value = _configuration[key];
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
