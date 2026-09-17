using MediatR.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MediatR.Tests.MicrosoftExtensionsDI;

public class TypeResolutionTests
{
    private readonly IServiceProvider _provider;

    public TypeResolutionTests()
    {
        IServiceCollection services = new ServiceCollection();
        _ = services.AddSingleton(new Logger());
        _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Ping>());
        _provider = services.BuildServiceProvider();
    }

    [Fact]
    public void ShouldResolveMediator()
        => _provider.GetService<IMediator>().ShouldNotBeNull();

    [Fact]
    public void ShouldResolveSender()
        => _provider.GetService<ISender>().ShouldNotBeNull();

    [Fact]
    public void ShouldResolvePublisher()
        => _provider.GetService<IPublisher>().ShouldNotBeNull();

    [Fact]
    public void ShouldResolveRequestHandler()
        => _provider.GetService<IRequestHandler<Ping, Pong>>().ShouldNotBeNull();

    [Fact]
    public void ShouldResolveVoidRequestHandler()
        => _provider.GetService<IRequestHandler<Ding>>().ShouldNotBeNull();

    [Fact]
    public void ShouldResolveNotificationHandlers()
        => _provider.GetServices<INotificationHandler<Pinged>>().Count().ShouldBe(4);

    [Fact]
    public void ShouldNotThrowWithMissingEnumerables()
        => Should.NotThrow(() => _provider.GetRequiredService<IEnumerable<IRequestExceptionAction<int, Exception>>>());

    [Fact]
    public void ShouldResolveFirstDuplicateHandler()
    {
        _ = _provider.GetService<IRequestHandler<DuplicateTest, string>>().ShouldNotBeNull();
        _ = _provider.GetService<IRequestHandler<DuplicateTest, string>>()
            .ShouldBeAssignableTo<DuplicateHandler1>();
    }

    [Fact]
    public void ShouldResolveIgnoreSecondDuplicateHandler()
        => _provider.GetServices<IRequestHandler<DuplicateTest, string>>().Count().ShouldBe(1);

    [Fact]
    public void ShouldHandleKeyedServices()
    {
        IServiceCollection services = new ServiceCollection();
        _ = services.AddSingleton(new Logger());
        _ = services.AddKeyedSingleton<string>("Foo", "Foo");
        _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Ping>());
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();

        _ = mediator.ShouldNotBeNull();
    }
}