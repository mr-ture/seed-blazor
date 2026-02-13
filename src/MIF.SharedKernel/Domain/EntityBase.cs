namespace MIF.SharedKernel.Domain;

/// <summary>
/// Base class for entities that use an integer ID.
/// Purpose: provide a shared identity property and common entity contract for int-keyed domain models.
/// </summary>
/// <remarks>
/// Use <see cref="EntityBase{TId}"/> when the key type is not <see cref="int"/>.
/// </remarks>
public abstract class EntityBase : IEntity
{
    /// <summary>
    /// Gets or sets the entity ID.
    /// </summary>
    /// <value>Database primary key.</value>
    public int Id { get; set; }
}

/// <summary>
/// Base class for entities that use a custom ID type.
/// Purpose: support domains that use non-int keys (for example <see cref="Guid"/> or <see cref="string"/>).
/// </summary>
/// <typeparam name="TId">
/// Entity key type. Must be non-null.
/// </typeparam>
public abstract class EntityBase<TId> : IEntity<TId> where TId : notnull
{
    /// <summary>
    /// Gets or sets the entity ID.
    /// </summary>
    /// <value>Primary key value.</value>
    public TId Id { get; set; } = default!;
}
