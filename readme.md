![Icon](https://raw.githubusercontent.com/devlooped/WebSocketChannel/main/assets/img/icon.png) WebSocketChannel
============

High-performance [System.Threading.Channels](https://devblogs.microsoft.com/dotnet/an-introduction-to-system-threading-channels/) API adapter for System.Net.WebSockets

[![Version](https://img.shields.io/nuget/v/WebSocketChannel.svg?color=royalblue)](https://www.nuget.org/packages/WebSocketChannel)
[![Downloads](https://img.shields.io/nuget/dt/WebSocketChannel.svg?color=green)](https://www.nuget.org/packages/WebSocketChannel)
[![License](https://img.shields.io/github/license/devlooped/WebSocketChannel.svg?color=blue)](https://github.com/devlooped/WebSocketChannel/blob/main/license.txt)
[![Build](https://github.com/devlooped/WebSocketChannel/workflows/build/badge.svg?branch=main)](https://github.com/devlooped/WebSocketChannel/actions)

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

[![CI Version](https://img.shields.io/endpoint?url=https://shields.kzu.io/vpre/WebSocketChannel/main&label=nuget.ci&color=brightgreen)](https://pkg.kzu.io/index.json)
[![Build](https://github.com/devlooped/WebSocketChannel/workflows/build/badge.svg?branch=main)](https://github.com/devlooped/WebSocketChannel/actions)

We also produce CI packages from branches and pull requests so you can dogfood builds as quickly as they are produced. 

The CI feed is `https://pkg.kzu.io/index.json`. 

The versioning scheme for packages is:

- PR builds: *42.42.42-pr*`[NUMBER]`
- Branch builds: *42.42.42-*`[BRANCH]`.`[COMMITS]`

<!-- #sponsors -->
<!-- include https://github.com/devlooped/sponsors/raw/main/footer.md -->
# Sponsors 

<!-- sponsors.md -->
[![Clarius Org](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/clarius.png "Clarius Org")](https://github.com/clarius)
[![Kirill Osenkov](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/KirillOsenkov.png "Kirill Osenkov")](https://github.com/KirillOsenkov)
[![MFB Technologies, Inc.](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/MFB-Technologies-Inc.png "MFB Technologies, Inc.")](https://github.com/MFB-Technologies-Inc)
[![Stephen Shaw](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/decriptor.png "Stephen Shaw")](https://github.com/decriptor)
[![Torutek](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/torutek-gh.png "Torutek")](https://github.com/torutek-gh)
[![DRIVE.NET, Inc.](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/drivenet.png "DRIVE.NET, Inc.")](https://github.com/drivenet)
[![Ashley Medway](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/AshleyMedway.png "Ashley Medway")](https://github.com/AshleyMedway)
[![Keith Pickford](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/Keflon.png "Keith Pickford")](https://github.com/Keflon)
[![Thomas Bolon](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/tbolon.png "Thomas Bolon")](https://github.com/tbolon)
[![Kori Francis](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/kfrancis.png "Kori Francis")](https://github.com/kfrancis)
[![Toni Wenzel](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/twenzel.png "Toni Wenzel")](https://github.com/twenzel)
[![Giorgi Dalakishvili](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/Giorgi.png "Giorgi Dalakishvili")](https://github.com/Giorgi)
[![Mike James](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/MikeCodesDotNET.png "Mike James")](https://github.com/MikeCodesDotNET)
[![Dan Siegel](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/dansiegel.png "Dan Siegel")](https://github.com/dansiegel)
[![Reuben Swartz](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/rbnswartz.png "Reuben Swartz")](https://github.com/rbnswartz)
[![Jacob Foshee](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/jfoshee.png "Jacob Foshee")](https://github.com/jfoshee)
[![](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/Mrxx99.png "")](https://github.com/Mrxx99)
[![Eric Johnson](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/eajhnsn1.png "Eric Johnson")](https://github.com/eajhnsn1)
[![Norman Mackay](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/mackayn.png "Norman Mackay")](https://github.com/mackayn)
[![Certify The Web](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/certifytheweb.png "Certify The Web")](https://github.com/certifytheweb)
[![Ix Technologies B.V.](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/IxTechnologies.png "Ix Technologies B.V.")](https://github.com/IxTechnologies)
[![David JENNI](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/davidjenni.png "David JENNI")](https://github.com/davidjenni)
[![Jonathan ](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/Jonathan-Hickey.png "Jonathan ")](https://github.com/Jonathan-Hickey)
[![Oleg Kyrylchuk](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/okyrylchuk.png "Oleg Kyrylchuk")](https://github.com/okyrylchuk)
[![Charley Wu](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/akunzai.png "Charley Wu")](https://github.com/akunzai)
[![Jakob Tikjøb Andersen](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/jakobt.png "Jakob Tikjøb Andersen")](https://github.com/jakobt)
[![Seann Alexander](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/seanalexander.png "Seann Alexander")](https://github.com/seanalexander)
[![Tino Hager](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/tinohager.png "Tino Hager")](https://github.com/tinohager)
[![Mark Seemann](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/ploeh.png "Mark Seemann")](https://github.com/ploeh)
[![Angelo Belchior](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/angelobelchior.png "Angelo Belchior")](https://github.com/angelobelchior)
[![Ken Bonny](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/KenBonny.png "Ken Bonny")](https://github.com/KenBonny)
[![Simon Cropp](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/SimonCropp.png "Simon Cropp")](https://github.com/SimonCropp)
[![agileworks-eu](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/agileworks-eu.png "agileworks-eu")](https://github.com/agileworks-eu)
[![sorahex](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/sorahex.png "sorahex")](https://github.com/sorahex)
[![Zheyu Shen](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/arsdragonfly.png "Zheyu Shen")](https://github.com/arsdragonfly)
[![Vezel](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/vezel-dev.png "Vezel")](https://github.com/vezel-dev)
[![Georg Jung](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/georg-jung.png "Georg Jung")](https://github.com/georg-jung)


<!-- sponsors.md -->

[![Sponsor this project](https://raw.githubusercontent.com/devlooped/sponsors/main/sponsor.png "Sponsor this project")](https://github.com/sponsors/devlooped)
&nbsp;

[Learn more about GitHub Sponsors](https://github.com/sponsors)

<!-- https://github.com/devlooped/sponsors/raw/main/footer.md -->
