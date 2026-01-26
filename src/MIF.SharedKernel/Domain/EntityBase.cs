namespace MIF.SharedKernel.Domain;

public abstract class EntityBase : IEntity
{
    public int Id { get; set; }
}

public abstract class EntityBase<TId> : IEntity<TId> where TId : notnull
{
    public TId Id { get; set; } = default!;
}
