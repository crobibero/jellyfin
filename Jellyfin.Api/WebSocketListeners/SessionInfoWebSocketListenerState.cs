using System.Collections.Frozen;
using System.Collections.Generic;
using MediaBrowser.Controller.Net;

namespace Jellyfin.Api.WebSocketListeners;

/// <summary>
/// The state of a connection subscribed to <see cref="SessionInfoWebSocketListener"/>.
/// </summary>
public class SessionInfoWebSocketListenerState : WebSocketListenerState
{
    /// <summary>
    /// Gets or sets the ids of the sessions in the last message sent on the connection.
    /// </summary>
    public IReadOnlySet<string> SentSessionIds { get; set; } = FrozenSet<string>.Empty;
}
