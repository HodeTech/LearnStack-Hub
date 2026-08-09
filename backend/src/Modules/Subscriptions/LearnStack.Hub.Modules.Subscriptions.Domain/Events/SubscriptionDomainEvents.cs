using LearnStack.Hub.SharedKernel.Domain;
using LearnStack.Hub.SharedKernel.Identifiers;

namespace LearnStack.Hub.Modules.Subscriptions.Domain.Events;

public sealed record SubscriptionStartedDomainEvent(LearnStackTenantId TenantId, Guid PlanId) : DomainEvent;

public sealed record SubscriptionActivatedDomainEvent(LearnStackTenantId TenantId) : DomainEvent;

public sealed record SubscriptionPlanChangedDomainEvent(LearnStackTenantId TenantId, Guid NewPlanId) : DomainEvent;

public sealed record SubscriptionCanceledDomainEvent(LearnStackTenantId TenantId, bool AtPeriodEnd) : DomainEvent;

public sealed record SubscriptionExpiredDomainEvent(LearnStackTenantId TenantId) : DomainEvent;
