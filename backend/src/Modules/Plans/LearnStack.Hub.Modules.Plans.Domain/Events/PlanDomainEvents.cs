using LearnStack.Hub.SharedKernel.Domain;

namespace LearnStack.Hub.Modules.Plans.Domain.Events;

public sealed record PlanCreatedDomainEvent(PlanId PlanId, PlanTier Tier) : DomainEvent;

public sealed record PlanUpdatedDomainEvent(PlanId PlanId) : DomainEvent;

public sealed record PlanDeactivatedDomainEvent(PlanId PlanId) : DomainEvent;
