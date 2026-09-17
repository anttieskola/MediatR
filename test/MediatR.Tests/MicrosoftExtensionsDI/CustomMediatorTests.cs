using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Linq;
using Xunit;

namespace MediatR.Tests.MicrosoftExtensionsDI;

public class CustomMediatorTests
{
    private readonly IServiceProvider _provider;

    public CustomMediatorTests()
    {
        IServiceCollection services = new ServiceCollection();
        _ = services.AddSingleton(new Logger());
        _ = services.AddMediatR(cfg =>
        {
            cfg.MediatorImplementationType = typeof(MyCustomMediator);
            _ = cfg.RegisterServicesFromAssemblyContaining(typeof(CustomMediatorTests));
        });
        _provider = services.BuildServiceProvider();
    }

    [Fact]
    public void ShouldResolveMediator()
    {
        _ = _provider.GetService<IMediator>().ShouldNotBeNull();
        _provider.GetRequiredService<IMediator>().GetType().ShouldBe(typeof(MyCustomMediator));
    }

    [Fact]
    public void ShouldResolveRequestHandler()
        => _provider.GetService<IRequestHandler<Ping, Pong>>().ShouldNotBeNull();

    [Fact]
    public void ShouldResolveNotificationHandlers()
        => _provider.GetServices<INotificationHandler<Pinged>>().Count().ShouldBe(4);

    [Fact]
    public void Can_Call_AddMediatr_multiple_times()
    {
        IServiceCollection services = new ServiceCollection();
        _ = services.AddSingleton(new Logger());
        _ = services.AddMediatR(cfg =>
        {
            cfg.MediatorImplementationType = typeof(MyCustomMediator);
            _ = cfg.RegisterServicesFromAssemblyContaining(typeof(CustomMediatorTests));
        });

        // Call AddMediatr again, this should NOT override our custom mediatr (With MS DI, last registration wins)
        _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining(typeof(CustomMediatorTests)));

        ServiceProvider provider = services.BuildServiceProvider();
        IMediator mediator = provider.GetRequiredService<IMediator>();
        mediator.GetType().ShouldBe(typeof(MyCustomMediator));
    }
}