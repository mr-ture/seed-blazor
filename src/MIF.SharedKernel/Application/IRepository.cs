using MIF.SharedKernel.Domain;

namespace MIF.SharedKernel.Application;

public interface IRepository<T> where T : IEntity
{
    Task<Result<T>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<int>> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task<Result> UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
