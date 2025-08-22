export interface Checklist {
	id: string;
	name: string;
	description?: string;
	team: string;
	workflowId?: string;
	stageId?: string;
	totalTasks?: number;
	modifyDate?: string;
	createDate?: string;
	createdAt?: string;
	type?: string;
	status?: string;
	isTemplate?: boolean;
	isActive?: boolean;
	modifyBy?: string;
	assignments?: any[];
}
