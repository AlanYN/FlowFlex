// 子组件路由
export default [
	{
		path: '/customer-portal',
		name: 'sub-CustomerPortal',
		component: () => import('./index.vue'),
		meta: {
			title: 'Customer Portal',
			requiresAuth: true,
			layout: 'portal',
		},
		children: [
			{
				path: 'progress',
				name: 'sub-OnboardingProgress',
				component: () => import('./components/OnboardingProgress.vue'),
				meta: {
					title: 'Cases Progress',
					icon: 'el-icon-progress',
				},
			},
			{
				path: 'messages',
				name: 'sub-MessageCenter',
				component: () => import('./components/MessageCenter.vue'),
				meta: {
					title: 'Message Center',
					icon: 'el-icon-message',
				},
			},
			{
				path: 'documents',
				name: 'sub-DocumentCenter',
				component: () => import('./components/DocumentCenter.vue'),
				meta: {
					title: 'Document Center',
					icon: 'el-icon-document',
				},
			},
			{
				path: 'questionnaire',
				name: 'sub-CustomerQuestionnaire',
				component: () => import('./components/CustomerQuestionnaire.vue'),
				meta: {
					title: 'Customer Questionnaire',
					icon: 'el-icon-edit-outline',
				},
			},
			{
				path: 'contact',
				name: 'sub-ContactUs',
				component: () => import('./components/ContactUs.vue'),
				meta: {
					title: 'Contact Us',
					icon: 'el-icon-service',
				},
			},
		],
	},
	{
		path: '/onboard/sub-portal/portal',
		name: 'sub-OnboardPortal',
		component: () => import('./portal.vue'),
		meta: {
			title: 'Cases Portal Details',
			requiresAuth: true,
		},
	},
];
