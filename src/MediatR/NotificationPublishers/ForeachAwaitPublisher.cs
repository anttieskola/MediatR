using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.NotificationPublishers;

/// <summary>
/// Awaits each notification handler in a single foreach loop:
/// <code>
/// foreach (var handler in handlers) {
///     await handler(notification, cancellationToken).ConfigureAwait(false);
/// }
/// </code>
/// </summary>
public class ForeachAwaitPublisher : INotificationPublisher
{
    public async Task Publish(IEnumerable<NotificationHandlerExecutor> handlerExecutors, INotification notification, CancellationToken cancellationToken)
    {
        foreach (NotificationHandlerExecutor handler in handlerExecutors)
        {
            await handler.HandlerCallback(notification, cancellationToken).ConfigureAwait(false);
        }
    }
}