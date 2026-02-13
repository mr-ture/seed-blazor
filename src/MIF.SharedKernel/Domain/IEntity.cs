namespace MIF.SharedKernel.Domain;

/// <summary>
/// Marker interface for entities with an integer primary key.
/// </summary>
public interface IEntity
{
    /// <summary>
    /// Primary key for the entity.
    /// </summary>
    int Id { get; }
}

/// <summary>
/// Marker interface for entities with a custom primary key type.
/// </summary>
public interface IEntity<TId> where TId : notnull
{
    /// <summary>
    /// Primary key for the entity.
    /// </summary>
    TId Id { get; }
}
