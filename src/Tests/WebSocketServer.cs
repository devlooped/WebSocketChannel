using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Devlooped.Net;

public record WebSocketServer(Uri Uri, Task Completion, CancellationTokenSource Cancellation) : IAsyncDisposable, IDisposable
{
    static int serverPort = 10000;

    public static WebSocketServer Create(ITestOutputHelper? output = null)
        => Create(Echo, output);

    public static WebSocketServer Create(Func<WebSocket, CancellationToken, Task> behavior, ITestOutputHelper? output = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });

        var port = Interlocked.Increment(ref serverPort);

        // Only turn on output loggig when running tests in the IDE, for easier troubleshooting.
        if (output != null && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VSAPPIDNAME")))
            builder.Logging.AddProvider(new LoggingProvider(output));

        var app = builder.Build();
        app.Urls.Add("http://localhost:" + port);

        app.UseWebSockets();

        var cts = new CancellationTokenSource();

        app.Use(async (context, next) =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await next();
            }
            else
            {
                using var websocket = await context.WebSockets.AcceptWebSocketAsync(
                    context.WebSockets.WebSocketRequestedProtocols.FirstOrDefault());

                await behavior(websocket, cts.Token);
                //await Task.Run(() => behavior(websocket, cts.Token));
            }
        });

        var completion = app.RunAsync(cts.Token);
        return new WebSocketServer(new Uri("ws://localhost:" + port), completion, cts);
    }

    public void Dispose()
    {
        Cancellation.Cancel();
        Completion.Wait();
    }

    public async ValueTask DisposeAsync()
    {
        Cancellation.Cancel();
        await Completion;
    }

    static async Task Echo(WebSocket webSocket, CancellationToken cancellation)
    {
        while (webSocket.State == WebSocketState.Open && !cancellation.IsCancellationRequested)
        {
            try
            {
                var pipe = new Pipe();
                var received = await webSocket.ReceiveAsync(pipe.Writer.GetMemory(512), cancellation).ConfigureAwait(false);
                while (!cancellation.IsCancellationRequested && !received.EndOfMessage && received.MessageType != WebSocketMessageType.Close)
                {
                    if (received.Count == 0)
                        break;

                    pipe.Writer.Advance(received.Count);
                    received = await webSocket.ReceiveAsync(pipe.Writer.GetMemory(512), cancellation).ConfigureAwait(false);
                }

                // We didn't get a complete message, we can't flush partial message.
                if (cancellation.IsCancellationRequested || !received.EndOfMessage)
                    break;

                if (received.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseOutputAsync(webSocket.CloseStatus ?? WebSocketCloseStatus.NormalClosure, webSocket.CloseStatusDescription, cancellation);
                    break;
                }

                // Advance the EndOfMessage bytes before flushing.
                pipe.Writer.Advance(received.Count);
                if (await pipe.Writer.FlushAsync(cancellation).ConfigureAwait(false) is var flushed && flushed.IsCompleted)
                    break;

                // Read what we just wrote with the flush.
                if (await pipe.Reader.ReadAsync(cancellation).ConfigureAwait(false) is var read && !read.IsCompleted && !read.IsCanceled)
                {
                    if (read.Buffer.IsSingleSegment)
                    {
                        await webSocket.SendAsync(read.Buffer.First, WebSocketMessageType.Binary, true, cancellation);
                    }
                    else
                    {
                        var enumerator = read.Buffer.GetEnumerator();
                        var done = !enumerator.MoveNext();
                        while (!done)
                        {
                            done = !enumerator.MoveNext();

                            // NOTE: we don't use the cancellation here because we don't want to send 
                            // partial messages from an already completely read buffer. 
                            if (done)
                                await webSocket.SendAsync(enumerator.Current, WebSocketMessageType.Binary, true, cancellation);
                            else
                                await webSocket.SendAsync(enumerator.Current, WebSocketMessageType.Binary, false, cancellation);
                        }
                    }
                    pipe.Reader.AdvanceTo(read.Buffer.End);
                }
            }
            catch (Exception ex) when (ex is OperationCanceledException ||
                                       ex is WebSocketException ||
                                       ex is InvalidOperationException)
            {
                break;
            }
        }
    }

    record LoggingProvider(ITestOutputHelper Output) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new OutputLogger(Output);
        public void Dispose() { }
        record OutputLogger(ITestOutputHelper Output) : ILogger
        {
            public IDisposable BeginScope<TState>(TState state) => NullDisposable.Default;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
                Output.WriteLine($"{logLevel.ToString().Substring(0, 4)}: {formatter.Invoke(state, exception)}");
        }
    }

    class NullDisposable : IDisposable
    {
        public static IDisposable Default { get; } = new NullDisposable();
        NullDisposable() { }
        public void Dispose() { }
    }
}
