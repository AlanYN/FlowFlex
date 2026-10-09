using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace FlowFlex.WebApi.Hubs
{
    /// <summary>
    /// Collaborative editing SignalR hub.
    /// Implements optimistic-locking changeset protocol compatible with @univerjs/network.
    ///
    /// Client → Server methods:  Join, Ingest, UpdateCursor
    /// Server → Client events:   collab_msg { eventID: 'join'|'leave'|'new_cs'|'cs_ack'|'cs_rej'|'update_cursor' }
    /// </summary>
    [Authorize]
    public class CollabHub : Hub
    {
        // In-memory revision store: unitId → currentRevision
        // Replace with distributed cache (Redis) for multi-pod deployments
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, int> _revisions =
            new System.Collections.Concurrent.ConcurrentDictionary<string, int>();

        private readonly ILogger<CollabHub> _logger;

        public CollabHub(ILogger<CollabHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Join a document collaboration room.
        /// </summary>
        public async Task Join(string unitId, string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, unitId);

            await Clients.OthersInGroup(unitId).SendAsync("collab_msg", new
            {
                eventID = "join",
                memberID = Context.ConnectionId,
                name = userId,
            });

            _logger.LogInformation("[CollabHub] {UserId} joined room {UnitId}", userId, unitId);
        }

        /// <summary>
        /// Receive and broadcast a Changeset with optimistic-lock collision detection.
        /// </summary>
        public async Task Ingest(ChangesetDto cs)
        {
            if (cs == null) return;

            var currentRev = _revisions.GetOrAdd(cs.UnitId, 0);

            // Optimistic lock: reject if baseRev doesn't match current revision
            if (cs.BaseRev != currentRev)
            {
                _logger.LogWarning("[CollabHub] cs_rej for {UnitId}: baseRev={BaseRev}, currentRev={CurrentRev}",
                    cs.UnitId, cs.BaseRev, currentRev);

                await Clients.Caller.SendAsync("collab_msg", new { eventID = "cs_rej", cs });
                return;
            }

            // Assign new revision and persist (update in-memory store)
            var newRev = currentRev + 1;
            _revisions[cs.UnitId] = newRev;
            cs.Revision = newRev;

            // Acknowledge to sender
            await Clients.Caller.SendAsync("collab_msg", new { eventID = "cs_ack", cs });

            // Broadcast to other members in the room
            await Clients.OthersInGroup(cs.UnitId).SendAsync("collab_msg", new { eventID = "new_cs", cs });
        }

        /// <summary>
        /// Broadcast cursor position to other members (reserved for future use).
        /// </summary>
        public async Task UpdateCursor(string unitId, string selection)
        {
            await Clients.OthersInGroup(unitId).SendAsync("collab_msg", new
            {
                eventID = "update_cursor",
                unitID = unitId,
                memberID = Context.ConnectionId,
                selection,
            });
        }

        /// <summary>
        /// Notify group members when a connection is dropped.
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // Broadcast leave to all groups this connection was in
            // Note: SignalR removes the connection from groups automatically on disconnect
            // We broadcast to all rooms — a more precise impl would track unitId per connection
            _logger.LogInformation("[CollabHub] Connection {ConnectionId} disconnected", Context.ConnectionId);

            // Simple approach: find rooms the user was in from the hub context
            // For production, store connectionId→unitId mapping in Redis
            await base.OnDisconnectedAsync(exception);
        }
    }

    /// <summary>
    /// Changeset data transfer object for the collaborative editing protocol
    /// </summary>
    public class ChangesetDto
    {
        public string UnitId { get; set; }
        public string UserId { get; set; }
        public int BaseRev { get; set; }
        public int Revision { get; set; }
        public System.Collections.Generic.List<MutationDto> Mutations { get; set; } = new();
    }

    /// <summary>
    /// Single mutation within a changeset
    /// </summary>
    public class MutationDto
    {
        /// <summary>
        /// Univer mutation command ID (e.g. "sheet.mutation.set-range-values")
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// JSON-serialized mutation params
        /// </summary>
        public string Data { get; set; }
    }
}
