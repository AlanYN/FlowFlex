import { ElMessage } from 'element-plus';

export const setItem = (key: string, val: any) => {
	const str = typeof val === 'string' ? val : JSON.stringify(val);
	localStorage.setItem(key, str);
};

export const getItem = (key: string) => {
	const item = localStorage.getItem(key);
	if (item?.startsWith('{') || item?.startsWith('"') || item?.startsWith('[')) {
		return JSON.parse(item);
	} else {
		return item;
	}
};

export const removeItem = (key: string) => {
	localStorage.removeItem(key);
};

export const clearAllItem = () => {
	localStorage.clear();
};

export const downloadBlob = (data: any) => {
	const contentDisposition = data.headers['content-disposition'];
	const index = contentDisposition.indexOf('e=');
	const fileName =
		contentDisposition.slice(index + 3, contentDisposition.length - 1) ||
		new Date().getTime() + '.csv';
	download(fileName, data.data);
};

export const download = (fileName: string, blob: Blob) => {
	const link = document.createElement('a');
	link.href = URL.createObjectURL(blob);
	link.download = fileName;
	link.click();
	link.remove();
	URL.revokeObjectURL(link.href);
};

export const validateEmail = (email: string) => {
	// eslint-disable-next-line
	const reg = /^[A-Za-z0-9]+([_\.][A-Za-z0-9]+)*@([A-Za-z0-9\-]+\.)+[A-Za-z]{2,6}$/;
	return reg.test(email);
};

export const getProcessErrorResponseMessage = (response: any) => {
	let result = response;
	if (response?.status && response.status === 400) {
		result = 'Bad Request, please adjust your request and try it again.';
		if (response.error) {
			result = response.error;
		}
		const err = response.data
			? response.data.data
				? response.data.data
				: response.data
			: response.data;
		//判断err 是否是对象
		if (err && typeof err === 'object') {
			if (err.msg) {
				result = err.msg;
			} else if (err.error) {
				result = err.error;
			}
		} else if (err) {
			try {
				result = JSON.parse(err).error;
			} catch (error) {
				result = err;
			}
		}
	}
	if (response?.status && response.status === 500) {
		result = 'Server Internal Error, Please call the admin to fix it.';
		if (response.error) {
			result = response.error;
		}
		let erData = response.data
			? response.data.data
				? response.data.data
				: response.data
			: response.data;
		if (erData) {
			let erroJson;
			try {
				erroJson = JSON.parse(erData).error;
			} catch (error) {
				result = erData.error;
			}
			if (erroJson) {
				if (erroJson.indexOf('"msg"') > -1) {
					try {
						result = JSON.parse(erroJson).msg;
					} catch (error) {
						result = erroJson;
					}
				} else {
					result = erroJson;
				}
			} else if (erData.msg) {
				result = erData.msg;
			}
		}

		erData = response.text;
		if (erData) {
			const erroJson = JSON.parse(erData);
			if (erroJson.msg) {
				result = erroJson.msg;
			}
		}
	}
	if (response?.status && response.status === 404) {
		result = 'Service you called is not found.';
	}
	if (response?.status && (response.status === 503 || response.status === 502)) {
		result = 'Service is under system maintenance, please come back later.';
	}
	if (response?.status && response.status === -1) {
		result = 'System connection time out, please try again later.';
	}
	return result;
};

