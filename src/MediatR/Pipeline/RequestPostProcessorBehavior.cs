using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.Pipeline;
/// <summary>
/// Behavior for executing all <see cref="IRequestPostProcessor{TRequest,TResponse}"/> instances after handling the request
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public class RequestPostProcessorBehavior<TRequest, TResponse>(IEnumerable<IRequestPostProcessor<TRequest, TResponse>> postProcessors) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        TResponse? response = await next(cancellationToken).ConfigureAwait(false);

        foreach (IRequestPostProcessor<TRequest, TResponse> processor in postProcessors)
        {
            await processor.Process(request, response, cancellationToken).ConfigureAwait(false);
        }

        return response;
    }
}