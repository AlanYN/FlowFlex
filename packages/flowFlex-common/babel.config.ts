module.exports = {
	presets: [
		[
			'@babel/preset-env',
			{
				useBuiltIns: 'usage',
				corejs: 3,
			},
		],
		[
			'@babel/preset-typescript',
			{
				allExtensions: true,
			},
		],
	],
};