export const getBrowserInfo = () => {
	const ua = navigator.userAgent;
	let name = '';
	let version = '';

	// 判断是否为 Chrome 浏览器
	if (/Chrome\/([\d.]+)/.test(ua)) {
		name = 'Chrome';
		version = RegExp.$1;
	}
	// 判断是否为 Firefox 浏览器
	else if (/Firefox\/([\d.]+)/.test(ua)) {
		name = 'Firefox';
		version = RegExp.$1;
	}
	// 判断是否为 Safari 浏览器
	else if (/Safari\/([\d.]+)/.test(ua)) {
		name = 'Safari';
		version = RegExp.$1;
	}
	// 判断是否为 IE 浏览器
	else if (/Trident\/([\d.]+)/.test(ua)) {
		name = 'Internet Explorer';
		version = RegExp.$1;
	}
	// 判断是否为 Edge 浏览器
	else if (/Edge\/([\d.]+)/.test(ua)) {
		name = 'Edge';
		version = RegExp.$1;
	}
	// 判断是否为 Opera 浏览器
	else if (/OPR\/([\d.]+)/.test(ua)) {
		name = 'Opera';
		version = RegExp.$1;
	}
	// 其他浏览器
	else {
		name = 'Unknown';
		version = 'Unknown';
	}

	return {
		name,
		version,
	};
};

export function convertToKebabCase(str: string) {
	return str
		.toLowerCase() // Convert to lowercase
		.replace(/\s+|\/|_/g, '-'); // Replace spaces, slashes, and underscores with hyphens
}

