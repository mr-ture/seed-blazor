namespace MIF.SharedKernel.Application;

/// <summary>
/// Repository contract for basic CRUD operations on entities.
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
public interface IRepository<T>
{
    /// <summary>
    /// Gets an entity by ID.
    /// </summary>
    /// <param name="id">The unique identifier of the entity to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Entity on success; failure result if not found or on error.</returns>
    Task<Result<T>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new entity.
    /// </summary>
    /// <param name="entity">Entity to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New entity ID on success; failure result on error.</returns>
    Task<Result<int>> AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    /// <param name="entity">Entity with updated values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success or failure result.</returns>
    Task<Result> UpdateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an entity by ID.
    /// </summary>
    /// <param name="id">Entity ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success or failure result.</returns>
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
