import { ref, type Ref } from 'vue';

/**
 * Collaborative editing session for a single Univer document.
 *
 * Transport: plain WebSocket (no extra npm dependency) speaking the JSON
 * protocol implemented by `WebApi/Hubs/CollabWebSocketEndpoint.cs`.
 *
 *   client -> server : join / presence / ingest / ping / leave
 *   server -> client : init / join / leave / presence / new_cs / cs_ack / cs_rej
 *
 * Note the old implementation used `univerAPI.createSocket()` from
 * `@univerjs/network`, which is (a) not installed in this project and (b) unable
 * to talk to an ASP.NET Core SignalR hub (SignalR requires its own handshake).
 */

export interface PresencePayload {
	/** Active worksheet id (Sheets only) */
	sheetId?: string;
	/** A1 notation of the current selection, e.g. "B3" or "B3:D5" */
	a1?: string;
	/** Short preview of the selected / edited content */
	text?: string;
	/** True while the user is typing inside a cell */
	editing?: boolean;
}

export interface CollaborationMember {
	userId: string;
	name: string;
	color: string;
	presence?: PresencePayload;
}

export interface MutationPayload {
	id: string;
	data: string;
}

export type CollaborationStatus = 'idle' | 'connecting' | 'online' | 'offline';

export interface CollaborationOptions {
	unitId: string;
	user: { id: string; name: string };
	token?: string;
	/** Base path of the collaboration endpoint (default `/ws/collab`) */
	path?: string;
	onInit?: (members: CollaborationMember[], revision: number) => void;
	onJoin?: (member: CollaborationMember) => void;
	onLeave?: (userId: string) => void;
	onPresence?: (userId: string, presence: PresencePayload) => void;
	onMutations?: (mutations: MutationPayload[], revision: number) => void;
	onStatusChange?: (status: CollaborationStatus) => void;
}

export interface CollaborationSession {
	status: Ref<CollaborationStatus>;
	baseRev: Ref<number>;
	connect: () => void;
	disconnect: () => void;
	sendPresence: (presence: PresencePayload) => void;
	/** Queue a local mutation — mutations are batched into one changeset */
	queueMutation: (mutation: { id: string; data: string }) => void;
}

/** Stable per-user colour palette — also used by the backend for new members */
export const PRESENCE_COLORS = [
	'#4285F4',
	'#EA4335',
	'#FBBC04',
	'#34A853',
	'#FF6D01',
	'#46BDC6',
	'#7B61FF',
	'#E91E63',
	'#00ACC1',
	'#8D6E63',
];

/** Deterministic colour for a user id (kept in sync with the C# implementation) */
export function colorForUser(userId: string): string {
	let hash = 0;
	for (let i = 0; i < userId.length; i++) {
		hash = (hash * 31 + userId.charCodeAt(i)) | 0;
	}
	return PRESENCE_COLORS[Math.abs(hash) % PRESENCE_COLORS.length];
}

const MUTATION_BATCH_DELAY = 80;
/** Ceiling for the batching window while the user keeps typing without a pause */
const MUTATION_MAX_DELAY = 400;
const MAX_RETRY_ATTEMPTS = 5;
const PING_INTERVAL = 25000;

