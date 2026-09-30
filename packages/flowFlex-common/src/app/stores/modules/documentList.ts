import { defineStore } from 'pinia';
import { ElMessage } from 'element-plus';
import { querySnapshots, deleteSnapshot } from '@/apis/ow/documentSnapshot';

export interface DocumentSnapshotItem {
	id: string;
	unitId: string;
	title: string;
	docType: 'Sheet' | 'Doc' | 'Slide';
	modifyDate: string;
	createDate: string;
	createBy: string;
	modifyBy: string;
	revision: number;
}

interface DocumentListState {
	list: DocumentSnapshotItem[];
	total: number;
	page: number;
	pageSize: number;
	keyword: string;
	docType: '' | 'Sheet' | 'Doc' | 'Slide';
	sortBy: string;
	sortOrder: string;
	loading: boolean;
}

export const useDocumentListStore = defineStore({
	id: 'item-wfe-document-list',

	state: (): DocumentListState => ({
		list: [],
		total: 0,
		page: 1,
		pageSize: 20,
		keyword: '',
		docType: '',
		sortBy: 'modify_date',
		sortOrder: 'desc',
		loading: false,
	}),

	actions: {
		async loadList() {
			this.loading = true;
			try {
				// transformResponseHook returns the full SuccessResponse body:
				// { code: "200", data: PagedResult<T>, msg, success }
				const res = (await querySnapshots({
					pageIndex: this.page,
					pageSize: this.pageSize,
					keyword: this.keyword || undefined,
					docType: this.docType || undefined,
					sortBy: this.sortBy,
					sortOrder: this.sortOrder,
				})) as {
					code: string;
					success: boolean;
					data: { items?: DocumentSnapshotItem[]; totalCount?: number } | null;
				} | null;
				if (res?.data?.items) {
					this.list = res.data.items;
					this.total = res.data.totalCount ?? 0;
				}
			} catch (error) {
				console.error('Failed to load document list:', error);
			} finally {
				this.loading = false;
			}
		},

		async deleteDocument(id: string) {
			try {
				await deleteSnapshot(id);
				ElMessage.success('Document deleted');
				await this.loadList();
			} catch (error) {
				ElMessage.error('Failed to delete, please try again');
			}
		},

		resetFilter() {
			this.keyword = '';
			this.docType = '';
			this.sortBy = 'modify_date';
			this.sortOrder = 'desc';
			this.page = 1;
		},

		setPage(page: number) {
			this.page = page;
			this.loadList();
		},

		setKeyword(keyword: string) {
			this.keyword = keyword;
			this.page = 1;
			this.loadList();
		},

		setDocType(docType: '' | 'Sheet' | 'Doc' | 'Slide') {
			this.docType = docType;
			this.page = 1;
			this.loadList();
		},

		setSortBy(sortBy: string) {
			this.sortBy = sortBy;
			this.page = 1;
			this.loadList();
		},
	},
});
