import { Options } from '#/setting';

const preferencesCommunication: Options[] = [
	{ key: 'Email', value: 'Email' },
	{ key: 'Call', value: 'Call' },
	{ key: 'Offline', value: 'Offline' },
];

const preferenceslanguage = [
	{ key: 'Chinese', value: 'Chinese' },
	{ key: 'German', value: 'German' },
	{ key: 'English', value: 'English' },
	{ key: 'French', value: 'French' },
	{ key: 'Italian', value: 'Italian' },
	{ key: 'Spanish', value: 'Spanish' },
	{ key: 'Korean', value: 'Korean' },
	{ key: 'Japanese', value: 'Japanese' },
];

const preferencesCurrency: Options[] = [
	{ key: 'USD', value: 'US Dollar ($)' },
	{ key: 'EUR', value: 'Euro (€)' },
	{ key: 'JPY', value: 'Japanese Yen (¥)' },
	{ key: 'GBP', value: 'British Pound (£)' },
	{ key: 'AUD', value: 'Australian Dollar (A$)' },
	{ key: 'CAD', value: 'Canadian Dollar (C$)' },
	{ key: 'CHF', value: 'Swiss Franc (CHF)' },
	{ key: 'CNY', value: 'Chinese Yuan (¥)' },
	{ key: 'HKD', value: 'Hong Kong Dollar (HK$)' },
	{ key: 'INR', value: 'Indian Rupee (₹)' },
	{ key: 'RUB', value: 'Russian Ruble (₽)' },
	{ key: 'BRL', value: 'Brazilian Real (R$)' },
	{ key: 'ZAR', value: 'South African Rand (R)' },
	{ key: 'SGD', value: 'Singapore Dollar (S$)' },
	{ key: 'KRW', value: 'South Korean Won (₩)' },
	{ key: 'MXN', value: 'Mexican Peso (Mex$)' },
	{ key: 'NZD', value: 'New Zealand Dollar (NZ$)' },
	{ key: 'PHP', value: 'Philippine Peso (₱)' },
	{ key: 'AED', value: 'UAE Dirham (د.إ)' },
];

const buyingReason: Options[] = [
	{ key: 'New Requirement', value: 'New Requirement' },
	{ key: 'Other', value: 'Other' },
	{ key: 'Replace', value: 'Replace' },
	{ key: 'Existing', value: 'Existing' },
];

const buyingTimeframe: Options[] = [
	{ key: 'less than 1 month', value: 'less than 1 month' },
	{ key: '1-3 month', value: '1-3 month' },
	{ key: '4-6 months', value: '4-6 months' },
];

const defaultAssignedGroup: Options[] = [
	{
		value: 'Sales',
		key: 'Sales',
	},
	{
		value: 'Account Management',
		key: 'Account Management',
	},
	{
		value: 'IT',
		key: 'IT',
	},
	{
		value: 'Legal',
		key: 'Legal',
	},
	{
		value: 'Operations',
		key: 'Operations',
	},
	{
		value: 'Finance',
		key: 'Finance',
	},
	{
		value: 'Customer',
		key: 'Customer',
	},
	{
		value: 'CSR',
		key: 'CSR',
	},
	{
		value: 'Implementation',
		key: 'Implementation',
	},
	{
		value: 'WISE Support',
		key: 'WISE Support',
	},
	{
		value: 'Billing',
		key: 'Billing',
	},
];

export {
	preferencesCommunication,
	preferenceslanguage,
	preferencesCurrency,
	buyingReason,
	buyingTimeframe,
	defaultAssignedGroup,
};
