using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Threading.Tasks;
using Xunit;

namespace MediatR.Tests.MicrosoftExtensionsDI;

public class DerivingRequestsTests
{
    private readonly IServiceProvider _provider;
    private readonly IMediator _mediator;

    public DerivingRequestsTests()
    {
        IServiceCollection services = new ServiceCollection();
        _ = services.AddSingleton(new Logger());
        _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining(typeof(Ping)));
        _provider = services.BuildServiceProvider();
        _mediator = _provider.GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task ShouldReturnPingPong()
    {
        Pong pong = await _mediator.Send(new Ping() { Message = "Ping" }, TestContext.Current.CancellationToken);
        pong.Message.ShouldBe("Ping Pong");
    }

    [Fact]
    public async Task ShouldReturnDerivedPingPong()
    {
        Pong pong = await _mediator.Send(new DerivedPing() { Message = "Ping" }, TestContext.Current.CancellationToken);
        pong.Message.ShouldBe("DerivedPing Pong");
    }
}