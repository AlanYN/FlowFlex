module.exports = {
	root: true,
	extends: ['@uni'],
	rules: {
		'vue/html-self-closing': [
			'error',
			{
				html: {
					void: 'always',
					normal: 'never',
					component: 'always',
				},
				svg: 'always',
				math: 'always',
			},
		],
		'@typescript-eslint/no-duplicate-enum-values': 'off', // 禁用重复枚举值检查
		'@typescript-eslint/no-unused-vars': [
			'error',
			{
				vars: 'all', // 检查所有变量
				args: 'none', // 忽略函数参数
				ignoreRestSiblings: true, // 忽略解构中的剩余属性
			},
		],
	},
};
