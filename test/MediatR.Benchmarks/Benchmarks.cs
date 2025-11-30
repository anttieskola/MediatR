using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;

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
            var services = new ServiceCollection();

            services.AddSingleton(TextWriter.Null);

            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssemblyContaining(typeof(Ping));
                cfg.AddOpenBehavior(typeof(GenericPipelineBehavior<,>));
            });

            var provider = services.BuildServiceProvider();

            _mediator = provider.GetRequiredService<IMediator>();
        }

        [Benchmark]
        public Task SendingRequests() => _mediator.Send(_request);

        [Benchmark]
        public Task PublishingNotifications() => _mediator.Publish(_notification);
    }
}
