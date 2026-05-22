using LearnStack.Hub.SharedKernel.Domain;
using LearnStack.Hub.SharedKernel.Identifiers;

namespace LearnStack.Hub.Modules.TenantLifecycle.Domain.Events;

public sealed record TenantCreatedDomainEvent(LearnStackTenantId TenantId) : DomainEvent;

public sealed record TenantActivatedDomainEvent(LearnStackTenantId TenantId) : DomainEvent;

public sealed record TenantSuspendedDomainEvent(LearnStackTenantId TenantId, string Reason) : DomainEvent;

public sealed record TenantArchivedDomainEvent(LearnStackTenantId TenantId) : DomainEvent;

public sealed record TenantTerminatedDomainEvent(LearnStackTenantId TenantId) : DomainEvent;
