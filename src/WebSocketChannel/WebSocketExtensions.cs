using System.ComponentModel;
using System.Threading.Channels;
using Devlooped.Net;

namespace System.Net.WebSockets;

/// <summary>
/// Provides the <see cref="CreateChannel"/> extension method for 
/// reading/writing to a <see cref="WebSocket"/> using the <see cref="Channel{T}"/>
/// API.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
static partial class WebSocketExtensions
{
    /// <summary>
    /// Creates a channel over the given <paramref name="webSocket"/> for reading/writing 
    /// purposes.
    /// </summary>
    /// <param name="webSocket">The <see cref="WebSocket"/> to create the channel over.</param>
    /// <param name="displayName">Optional friendly name to identify this channel while debugging or troubleshooting.</param>
    /// <returns>A channel to read/write the given <paramref name="webSocket"/>.</returns>
    public static Channel<ReadOnlyMemory<byte>> CreateChannel(this WebSocket webSocket, string? displayName = default)
        => WebSocketChannel.Create(webSocket, displayName);
}

