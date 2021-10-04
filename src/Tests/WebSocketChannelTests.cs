using System.Diagnostics;

namespace Devlooped.Net;

public record WebSocketChannelTests(ITestOutputHelper Output)
{
    [Fact]
    public async Task EchoWorks()
    {
        using var server = WebSocketServer.Create();
        using var client = new ClientWebSocket();
        await client.ConnectAsync(server.Uri, CancellationToken.None);

        var channel = client.CreateChannel();

        await channel.Writer.WriteAsync(Encoding.UTF8.GetBytes("hello").AsMemory());

        var value = await channel.Reader.ReadAsync();

        Assert.Equal("hello", Encoding.UTF8.GetString(value.Span));
    }

    [Fact]
    public async Task EchoChannelWorks()
    {
        using var server = WebSocketServer.Create(EchoChannel);
        using var client = new ClientWebSocket();
        await client.ConnectAsync(server.Uri, CancellationToken.None);

        var channel = WebSocketChannel.Create(client);

        await channel.Writer.WriteAsync(Encoding.UTF8.GetBytes("hello").AsMemory());

        var value = await channel.Reader.ReadAsync();

        Assert.Equal("hello", Encoding.UTF8.GetString(value.Span));
        server.Dispose();
    }

    [Fact]
    public async Task EchoWorksWithTryWrite()
    {
        using var server = WebSocketServer.Create();
        using var client = new ClientWebSocket();
        await client.ConnectAsync(server.Uri, CancellationToken.None);

        var channel = WebSocketChannel.Create(client);

        Assert.True(channel.Writer.TryWrite(Encoding.UTF8.GetBytes("hello").AsMemory()));

        var value = await channel.Reader.ReadAsync();

        Assert.Equal("hello", Encoding.UTF8.GetString(value.Span));
    }

    [Fact]
    public async Task EchoWorksWithTryRead()
    {
        using var server = WebSocketServer.Create();
        using var client = new ClientWebSocket();
        await client.ConnectAsync(server.Uri, CancellationToken.None);

        var channel = WebSocketChannel.Create(client);

        Assert.True(channel.Writer.TryWrite(Encoding.UTF8.GetBytes("hello").AsMemory()));

        var cts = new CancellationTokenSource(Debugger.IsAttached ? int.MaxValue : 250);
        var value = ReadOnlyMemory<byte>.Empty;
        while (!channel.Reader.TryRead(out value) && !cts.IsCancellationRequested)
            ;

        Assert.False(cts.IsCancellationRequested);
        Assert.Equal("hello", Encoding.UTF8.GetString(value.Span));
    }

    [Fact]
    public async Task WhenReadingAll_ThenCompletesWhenSocketClosed()
    {
        var cts = new CancellationTokenSource(50);
        using var server = WebSocketServer.Create((socket, cancellation) => EchoChannel(socket, cts.Token));
        using var client = new ClientWebSocket();
        await client.ConnectAsync(server.Uri, CancellationToken.None);

        var channel = WebSocketChannel.Create(client);

        _ = Task.Run(async () =>
        {
            while (!cts.IsCancellationRequested)
                await channel.Writer.WriteAsync(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()));
        });

        await foreach (var item in channel.Reader.ReadAllAsync())
        {
            _ = Encoding.UTF8.GetString(item.Span);
        }

