namespace MIF.SharedKernel.Domain;

public interface IEntity
{
    int Id { get; }
}

public interface IEntity<TId> where TId : notnull
{
    TId Id { get; }
}
