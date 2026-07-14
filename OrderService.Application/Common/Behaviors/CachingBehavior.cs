using MediatR;
using OrderService.Application.Common.Interfaces;

namespace OrderService.Application.Common.Behaviors;

public sealed class CachingBehavior<TRequest, TResponse>(ICacheService cacheService)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not ICachedQuery<TResponse> cachedQuery)
        {
            return await next();
        }

        var cached = await cacheService.GetAsync<TResponse>(cachedQuery.CacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var response = await next();
        await cacheService.SetAsync(cachedQuery.CacheKey, response, cachedQuery.Expiration, cancellationToken);

        return response;
    }
}
