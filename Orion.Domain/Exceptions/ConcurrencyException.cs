namespace Orion.Domain.Exceptions;

public class ConcurrencyException : Exception
{
    public ConcurrencyException(Guid aggregateId, int expectedVersion, int actualVersion)
        : base($"Concurrency conflict for aggregate {aggregateId}. Expected version {expectedVersion}, but was {actualVersion}.")
    {
    }
}
