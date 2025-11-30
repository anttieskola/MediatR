#pragma warning disable IDE0130 // Namespace is on purpose for dependency injection extensions
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace is on purpose for dependency injection extensions

public enum RequestExceptionActionProcessorStrategy
{
    ApplyForUnhandledExceptions,
    ApplyForAllExceptions
}