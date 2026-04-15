![Icon](https://raw.githubusercontent.com/devlooped/WebSocketChannel/main/assets/img/icon.png) WebSocketChannel
============

High-performance [System.Threading.Channels](https://devblogs.microsoft.com/dotnet/an-introduction-to-system-threading-channels/) API adapter for System.Net.WebSockets

[![Version](https://img.shields.io/nuget/v/WebSocketChannel.svg?color=royalblue)](https://www.nuget.org/packages/WebSocketChannel)
[![Downloads](https://img.shields.io/nuget/dt/WebSocketChannel.svg?color=green)](https://www.nuget.org/packages/WebSocketChannel)
[![License](https://img.shields.io/github/license/devlooped/WebSocketChannel.svg?color=blue)](https://github.com/devlooped/WebSocketChannel/blob/main/license.txt)
[![Build](https://img.shields.io/github/actions/workflow/status/devlooped/WebSocketChannel/build.yml?branch=main)](https://github.com/devlooped/WebSocketChannel/actions)

<!-- #content -->
# Usage

```csharp
var client = new ClientWebSocket();
await client.ConnectAsync(serverUri, CancellationToken.None);

Channel<ReadOnlyMemory<byte>> channel = client.CreateChannel();

await channel.Writer.WriteAsync(Encoding.UTF8.GetBytes("hello").AsMemory());

// Read single message when it arrives
ReadOnlyMemory<byte> response = await channel.Reader.ReadAsync();

// Read all messages while underlying websocket is open
await foreach (var item in channel.Reader.ReadAllAsync())
{
    Console.WriteLine(Encoding.UTF8.GetString(item.Span));
}

// Completing the writer closes the underlying websocket cleanly
channel.Writer.Complete();

// Can also complete reporting an error for the remote party
channel.Writer.Complete(new InvalidOperationException("Bad format"));
```


The `WebSocketChannel` can also be used on the server. The following example is basically 
taken from the documentation on [WebSockets in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/websockets?view=aspnetcore-5.0#configure-the-middleware) 
and adapted to use a `WebSocketChannel` to echo messages to the client:

```csharp
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            var channel = WebSocketChannel.Create(webSocket);
            try
            {
                await foreach (var item in channel.Reader.ReadAllAsync(context.RequestAborted))
                {
                    await channel.Writer.WriteAsync(item, context.RequestAborted);
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
        else
        {
            context.Response.StatusCode = (int) HttpStatusCode.BadRequest;
        }
    }
    else
    {
        await next();
    }
});
```

<!-- #content -->
# Installation

This project can be used either as a regular nuget package:

```
<PackageReference Include="WebSocketChannel" Version="*" />
```

Or alternatively, referenced directly as a source-only dependency using [dotnet-file](https://www.nuget.org/packages/dotnet-file):

```
> dotnet file add https://github.com/devlooped/WebSocketChannel/blob/main/src/WebSocketChannel/WebSocketChannel.cs
> dotnet file add https://github.com/devlooped/WebSocketChannel/blob/main/src/WebSocketChannel/WebSocketExtensions.cs
```

It's also possible to specify a desired target location for the referenced source files, such as:

```
> dotnet file add https://github.com/devlooped/WebSocketChannel/blob/main/src/WebSocketChannel/WebSocketChannel.cs src/MyProject/External/.
> dotnet file add https://github.com/devlooped/WebSocketChannel/blob/main/src/WebSocketChannel/WebSocketExtensions.cs src/MyProject/External/.
```

When referenced as loose source files, it's easy to also get automated PRs when the upstream files change, 
as in the [dotnet-file.yml](https://github.com/devlooped/dotnet-file/blob/main/.github/workflows/dotnet-file.yml) workflow that 
keeps the repository up to date with a template. See also [dotnet-config](https://dotnetconfig.org), which is used to 
for the `dotnet-file` configuration settings that tracks all this.



# Dogfooding

[![CI Version](https://img.shields.io/endpoint?url=https://shields.kzu.app/vpre/WebSocketChannel/main&label=nuget.ci&color=brightgreen)](https://pkg.kzu.app/index.json)
[![Build](https://github.com/devlooped/WebSocketChannel/workflows/build/badge.svg?branch=main)](https://github.com/devlooped/WebSocketChannel/actions)

We also produce CI packages from branches and pull requests so you can dogfood builds as quickly as they are produced. 

The CI feed is `https://pkg.kzu.app/index.json`. 

The versioning scheme for packages is:

- PR builds: *42.42.42-pr*`[NUMBER]`
- Branch builds: *42.42.42-*`[BRANCH]`.`[COMMITS]`

<!-- #sponsors -->
<!-- include https://github.com/devlooped/sponsors/raw/main/footer.md -->
# Sponsors 

<!-- sponsors.md -->
[![Clarius Org](https://avatars.githubusercontent.com/u/71888636?v=4&s=39 "Clarius Org")](https://github.com/clarius)
[![MFB Technologies, Inc.](https://avatars.githubusercontent.com/u/87181630?v=4&s=39 "MFB Technologies, Inc.")](https://github.com/MFB-Technologies-Inc)
[![Khamza Davletov](https://avatars.githubusercontent.com/u/13615108?u=11b0038e255cdf9d1940fbb9ae9d1d57115697ab&v=4&s=39 "Khamza Davletov")](https://github.com/khamza85)
[![SandRock](https://avatars.githubusercontent.com/u/321868?u=99e50a714276c43ae820632f1da88cb71632ec97&v=4&s=39 "SandRock")](https://github.com/sandrock)
[![DRIVE.NET, Inc.](https://avatars.githubusercontent.com/u/15047123?v=4&s=39 "DRIVE.NET, Inc.")](https://github.com/drivenet)
[![Keith Pickford](https://avatars.githubusercontent.com/u/16598898?u=64416b80caf7092a885f60bb31612270bffc9598&v=4&s=39 "Keith Pickford")](https://github.com/Keflon)
[![Thomas Bolon](https://avatars.githubusercontent.com/u/127185?u=7f50babfc888675e37feb80851a4e9708f573386&v=4&s=39 "Thomas Bolon")](https://github.com/tbolon)
[![Kori Francis](https://avatars.githubusercontent.com/u/67574?u=3991fb983e1c399edf39aebc00a9f9cd425703bd&v=4&s=39 "Kori Francis")](https://github.com/kfrancis)
[![Reuben Swartz](https://avatars.githubusercontent.com/u/724704?u=2076fe336f9f6ad678009f1595cbea434b0c5a41&v=4&s=39 "Reuben Swartz")](https://github.com/rbnswartz)
[![Jacob Foshee](https://avatars.githubusercontent.com/u/480334?v=4&s=39 "Jacob Foshee")](https://github.com/jfoshee)
[![](https://avatars.githubusercontent.com/u/33566379?u=bf62e2b46435a267fa246a64537870fd2449410f&v=4&s=39 "")](https://github.com/Mrxx99)
[![Eric Johnson](https://avatars.githubusercontent.com/u/26369281?u=41b560c2bc493149b32d384b960e0948c78767ab&v=4&s=39 "Eric Johnson")](https://github.com/eajhnsn1)
[![Jonathan ](https://avatars.githubusercontent.com/u/5510103?u=98dcfbef3f32de629d30f1f418a095bf09e14891&v=4&s=39 "Jonathan ")](https://github.com/Jonathan-Hickey)
[![Ken Bonny](https://avatars.githubusercontent.com/u/6417376?u=569af445b6f387917029ffb5129e9cf9f6f68421&v=4&s=39 "Ken Bonny")](https://github.com/KenBonny)
[![Simon Cropp](https://avatars.githubusercontent.com/u/122666?v=4&s=39 "Simon Cropp")](https://github.com/SimonCropp)
[![agileworks-eu](https://avatars.githubusercontent.com/u/5989304?v=4&s=39 "agileworks-eu")](https://github.com/agileworks-eu)
[![Zheyu Shen](https://avatars.githubusercontent.com/u/4067473?v=4&s=39 "Zheyu Shen")](https://github.com/arsdragonfly)
[![Vezel](https://avatars.githubusercontent.com/u/87844133?v=4&s=39 "Vezel")](https://github.com/vezel-dev)
[![ChilliCream](https://avatars.githubusercontent.com/u/16239022?v=4&s=39 "ChilliCream")](https://github.com/ChilliCream)
[![4OTC](https://avatars.githubusercontent.com/u/68428092?v=4&s=39 "4OTC")](https://github.com/4OTC)
[![domischell](https://avatars.githubusercontent.com/u/66068846?u=0a5c5e2e7d90f15ea657bc660f175605935c5bea&v=4&s=39 "domischell")](https://github.com/DominicSchell)
[![Adrian Alonso](https://avatars.githubusercontent.com/u/2027083?u=129cf516d99f5cb2fd0f4a0787a069f3446b7522&v=4&s=39 "Adrian Alonso")](https://github.com/adalon)
[![torutek](https://avatars.githubusercontent.com/u/33917059?v=4&s=39 "torutek")](https://github.com/torutek)
[![mccaffers](https://avatars.githubusercontent.com/u/16667079?u=110034edf51097a5ee82cb6a94ae5483568e3469&v=4&s=39 "mccaffers")](https://github.com/mccaffers)
[![Seika Logiciel](https://avatars.githubusercontent.com/u/2564602?v=4&s=39 "Seika Logiciel")](https://github.com/SeikaLogiciel)
[![Andrew Grant](https://avatars.githubusercontent.com/devlooped-user?s=39 "Andrew Grant")](https://github.com/wizardness)
[![Lars](https://avatars.githubusercontent.com/u/1727124?v=4&s=39 "Lars")](https://github.com/latonz)
[![prime167](https://avatars.githubusercontent.com/u/3722845?v=4&s=39 "prime167")](https://github.com/prime167)


<!-- sponsors.md -->
[![Sponsor this project](https://avatars.githubusercontent.com/devlooped-sponsor?s=118 "Sponsor this project")](https://github.com/sponsors/devlooped)

[Learn more about GitHub Sponsors](https://github.com/sponsors)

<!-- https://github.com/devlooped/sponsors/raw/main/footer.md -->
