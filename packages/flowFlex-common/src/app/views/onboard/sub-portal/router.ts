// 子组件路由
export default [
	{
		path: '/customer-portal',
		name: 'sub-CustomerPortal',
		component: () => import('./index.vue'),
		meta: {
			title: '客户门户',
			requiresAuth: true,
			layout: 'portal',
		},
		children: [
			{
				path: 'progress',
				name: 'sub-OnboardingProgress',
				component: () => import('./components/OnboardingProgress.vue'),
				meta: {
					title: '入职进度',
					icon: 'el-icon-progress',
				},
			},
			{
				path: 'messages',
				name: 'sub-MessageCenter',
				component: () => import('./components/MessageCenter.vue'),
				meta: {
					title: '消息中心',
					icon: 'el-icon-message',
				},
			},
			{
				path: 'documents',
				name: 'sub-DocumentCenter',
				component: () => import('./components/DocumentCenter.vue'),
				meta: {
					title: '文档中心',
					icon: 'el-icon-document',
				},
			},
			{
				path: 'questionnaire',
				name: 'sub-CustomerQuestionnaire',
				component: () => import('./components/CustomerQuestionnaire.vue'),
				meta: {
					title: '问卷调查',
					icon: 'el-icon-edit-outline',
				},
			},
			{
				path: 'contact',
				name: 'sub-ContactUs',
				component: () => import('./components/ContactUs.vue'),
				meta: {
					title: '联系我们',
					icon: 'el-icon-service',
				},
			},
		],
	},
];
