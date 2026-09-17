using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Linq;
using Xunit;

namespace MediatR.Tests.MicrosoftExtensionsDI;

public class DuplicateAssemblyResolutionTests
{
    private readonly IServiceProvider _provider;

    public DuplicateAssemblyResolutionTests()
    {
        IServiceCollection services = new ServiceCollection();
        _ = services.AddSingleton(new Logger());
        _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(Ping).Assembly, typeof(Ping).Assembly));
        _provider = services.BuildServiceProvider();
    }

    [Fact]
    public void ShouldResolveNotificationHandlersOnlyOnce() => _provider.GetServices<INotificationHandler<Pinged>>().Count().ShouldBe(4);
}