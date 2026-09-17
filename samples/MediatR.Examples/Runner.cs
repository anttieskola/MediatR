using MediatR.Examples.ExceptionHandler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediatR.Examples;

public static class Runner
{
    public static async Task Run(IMediator mediator, WrappingWriter writer, string projectName, bool testStreams = false)
    {
        await writer.WriteLineAsync("===============");
        await writer.WriteLineAsync(projectName);
        await writer.WriteLineAsync("===============");
        await writer.WriteLineAsync();

        await writer.WriteLineAsync("Sending Ping...");
        Pong pong = await mediator.Send(new Ping { Message = "Ping" });
        await writer.WriteLineAsync("Received: " + pong.Message);
        await writer.WriteLineAsync();

        await writer.WriteLineAsync("Publishing Pinged...");
        await mediator.Publish(new Pinged());
        await writer.WriteLineAsync();

        bool failedPong = await PublishPonged(mediator, writer);
        bool failedJing = await SendJing(mediator, writer);
        bool failedSing = testStreams && await TestSingStream(mediator, writer);

        bool isHandlerForSameExceptionWorks = await IsHandlerForSameExceptionWorks(mediator, writer).ConfigureAwait(false);
        bool isHandlerForBaseExceptionWorks = await IsHandlerForBaseExceptionWorks(mediator, writer).ConfigureAwait(false);
        bool isHandlerForLessSpecificExceptionWorks = await IsHandlerForLessSpecificExceptionWorks(mediator, writer).ConfigureAwait(false);
        bool isPreferredHandlerForBaseExceptionWorks = await IsPreferredHandlerForBaseExceptionWorks(mediator, writer).ConfigureAwait(false);
        bool isOverriddenHandlerForBaseExceptionWorks = await IsOverriddenHandlerForBaseExceptionWorks(mediator, writer).ConfigureAwait(false);

        await writer.WriteLineAsync("---------------");
        string contents = writer.Contents;
        int[] order = new[] {
            contents.IndexOf("- Starting Up", StringComparison.OrdinalIgnoreCase),
            contents.IndexOf("-- Handling Request", StringComparison.OrdinalIgnoreCase),
            contents.IndexOf("--- Handled Ping", StringComparison.OrdinalIgnoreCase),
            contents.IndexOf("-- Finished Request", StringComparison.OrdinalIgnoreCase),
            contents.IndexOf("- All Done", StringComparison.OrdinalIgnoreCase),
            contents.IndexOf("- All Done with Ping", StringComparison.OrdinalIgnoreCase),
        };

        int[] streamOrder = new[] {
            contents.IndexOf("-- Handling StreamRequest", StringComparison.OrdinalIgnoreCase),
            contents.IndexOf("--- Handled Sing: Sing, Song", StringComparison.OrdinalIgnoreCase),
            contents.IndexOf("-- Finished StreamRequest", StringComparison.OrdinalIgnoreCase),
        };

        var results = new RunResults
        {
            RequestHandlers = contents.Contains("--- Handled Ping:"),
            VoidRequestsHandlers = contents.Contains("--- Handled Jing:"),
            PipelineBehaviors = contents.Contains("-- Handling Request"),
            RequestPreProcessors = contents.Contains("- Starting Up"),
            RequestPostProcessors = contents.Contains("- All Done"),
            ConstrainedGenericBehaviors = contents.Contains("- All Done with Ping") && !failedJing,
            OrderedPipelineBehaviors = order.SequenceEqual(order.OrderBy(i => i)),
            NotificationHandler = contents.Contains("Got pinged async"),
            MultipleNotificationHandlers = contents.Contains("Got pinged async") && contents.Contains("Got pinged also async"),
            ConstrainedGenericNotificationHandler = contents.Contains("Got pinged constrained async") && !failedPong,
            CovariantNotificationHandler = contents.Contains("Got notified"),
            HandlerForSameException = isHandlerForSameExceptionWorks,
            HandlerForBaseException = isHandlerForBaseExceptionWorks,
            HandlerForLessSpecificException = isHandlerForLessSpecificExceptionWorks,
            PreferredHandlerForBaseException = isPreferredHandlerForBaseExceptionWorks,
            OverriddenHandlerForBaseException = isOverriddenHandlerForBaseExceptionWorks,

            // Streams
            StreamRequestHandlers = contents.Contains("--- Handled Sing: Sing, Song") && !failedSing,
            StreamPipelineBehaviors = contents.Contains("-- Handling StreamRequest"),
            StreamOrderedPipelineBehaviors = streamOrder.SequenceEqual(streamOrder.OrderBy(i => i))
        };

        await writer.WriteLineAsync($"Request Handler....................................................{YN(results.RequestHandlers)}");
        await writer.WriteLineAsync($"Void Request Handler...............................................{YN(results.VoidRequestsHandlers)}");
        await writer.WriteLineAsync($"Pipeline Behavior..................................................{YN(results.PipelineBehaviors)}");
        await writer.WriteLineAsync($"Pre-Processor......................................................{YN(results.RequestPreProcessors)}");
        await writer.WriteLineAsync($"Post-Processor.....................................................{YN(results.RequestPostProcessors)}");
        await writer.WriteLineAsync($"Constrained Post-Processor.........................................{YN(results.ConstrainedGenericBehaviors)}");
        await writer.WriteLineAsync($"Ordered Behaviors..................................................{YN(results.OrderedPipelineBehaviors)}");
        await writer.WriteLineAsync($"Notification Handler...............................................{YN(results.NotificationHandler)}");
        await writer.WriteLineAsync($"Notification Handlers..............................................{YN(results.MultipleNotificationHandlers)}");
        await writer.WriteLineAsync($"Constrained Notification Handler...................................{YN(results.ConstrainedGenericNotificationHandler)}");
        await writer.WriteLineAsync($"Covariant Notification Handler.....................................{YN(results.CovariantNotificationHandler)}");
        await writer.WriteLineAsync($"Handler for inherited request with same exception used.............{YN(results.HandlerForSameException)}");
        await writer.WriteLineAsync($"Handler for inherited request with base exception used.............{YN(results.HandlerForBaseException)}");
        await writer.WriteLineAsync($"Handler for request with less specific exception used by priority..{YN(results.HandlerForLessSpecificException)}");
        await writer.WriteLineAsync($"Preferred handler for inherited request with base exception used...{YN(results.PreferredHandlerForBaseException)}");
        await writer.WriteLineAsync($"Overridden handler for inherited request with same exception used..{YN(results.OverriddenHandlerForBaseException)}");

        if (testStreams)
        {
            await writer.WriteLineAsync($"Stream Request Handler.............................................{YN(results.StreamRequestHandlers)}");
            await writer.WriteLineAsync($"Stream Pipeline Behavior...........................................{YN(results.StreamPipelineBehaviors)}");
            await writer.WriteLineAsync($"Stream Ordered Behaviors...........................................{YN(results.StreamOrderedPipelineBehaviors)}");
        }

        await writer.WriteLineAsync();
    }

