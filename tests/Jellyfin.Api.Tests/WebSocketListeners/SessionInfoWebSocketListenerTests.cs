using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.WebSocketListeners;
using Jellyfin.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Net.WebSocketMessages;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.WebSocketListeners;

public class SessionInfoWebSocketListenerTests
{
    private readonly Mock<ISessionManager> _sessionManager = new();
    private readonly List<SessionInfo> _sessions = [];

    public SessionInfoWebSocketListenerTests()
    {
        _sessionManager.Setup(m => m.Sessions).Returns(_sessions);
        _sessionManager
            .Setup(m => m.ToSessionInfoDto(It.IsAny<SessionInfo>()))
            .Returns<SessionInfo>(s => new SessionInfoDto { Id = s.Id, UserId = s.UserId });
    }

    [Fact]
    public async Task Start_SendsCurrentListToNewConnectionOnly()
    {
        var user = CreateUser(isAdministrator: false);
        AddSession("own", user.Id);
        AddSession("other", CreateUser(isAdministrator: false).Id);
        var userConnection = new RecordingConnection(user);
        var adminConnection = new RecordingConnection(CreateUser(isAdministrator: true));

        var listener = await StartListenerAsync(userConnection, adminConnection);
        await listener.DisposeAsync();

        Assert.Equal([["own"]], userConnection.Sent);
        Assert.Equal([["own", "other"]], adminConnection.Sent);
    }

    [Fact]
    public async Task PlaybackProgress_OtherUsersSession_SkipsNonAdmin()
    {
        var user = CreateUser(isAdministrator: false);
        var other = CreateUser(isAdministrator: false);
        AddSession("own", user.Id);
        var otherSession = AddSession("other", other.Id);
        var userConnection = new RecordingConnection(user);
        var adminConnection = new RecordingConnection(CreateUser(isAdministrator: true));

        var listener = await StartListenerAsync(userConnection, adminConnection);
        RaisePlaybackProgress(otherSession);
        await listener.DisposeAsync();

        Assert.Equal([["own"]], userConnection.Sent);
        Assert.Equal([["own", "other"], ["own", "other"]], adminConnection.Sent);
    }

    [Fact]
    public async Task PlaybackProgress_OwnSession_SendsToNonAdmin()
    {
        var user = CreateUser(isAdministrator: false);
        var ownSession = AddSession("own", user.Id);
        AddSession("other", CreateUser(isAdministrator: false).Id);
        var userConnection = new RecordingConnection(user);

        var listener = await StartListenerAsync(userConnection);
        RaisePlaybackProgress(ownSession);
        await listener.DisposeAsync();

        Assert.Equal([["own"], ["own"]], userConnection.Sent);
    }

    [Fact]
    public async Task PlaybackProgress_UserRemovedFromSession_SendsOnceWithoutSession()
    {
        var user = CreateUser(isAdministrator: false);
        AddSession("own", user.Id);
        var sharedSession = AddSession("shared", CreateUser(isAdministrator: false).Id);
        sharedSession.AdditionalUsers = [new SessionUserInfo { UserId = user.Id }];
        var userConnection = new RecordingConnection(user);

        var listener = await StartListenerAsync(userConnection);
        RaisePlaybackProgress(sharedSession);
        await WaitForSentAsync(userConnection, 2);
        sharedSession.AdditionalUsers = [];
        RaisePlaybackProgress(sharedSession);
        await WaitForSentAsync(userConnection, 3);
        RaisePlaybackProgress(sharedSession);
        await listener.DisposeAsync();

        Assert.Equal([["own", "shared"], ["own", "shared"], ["own"]], userConnection.Sent);
    }

    private static User CreateUser(bool isAdministrator)
    {
        var user = new User("user", "auth-provider", "reset-provider") { Id = Guid.NewGuid() };
        user.AddDefaultPermissions();
        user.SetPermission(PermissionKind.IsAdministrator, isAdministrator);
        return user;
    }

    private static async Task WaitForSentAsync(RecordingConnection connection, int count)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(5));
        while (connection.Sent.Count < count)
        {
            await Task.Delay(10, cts.Token);
        }
    }

    private SessionInfo AddSession(string id, Guid userId)
    {
        var session = new SessionInfo(_sessionManager.Object, NullLogger.Instance) { Id = id, UserId = userId };
        _sessions.Add(session);
        return session;
    }

    private void RaisePlaybackProgress(SessionInfo session)
        => _sessionManager.Raise(m => m.PlaybackProgress += null, new PlaybackProgressEventArgs { Session = session });

    private async Task<SessionInfoWebSocketListener> StartListenerAsync(params RecordingConnection[] connections)
    {
        var listener = new SessionInfoWebSocketListener(NullLogger<SessionInfoWebSocketListener>.Instance, _sessionManager.Object);
        foreach (var connection in connections)
        {
            await listener.ProcessMessageAsync(new WebSocketMessageInfo
            {
                MessageType = SessionMessageType.SessionsStart,
                Data = "0,1000",
                Connection = connection
            });
        }

        return listener;
    }

    private sealed class RecordingConnection : IWebSocketConnection
    {
        public RecordingConnection(User user)
        {
            AuthorizationInfo = new AuthorizationInfo { User = user };
        }

        public event EventHandler<EventArgs>? Closed
        {
            add { }
            remove { }
        }

        public List<string?[]> Sent { get; } = [];

        public DateTime LastActivityDate => DateTime.UtcNow;

        public DateTime LastKeepAliveDate { get; set; }

        public Func<WebSocketMessageInfo, Task>? OnReceive { get; set; }

        public WebSocketState State => WebSocketState.Open;

        public AuthorizationInfo AuthorizationInfo { get; }

        public IPAddress? RemoteEndPoint => IPAddress.Loopback;

        public Task SendAsync(OutboundWebSocketMessage message, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task SendAsync<T>(OutboundWebSocketMessage<T> message, CancellationToken cancellationToken)
        {
            var sessions = Assert.IsAssignableFrom<IEnumerable<SessionInfoDto>>(message.Data);
            Sent.Add(sessions.Select(s => s.Id).ToArray());
            return Task.CompletedTask;
        }

        public Task ReceiveAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void ApplyRequestCulture()
        {
        }

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
