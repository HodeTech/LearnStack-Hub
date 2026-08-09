using LearnStack.Hub.SharedKernel;
using LearnStack.Hub.SharedKernel.Identifiers;
using Vogen;

namespace LearnStack.Hub.Modules.Subscriptions.Domain;

/// <summary>Strongly-typed identifier for the <see cref="HubSubscription"/> aggregate.</summary>
[ValueObject<Guid>(LearnStackHubVogenDefaults.IdMask)]
public readonly partial record struct HubSubscriptionId : IStronglyTypedId<Guid>
{
}
