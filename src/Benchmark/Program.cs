using System.Net.WebSockets;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Running;
using Devlooped.Net;

BenchmarkRunner.Run<Benchmarks>();

[NativeMemoryProfiler]
[MemoryDiagnoser]
public class Benchmarks
{
    [Params(1000, 2000, 5000/*, 10000, 20000*/)]
    public int RunTime = 1000;

    [Benchmark]
    public async Task ReadAllBytes()
    {
        var cts = new CancellationTokenSource(RunTime);
        using var server = WebSocketServer.Create();
        using var client = new ClientWebSocket();
        await client.ConnectAsync(server.Uri, CancellationToken.None);
        var channel = client.CreateChannel();

        try
        {
            _ = Task.Run(async () =>
            {
                var mem = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()).AsMemory();
                while (!cts.IsCancellationRequested)
                    await channel.Writer.WriteAsync(mem);

                await server.DisposeAsync();
            });

            await foreach (var item in channel.Reader.ReadAllAsync(cts.Token))
            {
                Console.WriteLine(Encoding.UTF8.GetString(item.Span));
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}