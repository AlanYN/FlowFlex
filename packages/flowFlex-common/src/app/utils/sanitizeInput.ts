import DOMPurify from 'dompurify';

export function sanitizeInput(input: string): string {
	return DOMPurify.sanitize(input, {
		ALLOWED_TAGS: ['b', 'i', 'code'],
		FORBID_ATTR: ['style', 'class'],
	});
}
