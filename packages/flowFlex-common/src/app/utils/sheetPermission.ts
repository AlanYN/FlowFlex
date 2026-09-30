/** Avatar colors for collaborators */
export const AVATAR_COLORS = [
	'#C53030',
	'#2C7A7B',
	'#2B6CB0',
	'#38A169',
	'#D69E2E',
	'#9F7AEA',
	'#319795',
	'#805AD5',
	'#3182CE',
	'#DD6B20',
];

/** Generate deterministic avatar color from a string */
export function avatarColor(name: string): string {
	if (!name) return AVATAR_COLORS[0];
	let h = 0;
	for (let i = 0; i < name.length; i++) h = name.charCodeAt(i) + ((h << 5) - h);
	return AVATAR_COLORS[Math.abs(h) % AVATAR_COLORS.length];
}

/** Strip Univer role prefix ("Owner-", "Editor-", "Reader-") from a userID */
export function stripRolePrefix(uid: string): string {
	const PREFIXES = new Set(['Owner', 'Editor', 'Reader']);
	const dashIdx = uid.indexOf('-');
	if (dashIdx > 0 && PREFIXES.has(uid.slice(0, dashIdx))) {
		return uid.slice(dashIdx + 1);
	}
	return uid;
}

/** Extract __creator:{userId} tag from a rule's name or description field */
export function extractCreatorTag(rule: { name?: string; description?: string }): string | null {
	const nameTag = String(rule.name ?? '').match(/__creator:([^\s\u200B]+)/);
	if (nameTag) return nameTag[1];
	const descTag = String(rule.description ?? '').match(/__creator:([^\s]+)/);
	return descTag ? descTag[1] : null;
}

/** Generate an SVG data URI avatar with the user's initial */
export function makeAvatarDataUri(name: string): string {
	const initial = (name || '?')[0].toUpperCase();
	const color = avatarColor(name);
	const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24"><circle cx="12" cy="12" r="12" fill="${color}"/><text x="12" y="17" text-anchor="middle" fill="white" font-size="13" font-family="sans-serif">${initial}</text></svg>`;
	// Use encodeURIComponent instead of btoa — btoa fails on non-Latin1 chars (e.g. Chinese names)
	return `data:image/svg+xml;charset=utf-8,${encodeURIComponent(svg)}`;
}
