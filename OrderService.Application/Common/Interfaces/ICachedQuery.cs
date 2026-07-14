using MediatR;

namespace OrderService.Application.Common.Interfaces;

public interface ICachedQuery<out TResponse> : IRequest<TResponse>
{
    string CacheKey { get; }
    TimeSpan Expiration { get; }
}
