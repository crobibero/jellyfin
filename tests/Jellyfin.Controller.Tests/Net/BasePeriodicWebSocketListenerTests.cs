using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Net.WebSocketMessages;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Controller.Tests.Net
{
    public class BasePeriodicWebSocketListenerTests
    {
        [Fact]
        public async Task SendData_Filter_SendsToMatchingConnections()
        {
            var included = new RecordingConnection();
            var excluded = new RecordingConnection();
            var listener = new TestListener();
            listener.Subscribe(included);
            listener.Subscribe(excluded);

            listener.Trigger((connection, _) => connection == included);
            await listener.DisposeAsync();

            Assert.Equal(["data"], included.Sent);
            Assert.Empty(excluded.Sent);
        }

        [Fact]
        public async Task SendData_NoFilter_SendsToAllConnections()
        {
            var first = new RecordingConnection();
            var second = new RecordingConnection();
            var listener = new TestListener();
            listener.Subscribe(first);
            listener.Subscribe(second);

            listener.Trigger(null);
            await listener.DisposeAsync();

            Assert.Equal(["data"], first.Sent);
            Assert.Equal(["data"], second.Sent);
        }

        [Fact]
        public async Task OnDataSent_FailedSend_NotCalled()
        {
            var connection = new RecordingConnection { FailuresLeft = 1 };
            var listener = new TestListener();
            listener.Subscribe(connection);

            listener.Trigger(null);
            listener.Trigger(null);
            await listener.DisposeAsync();

            Assert.Equal(["data"], connection.Sent);
            Assert.Equal(1, listener.DataSentCount);
        }

        private sealed class TestListener : BasePeriodicWebSocketListener<string, WebSocketListenerState>
        {
            public TestListener()
                : base(NullLogger<BasePeriodicWebSocketListener<string, WebSocketListenerState>>.Instance)
            {
            }

            public int DataSentCount { get; private set; }

            protected override SessionMessageType Type => SessionMessageType.Sessions;

            protected override SessionMessageType StartType => SessionMessageType.SessionsStart;

            protected override SessionMessageType StopType => SessionMessageType.SessionsStop;

            public void Subscribe(IWebSocketConnection connection)
                => Start(new WebSocketMessageInfo { MessageType = StartType, Data = "0,1000", Connection = connection });

            public void Trigger(Func<IWebSocketConnection, WebSocketListenerState, bool>? filter)
            {
                if (filter is null)
                {
                    SendData(true);
                }
                else
                {
                    SendData(true, filter);
                }
            }

            protected override Task<string> GetDataToSend() => Task.FromResult("data");

            protected override void OnDataSent(IWebSocketConnection connection, WebSocketListenerState state, string data) => DataSentCount++;
        }

        private sealed class RecordingConnection : IWebSocketConnection
        {
            public event EventHandler<EventArgs>? Closed
            {
                add { }
                remove { }
            }

            public int FailuresLeft { get; set; }

            public List<string?> Sent { get; } = [];

            public DateTime LastActivityDate => DateTime.UtcNow;

            public DateTime LastKeepAliveDate { get; set; }

            public Func<WebSocketMessageInfo, Task>? OnReceive { get; set; }

            public WebSocketState State => WebSocketState.Open;

            public AuthorizationInfo AuthorizationInfo { get; } = new();

            public IPAddress? RemoteEndPoint => IPAddress.Loopback;

            public Task SendAsync(OutboundWebSocketMessage message, CancellationToken cancellationToken)
                => throw new NotSupportedException();

            public Task SendAsync<T>(OutboundWebSocketMessage<T> message, CancellationToken cancellationToken)
            {
                if (FailuresLeft > 0)
                {
                    FailuresLeft--;
                    throw new OperationCanceledException();
                }

                Sent.Add(message.Data?.ToString());
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
}
