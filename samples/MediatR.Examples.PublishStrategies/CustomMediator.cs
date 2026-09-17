using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.Examples.PublishStrategies;

public class CustomMediator(IServiceProvider serviceFactory, Func<IEnumerable<NotificationHandlerExecutor>, INotification, CancellationToken, Task> publish)
    : Mediator(serviceFactory)
{
    private readonly Func<IEnumerable<NotificationHandlerExecutor>, INotification, CancellationToken, Task> _publish = publish;

    protected override Task PublishCore(IEnumerable<NotificationHandlerExecutor> handlerExecutors, INotification notification, CancellationToken cancellationToken)
        => _publish(handlerExecutors, notification, cancellationToken);
}