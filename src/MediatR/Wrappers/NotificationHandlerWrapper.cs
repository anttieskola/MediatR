using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.Wrappers;

#pragma warning disable S1694 // Keep API original

public abstract class NotificationHandlerWrapper
{
    public abstract Task Handle(INotification notification, IServiceProvider serviceFactory,
        Func<IEnumerable<NotificationHandlerExecutor>, INotification, CancellationToken, Task> publish,
        CancellationToken cancellationToken);
}

public class NotificationHandlerWrapperImpl<TNotification> : NotificationHandlerWrapper
    where TNotification : INotification
{
    public override Task Handle(INotification notification, IServiceProvider serviceFactory,
        Func<IEnumerable<NotificationHandlerExecutor>, INotification, CancellationToken, Task> publish,
        CancellationToken cancellationToken)
    {
        IEnumerable<NotificationHandlerExecutor> handlers = serviceFactory
            .GetServices<INotificationHandler<TNotification>>()
            .Select(static x => new NotificationHandlerExecutor(x, (theNotification, theToken) => x.Handle((TNotification) theNotification, theToken)));

        return publish(handlers, notification, cancellationToken);
    }
}

#pragma warning restore S1694 // Keep API original