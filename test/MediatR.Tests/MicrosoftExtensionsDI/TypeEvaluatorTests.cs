using Microsoft.Extensions.DependencyInjection;
using MediatR.Tests.MicrosoftExtensionsDI.Included;
using MediatR.Pipeline;
using System.Linq;
using Shouldly;
using System;
using Xunit;

namespace MediatR.Tests.MicrosoftExtensionsDI;

public class TypeEvaluatorTests
{
    private readonly IServiceProvider _provider;
    private readonly IServiceCollection _services;


    public TypeEvaluatorTests()
    {
        _services = new ServiceCollection();
        _services.AddSingleton(new Logger());
        _services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining(typeof(Ping));
            cfg.TypeEvaluator = t => t.Namespace == "MediatR.Tests.MicrosoftExtensionsDI.Included";
        });
        _provider = _services.BuildServiceProvider();
    }

    [Fact]
    public void ShouldResolveMediator()
    {
        _provider.GetService<IMediator>().ShouldNotBeNull();
    }

    [Fact]
    public void ShouldOnlyResolveIncludedRequestHandlers()
    {
        _provider.GetService<IRequestHandler<Foo, Bar>>().ShouldNotBeNull();
        _provider.GetService<IRequestHandler<Ping, Pong>>().ShouldBeNull();
    }

    [Fact]
    public void ShouldNotRegisterUnNeededBehaviors()
    {
        _services.Any(service => service.ImplementationType == typeof(RequestPreProcessorBehavior<,>))
            .ShouldBeFalse();
        _services.Any(service => service.ImplementationType == typeof(RequestPostProcessorBehavior<,>))
            .ShouldBeFalse();
        _services.Any(service => service.ImplementationType == typeof(RequestExceptionActionProcessorBehavior<,>))
            .ShouldBeFalse();
        _services.Any(service => service.ImplementationType == typeof(RequestExceptionProcessorBehavior<,>))
            .ShouldBeFalse();
    }
}