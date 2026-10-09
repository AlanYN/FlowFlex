/**
 * Deterministic Univer ids.
 *
 * Univer invents an id whenever a snapshot arrives without one:
 * `getEmptySnapshot()` returns `{ id: '', sheets: {} }`, `Workbook` then does
 * `id = generateRandomId(6)`, and `Worksheet` does `_sheetId = snapshot.id ??
 * generateRandomId(6)` (see `@univerjs/core`). Backend documents are created with
 * `dataJson = "{}"`, so **every tab of the same document ended up with a
 * different workbook id and a different worksheet id**.
 *
 * Collaboration payloads carry those ids, which is why nothing worked between
 * two peers:
 *
 * - mutation params (`unitId` / `subUnitId`) — a peer that does not own the id
 *   drops every remote change, so both sides silently diverge;
 * - presence (`sheetId`) — never equals the receiver's active sheet id, so the
 *   remote selection highlight and the name tag are never drawn.
 *
 * Deriving the ids from the document `unitId` makes every peer (and every later
 * reload / save) agree. A snapshot that already carries an id keeps it, so
 * documents saved before this change stay stable.
 */

const ID_PREFIX = 'ffid';

/** FNV-1a (32 bit) → base36 — deterministic, short, safe inside an id */
function hashSeed(seed: string): string {
	let hash = 0x811c9dc5;
	for (let i = 0; i < seed.length; i++) {
		hash = Math.imul(hash ^ seed.charCodeAt(i), 0x01000193) >>> 0;
	}
	return hash.toString(36);
}

/** A stable Univer id derived from a seed (document unitId, worksheet name, …) */
export function stableUniverId(seed: string, kind: string): string {
	return `${ID_PREFIX}${hashSeed(`${kind}:${seed}`)}${hashSeed(`${kind}:${seed}:salt`)}`;
}

function toRecord(value: unknown): Record<string, any> {
	return value && typeof value === 'object' ? { ...(value as Record<string, any>) } : {};
}

function usableId(value: unknown): string | null {
	return typeof value === 'string' && value.length > 0 ? value : null;
}

/**
 * Normalise a workbook snapshot so all peers use the same workbook id and the
 * same worksheet ids. The input is never mutated (callers may hold a `markRaw`
 * store object).
 */
export function normalizeWorkbookSnapshot(
	workbookData: unknown,
	docId?: string
): Record<string, any> {
	const snapshot = toRecord(workbookData);
	const workbookId = usableId(snapshot.id) ?? (docId ? stableUniverId(docId, 'workbook') : null);
	if (workbookId) snapshot.id = workbookId;

	const sheets = toRecord(snapshot.sheets);
	const sheetIds = Object.keys(sheets);
	const sheetOrder: string[] = Array.isArray(snapshot.sheetOrder)
		? snapshot.sheetOrder.filter((id: unknown): id is string => typeof id === 'string')
		: [];

	if (sheetIds.length === 0) {
		// An empty workbook would otherwise get a random first sheet in every tab.
		const sheetId = stableUniverId(docId || workbookId || 'default', 'sheet');
		sheets[sheetId] = { id: sheetId, name: 'Sheet1' };
		sheetIds.push(sheetId);
		sheetOrder.length = 0;
	} else {
		for (const key of sheetIds) {
			const sheet = toRecord(sheets[key]);
			// `Worksheet` takes its id from `snapshot.id` while the workbook map is
			// keyed by the object key — the two must agree.
			sheet.id = usableId(sheet.id) ?? key;
			sheets[key] = sheet;
		}
	}

	for (const key of sheetIds) if (!sheetOrder.includes(key)) sheetOrder.push(key);
	snapshot.sheets = sheets;
	snapshot.sheetOrder = sheetOrder;
	return snapshot;
}

/**
 * Normalise a document / slide snapshot. Only the unit id matters today, but
 * Docs and Slides use the same "id or random" rule as the workbook.
 */
export function normalizeUnitSnapshot(data: unknown, docId?: string): Record<string, any> {
	const snapshot = toRecord(data);
	const unitId = usableId(snapshot.id) ?? (docId ? stableUniverId(docId, 'unit') : null);
	if (unitId) snapshot.id = unitId;
	return snapshot;
}
