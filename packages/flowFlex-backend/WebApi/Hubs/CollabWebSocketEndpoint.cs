using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FlowFlex.WebApi.Hubs
{
    /// <summary>
    /// Raw WebSocket endpoint used by the web client for real-time collaboration
    /// (presence + changesets). A plain WebSocket is used because the browser client
    /// cannot perform the SignalR handshake without an extra npm dependency.
    ///
    /// Client -&gt; server : join / presence / ingest / ping / leave
    /// Server -&gt; client : init / join / leave / presence / new_cs / cs_ack / cs_rej / pong
    ///
    /// Identity travels in the query string (userId / userName); an access_token may be
    /// supplied for validation. TODO: derive the identity from the token and reject
    /// requests without a valid one.
    /// </summary>
    public static class CollabWebSocketEndpoint
    {
        private const int ReceiveBufferSize = 32 * 1024;
        private static readonly TimeSpan IdleTimeout = TimeSpan.FromSeconds(90);

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static async Task HandleAsync(
            HttpContext context,
            string unitId,
            CollabRoomRegistry registry,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("A WebSocket connection is required.");
                return;
            }

            if (string.IsNullOrWhiteSpace(unitId))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var userId = context.Request.Query["userId"].ToString();
            var userName = context.Request.Query["userName"].ToString();
            if (string.IsNullOrWhiteSpace(userId))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("userId is required.");
                return;
            }
            if (string.IsNullOrWhiteSpace(userName)) userName = userId;

            var validatedUser = await TryGetAuthenticatedUserIdAsync(context);
            if (validatedUser == null)
            {
                logger.LogWarning("[collab] no valid access token for user {UserId} on document {UnitId}", userId, unitId);
            }

            WebSocket socket;
            try
            {
                socket = await context.WebSockets.AcceptWebSocketAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[collab] failed to accept the WebSocket for {UnitId}", unitId);
                return;
            }

            using (socket)
            {
                var connection = new WebSocketConnection(socket, userId, logger);
                await registry.JoinAsync(unitId, userId, userName, connection, cancellationToken);
                logger.LogInformation("[collab] {UserId} joined {UnitId}", userId, unitId);

                try
                {
                    await ReceiveLoopAsync(socket, connection, unitId, userId, registry, logger, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // client disconnected or the request was aborted
                }
                catch (WebSocketException ex)
                {
                    logger.LogInformation("[collab] socket closed for {UserId} on {UnitId}: {Message}", userId, unitId, ex.Message);
                }
                finally
                {
                    await registry.LeaveAsync(unitId, connection.Id, CancellationToken.None);
                    logger.LogInformation("[collab] {UserId} left {UnitId}", userId, unitId);

                    if (socket.State == WebSocketState.Open)
                    {
                        try
                        {
                            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
                        }
                        catch (Exception)
                        {
                            // already gone
                        }
                    }
                }
            }
        }

        private static async Task ReceiveLoopAsync(
            WebSocket socket,
            WebSocketConnection connection,
            string unitId,
            string userId,
            CollabRoomRegistry registry,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            var buffer = new byte[ReceiveBufferSize];
            using var message = new MemoryStream();

            while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                using var idle = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                idle.CancelAfter(IdleTimeout);

                WebSocketReceiveResult result;
                try
                {
                    result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), idle.Token);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    logger.LogInformation("[collab] idle timeout for {UserId} on {UnitId}", userId, unitId);
                    return;
                }

                if (result.MessageType == WebSocketMessageType.Close) return;

                message.Write(buffer, 0, result.Count);
                if (!result.EndOfMessage) continue;

                var json = Encoding.UTF8.GetString(message.ToArray());
                message.SetLength(0);

                if (!await HandleMessageAsync(json, connection, unitId, userId, registry, logger, cancellationToken))
                {
                    return;
                }
            }
        }

        private static async Task<bool> HandleMessageAsync(
            string json,
            WebSocketConnection connection,
            string unitId,
            string userId,
            CollabRoomRegistry registry,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            InboundMessage message;
            try
            {
                message = JsonSerializer.Deserialize<InboundMessage>(json, JsonOptions);
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "[collab] malformed message from {UserId}", userId);
                return true;
            }

            if (message == null) return true;

            switch (message.Cmd)
            {
                case "presence":
                    await registry.PresenceAsync(unitId, userId, message.Presence, connection.Id, cancellationToken);
                    break;

                case "ingest":
                    if (message.Cs != null)
                    {
                        await registry.IngestAsync(
                            unitId,
                            userId,
                            message.Cs.BaseRev,
                            message.Cs.Mutations ?? new List<CollabMutationPayload>(),
                            connection.Id,
                            cancellationToken);
                    }
                    break;

                case "ping":
                    await connection.SendAsync(new { eventID = "pong" }, cancellationToken);
                    break;

                case "join":
                    // Identity already came from the query string
                    break;

                case "leave":
                    return false;

                default:
                    logger.LogDebug("[collab] ignoring unknown command {Cmd}", message.Cmd);
                    break;
            }

            return true;
        }

        private static async Task<string?> TryGetAuthenticatedUserIdAsync(HttpContext context)
        {
            var token = context.Request.Query["access_token"].ToString();
            if (string.IsNullOrWhiteSpace(token)) return null;

            context.Request.Headers.Authorization = $"Bearer {token}";
            try
            {
                var result = await context.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
                if (!result.Succeeded || result.Principal == null) return null;
                return result.Principal.FindFirst("sub")?.Value
                    ?? result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }
            catch
            {
                return null;
            }
        }

        private sealed class InboundMessage
        {
            [JsonPropertyName("cmd")]
            public string Cmd { get; set; }

            [JsonPropertyName("presence")]
            public JsonElement? Presence { get; set; }

            [JsonPropertyName("cs")]
            public InboundChangeset Cs { get; set; }
        }

        private sealed class InboundChangeset
        {
            [JsonPropertyName("baseRev")]
            public int BaseRev { get; set; }

            [JsonPropertyName("mutations")]
            public List<CollabMutationPayload> Mutations { get; set; }
        }

        private sealed class WebSocketConnection : ICollabConnection
        {
            private readonly WebSocket _socket;
            private readonly ILogger _logger;
            private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);

            public WebSocketConnection(WebSocket socket, string userId, ILogger logger)
            {
                _socket = socket;
                UserId = userId;
                _logger = logger;
            }

            public Guid Id { get; } = Guid.NewGuid();

            public string UserId { get; }

            public async Task SendAsync(object payload, CancellationToken cancellationToken)
            {
                if (_socket.State != WebSocketState.Open) return;

                var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, JsonOptions));
                await _sendLock.WaitAsync(cancellationToken);
                try
                {
                    if (_socket.State != WebSocketState.Open) return;
                    await _socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "[collab] send failed for {UserId}", UserId);
                }
                finally
                {
                    _sendLock.Release();
                }
            }
        }
    }
}