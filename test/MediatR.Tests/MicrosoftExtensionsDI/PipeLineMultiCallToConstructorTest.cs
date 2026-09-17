using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MediatR.Tests.MicrosoftExtensionsDI;

public class PipelineMultiCallToConstructorTests
{
    public class ConstructorTestBehavior<TRequest, TResponse>(Logger output) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            output.Messages.Add("ConstructorTestBehavior before");
            TResponse? response = await next(cancellationToken);
            output.Messages.Add("ConstructorTestBehavior after");

            return response;
        }
    }

    public class ConstructorTestRequest : IRequest<ConstructorTestResponse>
    {
        public string? Message { get; set; }
    }

    public class ConstructorTestResponse
    {
        public string? Message { get; set; }
    }

    public class ConstructorTestHandler : IRequestHandler<ConstructorTestRequest, ConstructorTestResponse>
    {

        private static readonly object _lockObject = new();
        private readonly Logger _logger;

        public static int ConstructorCallCount { get; private set; }

        public static void ResetCallCount()
        {
            lock (_lockObject)
            {
                ConstructorCallCount = 0;
            }
        }

        public ConstructorTestHandler(Logger logger)
        {
            _logger = logger;
            lock (_lockObject)
            {
                ConstructorCallCount++;
            }
        }

        public Task<ConstructorTestResponse> Handle(ConstructorTestRequest request, CancellationToken cancellationToken)
        {
            _logger.Messages.Add("Handler");
            return Task.FromResult(new ConstructorTestResponse { Message = request.Message + " ConstructorPong" });
        }
    }

    [Fact]
    public async Task Should_not_call_constructor_multiple_times_when_using_a_pipeline()
    {
        ConstructorTestHandler.ResetCallCount();
        ConstructorTestHandler.ConstructorCallCount.ShouldBe(0);

        Logger output = new();
        IServiceCollection services = new ServiceCollection();

        _ = services.AddSingleton(output);
        _ = services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ConstructorTestBehavior<,>));
        _ = services.AddMediatR(cfg =>
        {
            _ = cfg.RegisterServicesFromAssembly(typeof(Ping).Assembly);
            _ = cfg.AddOpenBehavior(typeof(ConstructorTestBehavior<,>));
        });
        ServiceProvider provider = services.BuildServiceProvider();

        IMediator mediator = provider.GetRequiredService<IMediator>();

        ConstructorTestResponse response = await mediator.Send(new ConstructorTestRequest { Message = "ConstructorPing" }, TestContext.Current.CancellationToken);

        response.Message.ShouldBe("ConstructorPing ConstructorPong");

        output.Messages.ShouldBe(
        [
            "ConstructorTestBehavior before",
            "Handler",
            "ConstructorTestBehavior after"
        ]);
        ConstructorTestHandler.ConstructorCallCount.ShouldBe(1);
    }
}