using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;

namespace MediatR.Tests;

public class ServiceFactoryTests
{
    public class Ping : IRequest<Pong>;

    public class Pong
    {
        public string? Message { get; set; }
    }

    [Fact]
    public async Task Should_throw_given_no_handler()
    {
        ServiceCollection serviceCollection = new();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        Mediator mediator = new(serviceProvider);

        _ = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.Send(new Ping(), TestContext.Current.CancellationToken)
        );
    }
}