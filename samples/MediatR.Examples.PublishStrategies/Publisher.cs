using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.Examples.PublishStrategies;

public class Publisher
{
    public Publisher(IServiceProvider serviceFactory)
    {
        PublishStrategies[PublishStrategy.Async] = new CustomMediator(serviceFactory, AsyncContinueOnException);
        PublishStrategies[PublishStrategy.ParallelNoWait] = new CustomMediator(serviceFactory, ParallelNoWait);
        PublishStrategies[PublishStrategy.ParallelWhenAll] = new CustomMediator(serviceFactory, ParallelWhenAll);
        PublishStrategies[PublishStrategy.ParallelWhenAny] = new CustomMediator(serviceFactory, ParallelWhenAny);
        PublishStrategies[PublishStrategy.SyncContinueOnException] = new CustomMediator(serviceFactory, SyncContinueOnException);
        PublishStrategies[PublishStrategy.SyncStopOnException] = new CustomMediator(serviceFactory, SyncStopOnException);
    }

    public IDictionary<PublishStrategy, IMediator> PublishStrategies { get; } = new Dictionary<PublishStrategy, IMediator>();
    public PublishStrategy DefaultStrategy { get; set; } = PublishStrategy.SyncContinueOnException;

    public Task Publish<TNotification>(TNotification notification)
        => Publish(notification, DefaultStrategy, default);

    public Task Publish<TNotification>(TNotification notification, PublishStrategy strategy)
        => Publish(notification, strategy, default);

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken)
        => Publish(notification, DefaultStrategy, cancellationToken);

    public Task Publish<TNotification>(TNotification notification, PublishStrategy strategy, CancellationToken cancellationToken) => !PublishStrategies.TryGetValue(strategy, out IMediator mediator)
            ? throw new ArgumentException($"Unknown strategy: {strategy}")
            : mediator.Publish(notification, cancellationToken);

    private static Task ParallelWhenAll(IEnumerable<NotificationHandlerExecutor> handlers, INotification notification, CancellationToken cancellationToken)
    {
        List<Task> tasks = [];

        foreach (NotificationHandlerExecutor handler in handlers)
        {
            tasks.Add(Task.Run(() => handler.HandlerCallback(notification, cancellationToken), cancellationToken));
        }

        return Task.WhenAll(tasks);
    }

    private static Task<Task> ParallelWhenAny(IEnumerable<NotificationHandlerExecutor> handlers, INotification notification, CancellationToken cancellationToken)
    {
        List<Task> tasks = [];

        foreach (NotificationHandlerExecutor handler in handlers)
        {
            tasks.Add(Task.Run(() => handler.HandlerCallback(notification, cancellationToken), cancellationToken));
        }

        return Task.WhenAny(tasks);
    }

    private static Task ParallelNoWait(IEnumerable<NotificationHandlerExecutor> handlers, INotification notification, CancellationToken cancellationToken)
    {
        foreach (NotificationHandlerExecutor handler in handlers)
        {
            _ = Task.Run(() => handler.HandlerCallback(notification, cancellationToken), cancellationToken);
        }

        return Task.CompletedTask;
    }

    private static async Task AsyncContinueOnException(IEnumerable<NotificationHandlerExecutor> handlers, INotification notification, CancellationToken cancellationToken)
    {
        List<Task> tasks = [];
        List<Exception> exceptions = [];

        foreach (NotificationHandlerExecutor handler in handlers)
        {
            try
            {
                tasks.Add(handler.HandlerCallback(notification, cancellationToken));
            }
            catch (Exception ex) when (ex is not (OutOfMemoryException or StackOverflowException))
            {
                exceptions.Add(ex);
            }
        }

        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (AggregateException ex)
        {
            exceptions.AddRange(ex.Flatten().InnerExceptions);
        }
        catch (Exception ex) when (ex is not (OutOfMemoryException or StackOverflowException))
        {
            exceptions.Add(ex);
        }

        if (exceptions.Count != 0)
        {
            throw new AggregateException(exceptions);
        }
    }

    private static async Task SyncStopOnException(IEnumerable<NotificationHandlerExecutor> handlers, INotification notification, CancellationToken cancellationToken)
    {
        foreach (NotificationHandlerExecutor handler in handlers)
        {
            await handler.HandlerCallback(notification, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task SyncContinueOnException(IEnumerable<NotificationHandlerExecutor> handlers, INotification notification, CancellationToken cancellationToken)
    {
        List<Exception> exceptions = [];

        foreach (NotificationHandlerExecutor handler in handlers)
        {
            try
            {
                await handler.HandlerCallback(notification, cancellationToken).ConfigureAwait(false);
            }
            catch (AggregateException ex)
            {
                exceptions.AddRange(ex.Flatten().InnerExceptions);
            }
            catch (Exception ex) when (ex is not (OutOfMemoryException or StackOverflowException))
            {
                exceptions.Add(ex);
            }
        }

        if (exceptions.Count != 0)
        {
            throw new AggregateException(exceptions);
        }
    }
}