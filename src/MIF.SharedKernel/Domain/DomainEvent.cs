namespace MIF.SharedKernel.Domain;

public abstract record DomainEvent(Guid Id, DateTime OccurredOn)
{
    protected DomainEvent() : this(Guid.NewGuid(), DateTime.UtcNow) { }
}
