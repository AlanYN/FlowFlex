import { MessageFolder, MessageType, MessageTag } from '@/enums/appEnum';

export interface ListApiResponse<T> {
	data: {
		totalPage: number;
		pageCount: number;
		pageIndex: number;
		pageSize: number;
		total: number;
		dataCount: number;
		data: T;
	};
	success: boolean;
	msg: string;
	code: string;
}

export interface ListApiParams {
	folder?: MessageFolder;
	label?: MessageTag;
	messageType?: MessageType;
	searchTerm?: string;
	relatedEntityId?: string;
	pageIndex?: number;
	pageSize?: number;
	sortField?: string;
	sortDirection?: string;
}

export interface ApiResponse<T> {
	data: T;
	success: boolean;
	msg: string;
	code: string;
}

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
	type: MessageFolder;
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

export interface MessageList {
	id: string;
	subject: string;
	bodyPreview: string;
	senderName: string;
	senderEmail: string;
	messageType: MessageFolder;
	labels: MessageTag[];
	isRead: boolean;
	isStarred: boolean;
	hasAttachments: boolean;
	receivedDate: string;
	sentDate: string;
	isRead: boolean;
}

export interface MessageInfo {
	id: string;
	subject: string;
	body: string;
	bodyPreview: string;
	senderName: string;
	senderEmail: string;
	senderId: string;
	messageType: MessageType;
	folder: MessageFolder;
	labels: MessageTag[];
	isArchived: boolean;
	recipients: {
		userId: string;
		name: string;
		email: string;
	}[];
	ccRecipients: [];
	bccRecipients: [];
	isRead: boolean;
	isStarred: boolean;
	isDraft: boolean;
	hasAttachments: boolean;
	attachments: {
		id: string;
		fileName: string;
		fileSize: number;
		contentType: string;
		storageUrl: string;
	}[];
	parentMessageId: null;
	conversationId: string;
	relatedEntityType: string;
	relatedEntityId: number;
	relatedEntityCode: string;
	receivedDate: string;
	sentDate: string;
}

export interface MessageCenterForm {
	subject: string;
	body: string;
	messageType?: string;
	recipients: {
		userId: string;
		name: string;
		email: string;
	}[];
	ccRecipients: [];
	bccRecipients: [];
	labels: MessageTag[];
	relatedEntityType: string;
	relatedEntityId: number;
	relatedEntityCode: string;
	portalId: null;
	attachments: any[];
	attachmentIds?: string[];
}
