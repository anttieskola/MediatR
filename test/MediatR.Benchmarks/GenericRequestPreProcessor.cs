using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR.Pipeline;

namespace MediatR.Benchmarks
{
    public class GenericRequestPreProcessor<TRequest>(TextWriter writer) : IRequestPreProcessor<TRequest>
        where TRequest : notnull
    {
        public Task Process(TRequest request, CancellationToken cancellationToken)
            => writer.WriteLineAsync("- Starting Up");
    }
}