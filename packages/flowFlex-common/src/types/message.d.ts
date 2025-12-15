import { Message } from '@/enums/appEnum';

export interface MessageParticipant {
	id: string;
	name: string;
	email: string;
	avatar?: string;
}

export interface MessageLabel {
	id: string;
	name: string;
	color: string;
	type: Message;
}

export interface RelatedEntity {
	id: string;
	type: string;
	displayName: string;
}

export interface Attachment {
	id: string;
	filename: string;
	size: string;
	mimeType: string;
	url: string;
}

export interface MessageInfo {
	id: string;
	subject: string;
	from: MessageParticipant;
	to: MessageParticipant[];
	cc?: MessageParticipant[];
	bcc?: MessageParticipant[];
	body: string;
	bodyHtml?: string;
	timestamp: string | Date;
	labels: MessageLabel[];
	relatedEntity?: RelatedEntity;
	attachments: Attachment[];
	isStarred: boolean;
	isRead: boolean;
	threadId?: string;
}
