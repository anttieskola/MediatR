using MediatR.Internal;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.Pipeline;
/// <summary>
/// Behavior for executing all <see cref="IRequestExceptionAction{TRequest,TException}"/> instances
///     after an exception is thrown by the following pipeline steps
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public class RequestExceptionActionProcessorBehavior<TRequest, TResponse>(IServiceProvider serviceProvider) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            IEnumerable<Type> exceptionTypes = GetExceptionTypes(exception.GetType());

            List<(MethodInfo MethodInfo, object Action)> actionsForException = exceptionTypes
                .SelectMany(exceptionType => GetActionsForException(exceptionType, request))
                .GroupBy(static actionForException => actionForException.Action.GetType())
                .Select(static actionForException => actionForException.First())
                .Select(static actionForException => (MethodInfo: GetMethodInfoForAction(actionForException.ExceptionType), actionForException.Action))
                .ToList();

            foreach ((MethodInfo MethodInfo, object Action) actionForException in actionsForException)
            {
                try
                {
                    await ((Task) (actionForException.MethodInfo.Invoke(actionForException.Action, [request, exception, cancellationToken])
                                  ?? throw new InvalidOperationException($"Could not create task for action method {actionForException.MethodInfo}."))).ConfigureAwait(false);
                }
                catch (TargetInvocationException invocationException) when (invocationException.InnerException != null)
                {
                    // Unwrap invocation exception to throw the actual error
                    ExceptionDispatchInfo.Capture(invocationException.InnerException).Throw();
                }
            }

            throw;
        }
    }

    private static IEnumerable<Type> GetExceptionTypes(Type? exceptionType)
    {
        while (exceptionType != null && exceptionType != typeof(object))
        {
            yield return exceptionType;
            exceptionType = exceptionType.BaseType;
        }
    }

    private IEnumerable<(Type ExceptionType, object Action)> GetActionsForException(Type exceptionType, TRequest request)
    {
        Type exceptionActionInterfaceType = typeof(IRequestExceptionAction<,>).MakeGenericType(typeof(TRequest), exceptionType);
        Type enumerableExceptionActionInterfaceType = typeof(IEnumerable<>).MakeGenericType(exceptionActionInterfaceType);

        IEnumerable<object> actionsForException = (IEnumerable<object>) _serviceProvider.GetRequiredService(enumerableExceptionActionInterfaceType);

        return HandlersOrderer.Prioritize([.. actionsForException], request).Select(action => (exceptionType, action));
    }

    private static MethodInfo GetMethodInfoForAction(Type exceptionType)
    {
        Type exceptionActionInterfaceType = typeof(IRequestExceptionAction<,>).MakeGenericType(typeof(TRequest), exceptionType);

        MethodInfo actionMethodInfo =
            exceptionActionInterfaceType.GetMethod(nameof(IRequestExceptionAction<,>.Execute))
            ?? throw new InvalidOperationException(
                $"Could not find method {nameof(IRequestExceptionAction<,>.Execute)} on type {exceptionActionInterfaceType}");

        return actionMethodInfo;
    }
}
