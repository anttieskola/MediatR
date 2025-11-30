using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR.Pipeline;

namespace MediatR.Benchmarks
{
    public class GenericRequestPostProcessor<TRequest, TResponse>(TextWriter writer)
        : IRequestPostProcessor<TRequest, TResponse>
        where TRequest : notnull
    {
        public Task Process(TRequest request, TResponse response, CancellationToken cancellationToken)
            => writer.WriteLineAsync("- All Done");
    }
}