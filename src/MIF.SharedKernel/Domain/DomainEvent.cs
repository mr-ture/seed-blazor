namespace MIF.SharedKernel.Domain;

/// <summary>
/// Base type for domain events raised by the domain model.
/// </summary>
/// <param name="Id">
/// Unique event identifier used for tracking and deduplication.
/// </param>
/// <param name="OccurredOn">
/// UTC timestamp for when the event occurred.
/// </param>
/// <remarks>
/// Use domain events to publish important business changes without coupling side effects
/// (notifications, projections, integrations) to entity logic.
/// </remarks>
public abstract record DomainEvent(Guid Id, DateTime OccurredOn)
{
    /// <summary>
    /// Creates a new event with a generated ID and current UTC timestamp.
    /// </summary>
    protected DomainEvent() : this(Guid.NewGuid(), DateTime.UtcNow) { }
}
