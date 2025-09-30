namespace Orion.Api.Services;

public interface IMessagePublisher
{
    void Publish<T>(T message);
}