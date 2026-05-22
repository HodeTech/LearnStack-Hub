using FluentAssertions;
using LearnStack.Hub.SharedKernel.Domain;
using LearnStack.Hub.SharedKernel.Identifiers;
using Xunit;

namespace LearnStack.Hub.Tests.Unit.SharedKernel;

public sealed class EntityTests
{
    // Reuses OperatorId (a real SharedKernel Vogen id) as the TId so the test
    // project doesn't need its own Vogen declaration.
    private sealed class SampleEntity : Entity<OperatorId>
    {
        public SampleEntity(OperatorId id)
            : base(id)
        {
        }

        public void Raise(IDomainEvent e) => RaiseDomainEvent(e);
    }

    private sealed class OtherEntity : Entity<OperatorId>
    {
        public OtherEntity(OperatorId id)
            : base(id)
        {
        }
    }

    private sealed record SampleEvent : DomainEvent;

    [Fact]
    public void SameId_AreEqual()
    {
        var id = OperatorId.From(Guid.CreateVersion7());

        var a = new SampleEntity(id);
        var b = new SampleEntity(id);

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void TransientEntities_AreNeverEqual()
    {
        var a = new SampleEntity(default);
        var b = new SampleEntity(default);

        a.Should().NotBe(b);
    }

    [Fact]
    public void DifferentRuntimeType_SameId_AreNotEqual()
    {
        var id = OperatorId.From(Guid.CreateVersion7());

        var a = new SampleEntity(id);
        var b = new OtherEntity(id);

        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void DomainEvents_AreCollectedAndCleared()
    {
        var entity = new SampleEntity(OperatorId.From(Guid.CreateVersion7()));
        var evt = new SampleEvent { EventId = Guid.CreateVersion7(), OccurredAt = DateTimeOffset.UtcNow };

        entity.Raise(evt);
        entity.DomainEvents.Should().ContainSingle().Which.Should().Be(evt);

        entity.ClearDomainEvents();
        entity.DomainEvents.Should().BeEmpty();
    }
}
