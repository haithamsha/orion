namespace Orion.Api.Services.EventSourcing;

/// <summary>
/// Exception thrown when there's a concurrency conflict in event sourcing
/// This happens when the expected version doesn't match the actual version
/// </summary>
public class ConcurrencyException : Exception
{
    public Guid AggregateId { get; }
    public int ExpectedVersion { get; }
    public int ActualVersion { get; }

    public ConcurrencyException(Guid aggregateId, int expectedVersion, int actualVersion)
        : base($"Concurrency conflict for aggregate {aggregateId}. Expected version {expectedVersion}, but actual version is {actualVersion}")
    {
        AggregateId = aggregateId;
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }
}