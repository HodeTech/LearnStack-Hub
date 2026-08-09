using LearnStack.Hub.SharedKernel;
using LearnStack.Hub.SharedKernel.Identifiers;
using Vogen;

namespace LearnStack.Hub.Modules.Plans.Domain;

/// <summary>Strongly-typed identifier for the <see cref="Plan"/> aggregate.</summary>
[ValueObject<Guid>(LearnStackHubVogenDefaults.IdMask)]
public readonly partial record struct PlanId : IStronglyTypedId<Guid>
{
}