    private static async Task<bool> PublishPonged(IMediator mediator, WrappingWriter writer)
    {
        await writer.WriteLineAsync("Publishing Ponged...");
        bool failedPong = false;
        try
        {
            await mediator.Publish(new Ponged());
        }
        catch (Exception e)
        {
            failedPong = true;
            await writer.WriteLineAsync(e.ToString());
        }
        await writer.WriteLineAsync();
        return failedPong;
    }

    private static async Task<bool> SendJing(IMediator mediator, WrappingWriter writer)
    {
        bool failedJing = false;
        await writer.WriteLineAsync("Sending Jing...");
        try
        {
            await mediator.Send(new Jing { Message = "Jing" });
        }
        catch (Exception e)
        {
            failedJing = true;
            await writer.WriteLineAsync(e.ToString());
        }
        await writer.WriteLineAsync();
        return failedJing;
    }

    private static async Task<bool> TestSingStream(IMediator mediator, WrappingWriter writer)
    {
        await writer.WriteLineAsync("Sending Sing...");
        try
        {
            string[] expected = new[]
            {
                "Singing do", "Singing re", "Singing mi", "Singing fa",
                "Singing so", "Singing la", "Singing ti", "Singing do"
            };
            int index = 0;
            bool failedSing = false;
            await foreach (Song song in mediator.CreateStream(new Sing { Message = "Sing" }))
            {
                if (index < expected.Length)
                {
                    failedSing = !song.Message.Contains(expected[index]);
                }

                failedSing = failedSing || (++index) > 10;
            }

            await writer.WriteLineAsync();
            return failedSing;
        }
        catch (Exception e)
        {
            await writer.WriteLineAsync(e.ToString());
            await writer.WriteLineAsync();
            return true;
        }
    }

    private static string YN(bool value) => value ? "Y" : "N";

    private static async Task<bool> IsHandlerForSameExceptionWorks(IMediator mediator, WrappingWriter writer)
    {
        bool isHandledCorrectly = false;

        await writer.WriteLineAsync("Checking handler to catch exact exception...");
        try
        {
            _ = await mediator.Send(new PingProtectedResource { Message = "Ping to protected resource" });
            isHandledCorrectly = IsExceptionHandledBy<ForbiddenException, AccessDeniedExceptionHandler>(writer);
        }
        catch (Exception e)
        {
            await writer.WriteLineAsync(e.Message);
        }
        await writer.WriteLineAsync();

        return isHandledCorrectly;
    }

