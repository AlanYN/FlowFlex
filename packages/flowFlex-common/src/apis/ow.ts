import { defHttp } from '@/app/apis/axios';

// Types
export interface Workflow {
  id?: number;
  name: string;
  description: string;
  isActive: boolean;
  status?: string;
  startDate?: string;
  stages?: WorkflowStage[];
  createdAt?: string;
}

export interface WorkflowStage {
  id?: number;
  name: string;
  description: string;
  order: number;
  defaultAssignedGroup?: string;
  estimatedDuration: number;
  isActive?: boolean;
  workflowVersion?: string;
}

export interface ApiResponse<T = any> {
  success: boolean;
  data: T;
  message?: string;
}

// API Functions
export const getWorkflowList = (): Promise<ApiResponse<Workflow[]>> => {
  return defHttp.get({
    url: '/api/workflow/list'
  });
};

export const createWorkflow = (workflow: Partial<Workflow>): Promise<ApiResponse<number>> => {
  return defHttp.post({
    url: '/api/workflow',
    data: workflow
  });
};

export const updateWorkflow = (id: number, workflow: Partial<Workflow>): Promise<ApiResponse<boolean>> => {
  return defHttp.put({
    url: `/api/workflow/${id}`,
    data: workflow
  });
};

export const getWorkflowDetail = (id: number): Promise<ApiResponse<Workflow>> => {
  return defHttp.get({
    url: `/api/workflow/${id}`
  });
};

export const deleteWorkflow = (id: number): Promise<ApiResponse<boolean>> => {
  return defHttp.delete({
    url: `/api/workflow/${id}`
  });
};

export const getWorkflows = (): Promise<ApiResponse<Workflow[]>> => {
  return getWorkflowList();
};

export const getAllStages = (): Promise<ApiResponse<WorkflowStage[]>> => {
  return defHttp.get({
    url: '/api/workflow/stages'
  });
};

export const getStagesByWorkflow = (workflowId: number): Promise<ApiResponse<WorkflowStage[]>> => {
  return defHttp.get({
    url: `/api/workflow/${workflowId}/stages`
  });
}; 