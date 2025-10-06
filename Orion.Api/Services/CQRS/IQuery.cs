using MediatR;

namespace Orion.Api.Services.CQRS;

/// <summary>
/// Interface for queries that return a value
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the query</typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse>
{
}