export function convertToTitleCase(str: string) {
	return str
		.replace(/-/g, ' ') // Replace all hyphens with spaces
		.replace(/_/g, ' ') // Replace all underscores with spaces
		.replace(/\//g, ' ') // Replace all slashes with spaces
		.replace(/\b(\w)/g, function (char) {
			return char.toUpperCase();
		}); // Capitalize the first letter of each word
}

export function formattedTime12Hour(date) {
	if (!isEmpty(date)) {
		date = new Date(date);
	}
	if (date instanceof Date) {
		const year = date.getFullYear();
		const month = date.getMonth() + 1;
		const day = date.getDate();
		const hours = date.getHours();
		const minutes = date.getMinutes();
		const period = hours >= 12 ? 'PM' : 'AM';
		const formattedHours = hours % 12 || 12;
		return `${padZero(month)}/${padZero(day)}/${year} ${formattedHours}:${padZero(
			minutes
		)} ${period}`;
	} else {
		return '';
	}
}

export function isEmpty(value) {
	if (typeof value === 'undefined' || value === null || value === '') {
		return true;
	}
	return false;
}

function padZero(value) {
	return value < 10 ? `0${value}` : value;
}

export function formatDate(date, fmt) {
	if (date != undefined) {
		date = new Date(date);
		if (/(y+)/.test(fmt)) {
			fmt = fmt.replace(RegExp.$1, (date.getFullYear() + '').substr(4 - RegExp.$1.length));
		}
		const o = {
			'M+': date.getMonth() + 1,
			'd+': date.getDate(),
			'h+': date.getHours(),
			'm+': date.getMinutes(),
			's+': date.getSeconds(),
		};
		for (const k in o) {
			if (new RegExp(`(${k})`).test(fmt)) {
				const str = o[k] + '';
				fmt = fmt.replace(RegExp.$1, RegExp.$1.length === 1 ? str : padLeftZero(str));
			}
		}
		return fmt;
	} else {
		return null;
	}
}

function padLeftZero(str) {
	return ('00' + str).substr(str.length);
}

// secondVal: For example in 1 - 10 of 160 it's 10
// totalItems: For example in 1 - 10 of 160 it's 160
export function getCurrentTablePage(totalItems: number, secondVal: number, recordsPerPage: number) {
	const totalPages = Math.ceil(totalItems / recordsPerPage);
	const diff = totalItems - secondVal;
	const pagesLeft = Math.ceil(diff / recordsPerPage);
	return totalPages - pagesLeft;
}

export const obj2Param = (obj: object) => {
	let objStr = '';
	Object.keys(obj).map((key) => {
		if (obj[key] != 0 && obj[key] != null && obj[key] != '') {
			if (objStr === '') {
				objStr = '?' + key + '=' + obj[key];
			} else {
				objStr = objStr + '&' + key + '=' + obj[key];
			}
		}
	});
	return objStr;
};

//时间格式化成12小时制MM/dd/yyyy h:mm:ss am/pm
export function formattedTime12HourSeconds(date) {
	date = new Date(date);
	if (!isNaN(date)) {
		const year = date.getFullYear();
		const month = date.getMonth() + 1;
		const day = date.getDate();
		const hours = date.getHours();
		const minutes = date.getMinutes();
		const seconds = date.getSeconds();
		const period = hours >= 12 ? 'PM' : 'AM';
		const formattedHours = hours % 12 || 12;
		return `${padZero(month)}/${padZero(day)}/${year} ${formattedHours}:${padZero(
			minutes
		)}:${padZero(seconds)} ${period}`;
	} else {
		return 'null';
	}
}

export function formattedTime24HourSeconds(inputDate) {
	const date = new Date(inputDate);
	const year = date.getFullYear();
	const month = String(date.getMonth() + 1).padStart(2, '0');
	const day = String(date.getDate()).padStart(2, '0');
	const hours = String(date.getHours()).padStart(2, '0');
	const minutes = String(date.getMinutes()).padStart(2, '0');
	const seconds = String(date.getSeconds()).padStart(2, '0');

	return `${year}-${month}-${day} ${hours}:${minutes}:${seconds}`;
}

// 获取数组符合条件的项放在最前面
export const moveItemToFront = (array, conditionFn) => {
	// 使用 filter 方法分别找出满足条件和不满足条件的项
	const matches = array.filter(conditionFn);
	const nonMatches = array.filter((item) => !conditionFn(item));

	// 使用 concat 方法将满足条件的项放在前面
	return matches.concat(nonMatches);
};

/**
 * 判断path是否为外链
 * @param {string} path
 * @returns {Boolean}
 */
export function isExternal(path) {
	return /^(https?:|mailto:|tel:)/.test(path);
}

/**
 *
 * @param dataArr 需要转换的数组
 * @param value 需要转换为value的��
 * @param label 需要转换的label的键
 */
export function dataConversion(dataArr: any[], value, label) {
	const arr = [] as any[];
	dataArr.forEach((item) => {
		arr.push({
			value: item[value],
			label: item[label],
		});
	});
	return arr;
}

export function keyMapLabel(dataArr: any[], key) {
	if (Array.isArray(dataArr) && dataArr.length > 0) {
		const filterArr = dataArr.filter((item) => item.key == key);
		return filterArr.length > 0 ? filterArr[0]?.value : key;
	}
	return key;
}

export function openLink(url: string) {
	checkLinkValidity(url).then((isValid) => {
		if (isValid) {
			console.log('The link is valid.');
			window.open(url, '_blank');
		} else {
			ElMessage.error('The link is invalid.');
		}
	});
}

function isValidURL(string: string): boolean {
	try {
		new URL(string);
		return true;
	} catch (_) {
		return false;
	}
}

// 检测链接是否有效
async function checkLinkValidity(url) {
	if (!isValidURL(url)) {
		console.error('Invalid URL:', url);
		return;
	}

	try {
		const response = await fetch(url, { method: 'HEAD', mode: 'no-cors' });
		// 若请求成功，fetch会返回一个response对象
		return response.type === 'opaque' || response.ok;
	} catch (error) {
		console.error('Error:', error);
		return false;
	}
}

export function isIframe() {
	return window.parent !== window.self;
}

// 解析url地址栏参数包含hash
export const parseUrlSearch = (url) => {
	let nurl = url;
	const obj = {};

	if (url.includes('?')) {
		const search = url.split('?')[1];
		const searchArr = search.split('&');
		searchArr.forEach((item) => {
			const [key, value] = item.split('=');
			obj[key] = value;
		});

		const urlArr = url.split('?');
		urlArr.pop();
		nurl = urlArr.join('?');
	}

	return {
		url: nurl,
		query: obj,
	};
};

export function objectToQueryString(obj: Record<string, any>): string {
	return Object.entries(obj)
		.map(([key, value]) => `${encodeURIComponent(key)}=${encodeURIComponent(value)}`)
		.join('&');
}
