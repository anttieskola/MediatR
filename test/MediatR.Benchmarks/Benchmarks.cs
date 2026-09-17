using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Threading.Tasks;

namespace MediatR.Benchmarks
{
    [DotTraceDiagnoser]
    public class Benchmarks
    {
        private IMediator _mediator;
        private readonly Ping _request = new() { Message = "Hello World" };
        private readonly Pinged _notification = new();

        [GlobalSetup]
        public void GlobalSetup()
        {
            ServiceCollection services = new();

            _ = services.AddSingleton(TextWriter.Null);

            _ = services.AddMediatR(cfg =>
            {
                _ = cfg.RegisterServicesFromAssemblyContaining<Ping>();
                _ = cfg.AddOpenBehavior(typeof(GenericPipelineBehavior<,>));
            });

            ServiceProvider provider = services.BuildServiceProvider();

            _mediator = provider.GetRequiredService<IMediator>();
        }

        [Benchmark]
        public Task SendingRequests() => _mediator.Send(_request);

        [Benchmark]
        public Task PublishingNotifications() => _mediator.Publish(_notification);
    }
}