        Assert.True(channel.Reader.Completion.IsCompleted);
    }

    [Fact]
    public async Task WhenSocketNotOpen_ThenWriteThrows()
    {
        using var client = new ClientWebSocket();

        var channel = WebSocketChannel.Create(client);

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await channel.Writer.WriteAsync(Encoding.UTF8.GetBytes("hello").AsMemory()));
    }

    [Fact]
    public async Task WhenSocketClosed_ThenWriteThrows()
    {
        using var server = WebSocketServer.Create();
        using var client = new ClientWebSocket();
        await client.ConnectAsync(server.Uri, CancellationToken.None);

        var channel = WebSocketChannel.Create(client);

        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", default);

        await Assert.ThrowsAsync<WebSocketException>(
            async () => await channel.Writer.WriteAsync(Encoding.UTF8.GetBytes("hello").AsMemory()));
    }

    [Fact]
    public async Task WhenSocketClosed_ThenReadThrows()
    {
        using var server = WebSocketServer.Create();
        using var client = new ClientWebSocket();
        await client.ConnectAsync(server.Uri, CancellationToken.None);

        var channel = WebSocketChannel.Create(client);

        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", default);

        await Assert.ThrowsAsync<WebSocketException>(async () => await channel.Reader.ReadAsync());
    }

    [Fact]
    public async Task WhenSocketClosed_ThenTryReadReturnsFalse()
    {
        using var server = WebSocketServer.Create();
        using var client = new ClientWebSocket();
        await client.ConnectAsync(server.Uri, CancellationToken.None);

        var channel = WebSocketChannel.Create(client);

        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", default);

        Assert.False(channel.Reader.TryRead(out _));
    }

    [Fact]
    public async Task WhenChannelCompleteWithError_ThenReadThrowsSameError()
    {
        using var server = WebSocketServer.Create();
        using var client = new ClientWebSocket();
        await client.ConnectAsync(server.Uri, CancellationToken.None);

        var channel = WebSocketChannel.Create(client);

        channel.Writer.Complete(new ArgumentException("foo", "foo"));

        await Assert.ThrowsAsync<ArgumentException>("foo",
            async () => await channel.Reader.ReadAsync());
    }

    [Fact]
    public async Task WhenChannelCompleteWithError_ThenTryReadReturnsFalse()
    {
        using var server = WebSocketServer.Create();
        using var client = new ClientWebSocket();
        await client.ConnectAsync(server.Uri, CancellationToken.None);

        var channel = WebSocketChannel.Create(client);

        channel.Writer.Complete(new ArgumentException("foo", "foo"));

        Assert.False(channel.Reader.TryRead(out _));
    }

    [Fact]
    public async Task WhenChannelCompleteWithError_ThenWaitToReadThrows()
    {
        using var server = WebSocketServer.Create();
        using var client = new ClientWebSocket();
        await client.ConnectAsync(server.Uri, CancellationToken.None);

        var channel = WebSocketChannel.Create(client);

        channel.Writer.Complete(new ArgumentException("foo", "foo"));

        await Assert.ThrowsAsync<ArgumentException>("foo",
            async () => await channel.Reader.WaitToReadAsync());
    }

    [Fact]
    public async Task WhenChannelCompleteWithError_ThenWriteThrows()
    {
        using var server = WebSocketServer.Create();
        using var client = new ClientWebSocket();
        await client.ConnectAsync(server.Uri, CancellationToken.None);

        var channel = WebSocketChannel.Create(client);

        channel.Writer.Complete(new ArgumentException("foo", "foo"));

        await Assert.ThrowsAsync<ArgumentException>("foo",
            async () => await channel.Writer.WriteAsync(Encoding.UTF8.GetBytes("hello").AsMemory()));
    }

    [Fact]
    public async Task WhenChannelCompleteWithError_ThenWaitToWriteThrows()
    {
        using var server = WebSocketServer.Create();
        using var client = new ClientWebSocket();
        await client.ConnectAsync(server.Uri, CancellationToken.None);

        var channel = WebSocketChannel.Create(client);

        channel.Writer.Complete(new ArgumentException("foo", "foo"));

        await Assert.ThrowsAsync<ArgumentException>("foo",
            async () => await channel.Writer.WaitToWriteAsync());
    }

    [Fact]
    public async Task WhenTokenCancelled_ThenWaitToWriteThrows()
    {
        using var client = new ClientWebSocket();
        var channel = WebSocketChannel.Create(client);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(async () => await channel.Writer.WaitToWriteAsync(cts.Token));
    }

    [Fact]
    public async Task WhenTokenCancelled_ThenWriteThrows()
    {
        using var client = new ClientWebSocket();
        var channel = WebSocketChannel.Create(client);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(async () => await channel.Writer.WriteAsync(ReadOnlyMemory<byte>.Empty, cts.Token));
    }

    [Fact]
    public async Task WhenSocketNotOpen_ThenWaitToWriteReturnsFalse()
    {
        using var client = new ClientWebSocket();
        var channel = WebSocketChannel.Create(client);

        Assert.False(await channel.Writer.WaitToWriteAsync());
    }

    [Fact]
    public async Task WhenSocketNotOpen_ThenWaitToReadReturnsFalse()
    {
        using var client = new ClientWebSocket();
        var channel = WebSocketChannel.Create(client);

        Assert.False(await channel.Reader.WaitToReadAsync());
    }

    [Fact]
    public async Task WhenSocketOpen_ThenWaitToWriteReturnsTrue()
    {
        using var server = WebSocketServer.Create();
        using var client = new ClientWebSocket();
        await client.ConnectAsync(server.Uri, CancellationToken.None);
        var channel = WebSocketChannel.Create(client);

        Assert.True(await channel.Writer.WaitToWriteAsync());
    }

    [Fact]
    public async Task WhenSocketOpen_ThenWaitToReadReturnsTrue()
    {
        using var server = WebSocketServer.Create();
        using var client = new ClientWebSocket();
        await client.ConnectAsync(server.Uri, CancellationToken.None);
        var channel = WebSocketChannel.Create(client);

        Assert.True(await channel.Reader.WaitToReadAsync());
    }

    [Fact]
    public void WhenChannelCompleteWithError_ThenTryCompleteReturnsFalse()
    {
        using var client = new ClientWebSocket();
        var channel = WebSocketChannel.Create(client);
        channel.Writer.Complete(new ArgumentException("foo", "foo"));

        Assert.False(channel.Writer.TryComplete());
    }

    [Fact]
    public void WhenChannelComplete_ThenReaderCompletionIsCompletedSuccessfully()
    {
        using var client = new ClientWebSocket();
        var channel = WebSocketChannel.Create(client);
        channel.Writer.Complete();

        Assert.True(channel.Reader.Completion.IsCompletedSuccessfully);
    }

    [Fact]
    public void WhenChannelComplete_ThenTryWriteReturnsFalse()
    {
        using var client = new ClientWebSocket();
        var channel = WebSocketChannel.Create(client);
        channel.Writer.Complete();

        Assert.False(channel.Writer.TryWrite(ReadOnlyMemory<byte>.Empty));
    }

    [Fact]
    public void WhenChannelCompleteWithError_ThenReaderCompletionIsFaulted()
    {
        using var client = new ClientWebSocket();
        var channel = WebSocketChannel.Create(client);
        channel.Writer.Complete(new Exception("error"));

        Assert.True(channel.Reader.Completion.IsFaulted);
    }

    [Fact]
    public async Task WhenConcurrentlyWriting_ThenSerializesWrites()
    {
        using var server = WebSocketServer.Create();
        using var client = new ClientWebSocket();
        await client.ConnectAsync(server.Uri, CancellationToken.None);

        var channel = WebSocketChannel.Create(client);

        await Task.WhenAll(Enumerable.Range(0, 25).Select(_ => Task.Run(
            async () => await channel.Writer.WriteAsync(Encoding.UTF8.GetBytes("hello").AsMemory()))).ToArray());
    }

    [Fact]
    public async Task WhenConcurrentlyReading_ThenSerializesReads()
    {
        using var server = WebSocketServer.Create();
        using var client = new ClientWebSocket();
        await client.ConnectAsync(server.Uri, CancellationToken.None);

        var channel = WebSocketChannel.Create(client);

        var tasks = new List<Task>(Enumerable.Range(0, 25).Select(_ => Task.Run(
            async () => await channel.Writer.WriteAsync(Encoding.UTF8.GetBytes("hello").AsMemory()))));

        tasks.AddRange(Enumerable.Range(0, 25).Select(_ => Task.Run(
            async () => await channel.Reader.ReadAsync())));

        await Task.WhenAll(tasks.ToArray());
    }

    [Fact]
    public async Task WhenTokenCancelled_ThenWaitToReadThrows()
    {
        using var client = new ClientWebSocket();
        var channel = WebSocketChannel.Create(client);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(async () => await channel.Reader.WaitToReadAsync(cts.Token));
    }

    [Fact]
    public async Task WhenTokenCancelled_ThenReadThrows()
    {
        using var client = new ClientWebSocket();
        var channel = WebSocketChannel.Create(client);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(async () => await channel.Reader.ReadAsync(cts.Token));
    }

    [Fact]
    public async Task WhenSocketRequestsClose_ThenReadThrows()
    {
        using var server = WebSocketServer.Create((socket, cancellation)
            => Task.Delay(50).ContinueWith(_ => socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Go", cancellation)));

        using var client = new ClientWebSocket();
        await client.ConnectAsync(server.Uri, CancellationToken.None);
        var channel = WebSocketChannel.Create(client);

        await Assert.ThrowsAsync<WebSocketException>(
            async () => await channel.Reader.ReadAsync());
    }

    [Fact]
    public async Task WhenCompletingWriter_ThenClosesSocket()
    {
        using var server = WebSocketServer.Create();
        using var client = new ClientWebSocket();
        await client.ConnectAsync(server.Uri, CancellationToken.None);

        var channel = WebSocketChannel.Create(client);
        Assert.Equal(WebSocketState.Open, client.State);

        Assert.True(channel.Writer.TryComplete());
        Assert.NotEqual(WebSocketState.Open, client.State);
    }

    [Fact]
    public async Task WhenCompletingWriterWithError_ThenClosesClientSocketStatusDescription()
    {
        using var server = WebSocketServer.Create();
        using var client = new ClientWebSocket();
        await client.ConnectAsync(server.Uri, CancellationToken.None);

        var channel = WebSocketChannel.Create(client);

        Assert.True(channel.Writer.TryComplete(new Exception("Client broken")));
        Assert.Equal("Client broken", client.CloseStatusDescription);
        Assert.Equal(WebSocketCloseStatus.NormalClosure, client.CloseStatus);
    }

    [Fact]
    public async Task WhenCompletingServerSocketWithError_ThenClosesClientSocketInternalServerError()
    {
        using var server = WebSocketServer.Create((socket, cancellation)
            => Task.Delay(50).ContinueWith(_ => WebSocketChannel.Create(socket).Writer.Complete(new Exception("Server broken"))));
        using var client = new ClientWebSocket();
        await client.ConnectAsync(server.Uri, CancellationToken.None);

        var channel = WebSocketChannel.Create(client);

        while (channel.Reader.TryRead(out _))
            ;

        Assert.Equal("Server broken", client.CloseStatusDescription);
        Assert.Equal(WebSocketCloseStatus.InternalServerError, client.CloseStatus);
    }

    static async Task EchoChannel(WebSocket webSocket, CancellationToken cancellation)
    {
        var channel = WebSocketChannel.Create(webSocket);
        try
        {
            await foreach (var item in channel.Reader.ReadAllAsync(cancellation))
            {
                await channel.Writer.WriteAsync(item, cancellation);
            }
        }
        catch (OperationCanceledException)
        {
            try
            {
                await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, default);
            }
            catch { } // Best effort to try closing cleanly. Client may be entirely gone.
        }
    }
}