    private static async Task<bool> IsHandlerForBaseExceptionWorks(IMediator mediator, WrappingWriter writer)
    {
        bool isHandledCorrectly = false;

        await writer.WriteLineAsync("Checking shared handler to catch exception by base type...");
        try
        {
            _ = await mediator.Send(new PingResource { Message = "Ping to missed resource" });
            isHandledCorrectly = IsExceptionHandledBy<ResourceNotFoundException, ConnectionExceptionHandler>(writer);
        }
        catch (Exception e)
        {
            await writer.WriteLineAsync(e.Message);
        }
        await writer.WriteLineAsync();

        return isHandledCorrectly;
    }

    private static async Task<bool> IsHandlerForLessSpecificExceptionWorks(IMediator mediator, WrappingWriter writer)
    {
        bool isHandledCorrectly = false;

        await writer.WriteLineAsync("Checking base handler to catch any exception...");
        try
        {
            _ = await mediator.Send(new PingResourceTimeout { Message = "Ping to ISS resource" });
            isHandledCorrectly = IsExceptionHandledBy<TaskCanceledException, CommonExceptionHandler>(writer);
        }
        catch (Exception e)
        {
            await writer.WriteLineAsync(e.Message);
        }
        await writer.WriteLineAsync();

        return isHandledCorrectly;
    }

    private static async Task<bool> IsPreferredHandlerForBaseExceptionWorks(IMediator mediator, WrappingWriter writer)
    {
        bool isHandledCorrectly = false;

        await writer.WriteLineAsync("Selecting preferred handler to handle exception...");

        try
        {
            _ = await mediator.Send(new ExceptionHandler.Overrides.PingResourceTimeout { Message = "Ping to ISS resource (preferred)" });
            isHandledCorrectly = IsExceptionHandledBy<TaskCanceledException, ExceptionHandler.Overrides.CommonExceptionHandler>(writer);
        }
        catch (Exception e)
        {
            await writer.WriteLineAsync(e.Message);
        }
        await writer.WriteLineAsync();

        return isHandledCorrectly;
    }

    private static async Task<bool> IsOverriddenHandlerForBaseExceptionWorks(IMediator mediator, WrappingWriter writer)
    {
        bool isHandledCorrectly = false;

        await writer.WriteLineAsync("Selecting new handler to handle exception...");

        try
        {
            _ = await mediator.Send(new PingNewResource { Message = "Ping to ISS resource (override)" });
            isHandledCorrectly = IsExceptionHandledBy<ServerException, ExceptionHandler.Overrides.ServerExceptionHandler>(writer);
        }
        catch (Exception e)
        {
            await writer.WriteLineAsync(e.Message);
        }
        await writer.WriteLineAsync();

        return isHandledCorrectly;
    }

    private static bool IsExceptionHandledBy<TException, THandler>(WrappingWriter writer)
        where TException : Exception
    {
        List<string> messages = writer.Contents.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
        if (messages.Count - 3 < 0)
        {
            return false;
        }

        // Note: For this handler type to be found in messages, it must be written in messages by LogExceptionAction
        return messages[^2].Contains(typeof(THandler).FullName)
               // Note: For this exception type to be found in messages, exception must be written in all tested exception handlers
               && messages[^3].Contains(typeof(TException).FullName);
    }
}

public class RunResults
{
    public bool RequestHandlers { get; set; }
    public bool VoidRequestsHandlers { get; set; }
    public bool PipelineBehaviors { get; set; }
    public bool RequestPreProcessors { get; set; }
    public bool RequestPostProcessors { get; set; }
    public bool OrderedPipelineBehaviors { get; set; }
    public bool ConstrainedGenericBehaviors { get; set; }
    public bool NotificationHandler { get; set; }
    public bool MultipleNotificationHandlers { get; set; }
    public bool CovariantNotificationHandler { get; set; }
    public bool ConstrainedGenericNotificationHandler { get; set; }
    public bool HandlerForSameException { get; set; }
    public bool HandlerForBaseException { get; set; }
    public bool HandlerForLessSpecificException { get; set; }
    public bool PreferredHandlerForBaseException { get; set; }
    public bool OverriddenHandlerForBaseException { get; set; }

    // Stream results
    public bool StreamRequestHandlers { get; set; }
    public bool StreamPipelineBehaviors { get; set; }
    public bool StreamOrderedPipelineBehaviors { get; set; }
}

public class WrappingWriter(TextWriter innerWriter) : TextWriter
{
    private readonly TextWriter _innerWriter = innerWriter;
    private readonly StringBuilder _stringWriter = new();

    public override void Write(char value)
    {
        _ = _stringWriter.Append(value);
        _innerWriter.Write(value);
    }

    public override Task WriteLineAsync(string value)
    {
        _ = _stringWriter.AppendLine(value);
        return _innerWriter.WriteLineAsync(value);
    }

    public override Encoding Encoding => _innerWriter.Encoding;

    public string Contents => _stringWriter.ToString();
}
