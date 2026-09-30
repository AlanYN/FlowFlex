using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FlowFlex.WebApi.Hubs
{
    /// <summary>
    /// A single connection taking part in a collaboration room.
    /// Implemented by the raw WebSocket endpoint (and reusable by the SignalR hub).
    /// </summary>
    public interface ICollabConnection
    {
        Guid Id { get; }

        string UserId { get; }

        Task SendAsync(object payload, CancellationToken cancellationToken);
    }

    /// <summary>
    /// In-memory collaboration state: member roster, live presence, latest changeset
    /// revision (optimistic locking) and the fan-out to connected members.
    ///
    /// NOTE: single-instance state — replace with Redis (or sticky sessions) when the
    /// API runs on more than one pod.
    /// </summary>
    public sealed class CollabRoomRegistry
    {
        /// <summary>Palette shared with the frontend (see useCollaboration.ts)</summary>
        private static readonly string[] Palette =
        {
            "#4285F4", "#EA4335", "#FBBC04", "#34A853", "#FF6D01",
            "#46BDC6", "#7B61FF", "#E91E63", "#00ACC1", "#8D6E63"
        };

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public sealed class Member
        {
            public string UserId { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Color { get; set; } = Palette[0];
            /// <summary>Number of open connections for this user</summary>
            public int Connections { get; set; }
            /// <summary>Last presence payload reported by this user</summary>
            public JsonElement? Presence { get; set; }
        }

        private sealed class Room
        {
            public readonly object Gate = new object();
            public readonly Dictionary<string, Member> Members = new Dictionary<string, Member>(StringComparer.Ordinal);
            public readonly Dictionary<Guid, ICollabConnection> Connections = new Dictionary<Guid, ICollabConnection>();
            public int Revision;
        }

        private readonly ConcurrentDictionary<string, Room> _rooms = new ConcurrentDictionary<string, Room>();

        /// <summary>Deterministic colour for a user id (mirrors the frontend implementation)</summary>
        public static string ColorFor(string userId)
        {
            var hash = 0;
            foreach (var ch in userId ?? string.Empty)
            {
                hash = unchecked(hash * 31 + ch);
            }
            return Palette[(hash & int.MaxValue) % Palette.Length];
        }

        /// <summary>
        /// Register the connection, send the roster back to it and announce the
        /// new member to everybody else. Returns the current revision.
        /// </summary>
        public async Task<int> JoinAsync(string unitId, string userId, string name, ICollabConnection connection, CancellationToken cancellationToken)
        {
            var room = _rooms.GetOrAdd(unitId, _ => new Room());
            int revision;
            object self;
            List<object> members;
            List<ICollabConnection> others;

            lock (room.Gate)
            {
                // Re-attach if the room was swept between GetOrAdd and acquiring the lock
                if (!_rooms.ContainsKey(unitId)) _rooms[unitId] = room;

                if (!room.Members.TryGetValue(userId, out var member))
                {
                    member = new Member
                    {
                        UserId = userId,
                        Name = string.IsNullOrWhiteSpace(name) ? userId : name,
                        Color = ColorFor(userId)
                    };
                    room.Members[userId] = member;
                }
                else if (!string.IsNullOrWhiteSpace(name))
                {
                    member.Name = name;
                }

                member.Connections++;
                room.Connections[connection.Id] = connection;
                revision = room.Revision;
                self = Snapshot(member);
                members = room.Members.Values.Select(Snapshot).ToList();
                others = room.Connections.Where(kv => kv.Key != connection.Id).Select(kv => kv.Value).ToList();
            }

            await connection.SendAsync(new { eventID = "init", unitId, revision, self, members }, cancellationToken);
            await BroadcastAsync(others, new { eventID = "join", member = self }, cancellationToken);
            return revision;
        }

        /// <summary>Drop a connection; returns true when the user left the room entirely</summary>
        public async Task<bool> LeaveAsync(string unitId, Guid connectionId, CancellationToken cancellationToken)
        {
            if (!_rooms.TryGetValue(unitId, out var room)) return false;

            string userId;
            bool fullyLeft = false;
            List<ICollabConnection> remaining;

            lock (room.Gate)
            {
                if (!room.Connections.TryGetValue(connectionId, out var connection)) return false;
                userId = connection.UserId;
                room.Connections.Remove(connectionId);

                if (room.Members.TryGetValue(userId, out var member))
                {
                    member.Connections--;
                    member.Presence = null;
                    if (member.Connections <= 0)
                    {
                        room.Members.Remove(userId);
                        fullyLeft = true;
                    }
                }

                remaining = room.Connections.Values.ToList();
                if (room.Members.Count == 0 && room.Connections.Count == 0)
                {
                    _rooms.TryRemove(unitId, out _);
                }
            }

            if (fullyLeft)
            {
                await BroadcastAsync(remaining, new { eventID = "leave", memberID = userId }, cancellationToken);
            }
            return fullyLeft;
        }

        /// <summary>Relay the presence (cursor / selection / editing state) of one member</summary>
        public async Task PresenceAsync(string unitId, string userId, JsonElement? presence, Guid connectionId, CancellationToken cancellationToken)
        {
            if (!_rooms.TryGetValue(unitId, out var room)) return;

            List<ICollabConnection> others;
            lock (room.Gate)
            {
                if (room.Members.TryGetValue(userId, out var member)) member.Presence = presence;
                others = room.Connections.Where(kv => kv.Key != connectionId).Select(kv => kv.Value).ToList();
            }

            await BroadcastAsync(others, new { eventID = "presence", memberID = userId, presence }, cancellationToken);
        }

        /// <summary>
        /// Optimistic-lock ingest: the changeset is accepted only when baseRev matches
        /// the room revision. Returns the resulting revision.
        /// </summary>
        public async Task<int> IngestAsync(
            string unitId,
            string userId,
            int baseRev,
            IReadOnlyList<CollabMutationPayload> mutations,
            Guid connectionId,
            CancellationToken cancellationToken)
        {
            var room = _rooms.GetOrAdd(unitId, _ => new Room());
            bool accepted;
            int currentRevision;
            int newRevision;
            List<ICollabConnection> all;
            List<ICollabConnection> others;

            lock (room.Gate)
            {
                if (!_rooms.ContainsKey(unitId)) _rooms[unitId] = room;
                currentRevision = room.Revision;
                accepted = baseRev == currentRevision;
                if (accepted) room.Revision++;
                newRevision = room.Revision;
                all = room.Connections.Values.ToList();
                others = room.Connections.Where(kv => kv.Key != connectionId).Select(kv => kv.Value).ToList();
            }

            var mutationsPayload = mutations.Select(m => new { id = m.Id, data = m.Data }).ToList();

            if (!accepted)
            {
                await BroadcastAsync(
                    all.Where(c => c.Id == connectionId).ToList(),
                    new { eventID = "cs_rej", cs = new { revision = currentRevision, mutations = mutationsPayload } },
                    cancellationToken);
                return currentRevision;
            }

            var payload = new { revision = newRevision, mutations = mutationsPayload };
            await BroadcastAsync(
                all.Where(c => c.Id == connectionId).ToList(),
                new { eventID = "cs_ack", cs = payload },
                cancellationToken);
            await BroadcastAsync(others, new { eventID = "new_cs", cs = payload }, cancellationToken);
            return newRevision;
        }

        private static object Snapshot(Member member) => new
        {
            userId = member.UserId,
            name = member.Name,
            color = member.Color,
            presence = member.Presence
        };

        private static async Task BroadcastAsync(IReadOnlyList<ICollabConnection> targets, object payload, CancellationToken cancellationToken)
        {
            foreach (var target in targets)
            {
                try
                {
                    await target.SendAsync(payload, cancellationToken);
                }
                catch (Exception)
                {
                    // A broken connection is cleaned up by its own receive loop
                }
            }
        }

        private static async Task BroadcastAsync(IEnumerable<ICollabConnection> targets, object payload, CancellationToken cancellationToken)
        {
            await BroadcastAsync(targets.ToList(), payload, cancellationToken);
        }
    }

    /// <summary>Single mutation inside a changeset (matches the frontend protocol)</summary>
    public sealed class CollabMutationPayload
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("data")]
        public string Data { get; set; } = string.Empty;
    }
}