export function useCollaboration(options: CollaborationOptions): CollaborationSession {
	const status = ref<CollaborationStatus>('idle');
	const baseRev = ref(0);

	let socket: WebSocket | null = null;
	let reconnectTimer: ReturnType<typeof setTimeout> | null = null;
	let pingTimer: ReturnType<typeof setInterval> | null = null;
	let reconnectAttempts = 0;
	let closedByUser = false;

	/** Mutations waiting to be sent as a single changeset */
	let pendingMutations: Array<{ id: string; data: string }> = [];
	let batchTimer: ReturnType<typeof setTimeout> | null = null;
	/** Guarantees a flush even while `batchTimer` keeps being pushed back */
	let batchMaxTimer: ReturnType<typeof setTimeout> | null = null;
	/** Last changeset that the server rejected — retried with a fresh baseRev */
	let rejectedBatch: {
		mutations: Array<{ id: string; data: string }>;
		attempts: number;
	} | null = null;
	let lastPresence: PresencePayload | null = null;

	function setStatus(next: CollaborationStatus) {
		if (status.value === next) return;
		status.value = next;
		options.onStatusChange?.(next);
	}

	function buildUrl() {
		const override = (import.meta.env.VITE_GLOB_COLLAB_URL as string | undefined)?.trim();
		const base = options.path ?? '/ws/collab';
		const path = `${base}/${encodeURIComponent(options.unitId)}`;
		const query = new URLSearchParams({
			userId: options.user.id,
			userName: options.user.name,
		});
		if (options.token) query.set('access_token', options.token);
		if (override) return `${override.replace(/\/$/, '')}${path}?${query.toString()}`;
		const scheme = window.location.protocol === 'https:' ? 'wss' : 'ws';
		return `${scheme}://${window.location.host}${path}?${query.toString()}`;
	}

	function send(payload: unknown) {
		if (socket && socket.readyState === WebSocket.OPEN) {
			socket.send(JSON.stringify(payload));
		}
	}

	function sendPresenceNow() {
		if (!lastPresence) return;
		send({ cmd: 'presence', presence: lastPresence });
	}

	function connect() {
		if (
			socket &&
			(socket.readyState === WebSocket.OPEN || socket.readyState === WebSocket.CONNECTING)
		) {
			return;
		}
		closedByUser = false;
		setStatus('connecting');

		let ws: WebSocket;
		try {
			ws = new WebSocket(buildUrl());
		} catch (error) {
			console.warn('[collab] unable to open socket', error);
			setStatus('offline');
			scheduleReconnect();
			return;
		}
		socket = ws;

		ws.onopen = () => {
			reconnectAttempts = 0;
			setStatus('online');
			send({
				cmd: 'join',
				unitId: options.unitId,
				userId: options.user.id,
				name: options.user.name,
			});
			// Re-publish the current cursor and anything typed while offline
			sendPresenceNow();
			if (pendingMutations.length > 0) flushMutations();
			if (pingTimer) clearInterval(pingTimer);
			pingTimer = setInterval(() => send({ cmd: 'ping' }), PING_INTERVAL);
		};

		ws.onmessage = (event: MessageEvent) => {
			let msg: any;
			try {
				msg = JSON.parse(event.data);
			} catch {
				return;
			}
			handleMessage(msg);
		};

		ws.onerror = () => {
			// `onclose` always follows, reconnection is handled there
		};

		ws.onclose = () => {
			if (pingTimer) clearInterval(pingTimer);
			pingTimer = null;
			socket = null;
			if (closedByUser) {
				setStatus('idle');
				return;
			}
			setStatus('offline');
			scheduleReconnect();
		};
	}

	function scheduleReconnect() {
		if (closedByUser || reconnectTimer) return;
		const delay = Math.min(1000 * 2 ** reconnectAttempts, 10000);
		reconnectAttempts += 1;
		reconnectTimer = setTimeout(() => {
			reconnectTimer = null;
			connect();
		}, delay);
	}

	function handleMessage(msg: any) {
		switch (msg?.eventID) {
			case 'init': {
				const members: CollaborationMember[] = (msg.members ?? []).map((m: any) => ({
					userId: m.userId,
					name: m.name ?? m.userId,
					color: m.color || colorForUser(m.userId),
					presence: m.presence ?? undefined,
				}));
				if (typeof msg.revision === 'number') baseRev.value = msg.revision;
				options.onInit?.(
					members.filter((m: CollaborationMember) => m.userId !== options.user.id),
					msg.revision ?? 0
				);
				break;
			}
			case 'join': {
				const m = msg.member ?? {};
				if (!m.userId || m.userId === options.user.id) break;
				options.onJoin?.({
					userId: m.userId,
					name: m.name ?? m.userId,
					color: m.color || colorForUser(m.userId),
					presence: m.presence ?? undefined,
				});
				break;
			}
			case 'leave': {
				if (!msg.memberID || msg.memberID === options.user.id) break;
				options.onLeave?.(msg.memberID);
				break;
			}
			case 'presence': {
				if (!msg.memberID || msg.memberID === options.user.id) break;
				options.onPresence?.(msg.memberID, msg.presence ?? {});
				break;
			}
			case 'new_cs': {
				const cs = msg.cs ?? {};
				const mutations: MutationPayload[] = cs.mutations ?? [];
				if (typeof cs.revision === 'number') baseRev.value = cs.revision;
				options.onMutations?.(mutations, cs.revision ?? baseRev.value);
				break;
			}
			case 'cs_ack': {
				if (typeof msg.cs?.revision === 'number') baseRev.value = msg.cs.revision;
				rejectedBatch = null;
				break;
			}
			case 'cs_rej': {
				// Revision mismatch — adopt the server revision and retry
				const serverRev = msg.cs?.revision;
				if (typeof serverRev === 'number') baseRev.value = serverRev;
				if (rejectedBatch && rejectedBatch.attempts < MAX_RETRY_ATTEMPTS) {
					rejectedBatch.attempts += 1;
					const retry = rejectedBatch;
					rejectedBatch = null;
					setTimeout(() => {
						send({
							cmd: 'ingest',
							cs: {
								unitId: options.unitId,
								userId: options.user.id,
								baseRev: baseRev.value,
								mutations: retry.mutations.map((m) => ({ id: m.id, data: m.data })),
							},
						});
						rejectedBatch = retry;
					}, 150);
				} else {
					rejectedBatch = null;
					console.warn('[collab] changeset rejected, giving up after retries');
				}
				break;
			}
			case 'error': {
				console.warn('[collab] server error:', msg.message);
				break;
			}
			default:
				break;
		}
	}

	function flushMutations() {
		if (batchTimer) {
			clearTimeout(batchTimer);
			batchTimer = null;
		}
		if (batchMaxTimer) {
			clearTimeout(batchMaxTimer);
			batchMaxTimer = null;
		}
		if (pendingMutations.length === 0) return;
		const mutations = pendingMutations;
		pendingMutations = [];
		const payload = {
			cmd: 'ingest',
			cs: {
				unitId: options.unitId,
				userId: options.user.id,
				baseRev: baseRev.value,
				mutations: mutations.map((m) => ({ id: m.id, data: m.data })),
			},
		};
		rejectedBatch = { mutations, attempts: 0 };
		if (socket && socket.readyState === WebSocket.OPEN) {
			send(payload);
		} else {
			// Offline: keep the changes so they are sent once reconnected
			pendingMutations = [...mutations, ...pendingMutations];
			scheduleReconnect();
		}
	}

	function queueMutation(mutation: { id: string; data: string }) {
		pendingMutations.push(mutation);
		// Throttle, not debounce: a user who types without pausing used to push the
		// flush back on every keystroke, so nothing was sent until they stopped.
		if (batchTimer) return;
		batchTimer = setTimeout(flushMutations, MUTATION_BATCH_DELAY);
		if (!batchMaxTimer) batchMaxTimer = setTimeout(flushMutations, MUTATION_MAX_DELAY);
	}

	function sendPresence(presence: PresencePayload) {
		lastPresence = { ...presence };
		sendPresenceNow();
	}

	function disconnect() {
		closedByUser = true;
		if (reconnectTimer) clearTimeout(reconnectTimer);
		reconnectTimer = null;
		if (batchTimer) clearTimeout(batchTimer);
		batchTimer = null;
		if (batchMaxTimer) clearTimeout(batchMaxTimer);
		batchMaxTimer = null;
		if (pingTimer) clearInterval(pingTimer);
		pingTimer = null;
		if (socket) {
			if (socket.readyState === WebSocket.OPEN) send({ cmd: 'leave' });
			try {
				socket.close();
			} catch {
				// ignore
			}
			socket = null;
		}
		setStatus('idle');
	}

	return { status, baseRev, connect, disconnect, sendPresence, queueMutation };
}
