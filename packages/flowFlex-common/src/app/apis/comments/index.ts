import { defHttp } from '@/apis/axios';

import { useGlobSetting } from '@/settings';

const globSetting = useGlobSetting();

const Api = () => {
	return {
		commentApi: `${globSetting.apiProName}/shared/v1/comments`,

		updateCommentCount: `${globSetting.apiProName}/activities/v1/detail`,
	};
};

export function getComment(params) {
	return defHttp.get({ url: `${Api().commentApi}`, params });
}

export function postComment(params) {
	return defHttp.post({ url: `${Api().commentApi}`, params });
}

export function editComment(params) {
	return defHttp.put({ url: `${Api().commentApi}`, params });
}

export function deleteComment(id) {
	return defHttp.delete({ url: `${Api().commentApi}/${id}` });
}

export function updateCommentCount(params) {
	return defHttp.get({
		url: Api().updateCommentCount,
		params,
	});
}
