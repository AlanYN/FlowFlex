/**
 * 监听数字输入、替换掉字母和字符为空字符
 * @param event
 */
export const handleInputNumberChangeReplaceString = (event) => {
	// 保留小数点后两位
	let value = event.target.value;
	// 使用正则表达式匹配非数字和多余的小数点
	value = value
		.replace(/[^\d.]/g, '')
		.replace(/\.{2,}/g, '.')
		.replace('.', '$#$')
		.replace(/\./g, '')
		.replace('$#$', '.');
	// 只保留小数点后两位
	value = value.replace(/^(-?\d+)\.(\d{2}).+$/, '$1.$2');
	//
	// 更新输入框的值
	event.target.value = value;
	// 更新绑定的数据
	// formDate.value.annualSpending = value;
};

/**
 * 转为5位浮点小数 （不支持 - ）
 * @param val
 * @param minus 是否可以是小数
 * @returns {string}
 * e.g. 12.34a => 12.34
 */
export const toFloatNumber = (val, minNumber, minus = false) => {
	const newValue = minus
		? val
				.toString()
				.replace(/。/, '.')
				.replace(/[^\d.-]+/g, '') // 把不是数字，不是小数点、-的过滤掉
				.replace(/^(-?)0+(\d)/, '$1$2') // 如果第一位是0，且后面是数字，保留负号，过滤掉0
				.replace(/^\./, '0.') // 如果输入的第一位为小数点，则替换成 0.
				.replace(/^-?\./, '-0.') // 如果负号后面是小数点，则替换成 -0.
				.replace(/^(-?\d+)\.(\d{5}).*$/, '$1.$2') // 只保留第一个"."，清除多余的"."
				.replace(/(?!^)-/g, '') // 移除所有非开头位置的负号
		: val
				.toString()
				.replace(/。/, '.')
				.replace(/[^\d^.^]+/g, '') // 第二步：把不是数字，不是小数点、-的过滤掉
				.replace(/^0+(\d)/, '$1') // 第三步：第一位0开头，0后面为数字，则过滤掉，取后面的数字
				.replace(/^\./, '0.') // 第四步：如果输入的第一位为小数点，则替换成 0. 实现自动补全
				.replace('.', '$#$')
				.replace(/\./g, '')
				.replace('$#$', '.')
				.replace(/^(\d+)\.(\d{5}).+$/, '$1.$2'); // 只保留第一个".", 清除多余的"."
	// .match(/^\d*(\.?\d{0,9})/g)[0] || ""; // 第五步：最终匹配得到结果 以数字开头，只有一个小数点，而且小数点后面只能有1到9位小数
	if (!minus && newValue < minNumber) {
		return minNumber;
	}

	return newValue;
};

/**
 *
 *  只能是0以上的正整数
 * @param val
 * @param minus 是否可以是小数
 * @returns
 */
export const toIntegerNumber = (val, minNumber, minus = false) => {
	const newValue = minus
		? val
				.toString()
				.replace(/[^-\d]/g, '') // 移除所有非数字和非负号字符
				.replace(/(?!^-)-/g, '') // 移除所有非开头位置的负号
				.replace(/^(-?)0+(?!$)/, '$1') // 去掉前导零，但允许单个零，并保留负号
		: val
				.toString()
				.replace(/[^\d]/g, '') // 移除所有非数字字符
				.replace(/^0+(?!$)/, ''); // 去掉前导零，但允许单个零

	if (newValue !== '' && !minus && newValue < minNumber) {
		return minNumber;
	}
	return newValue;
};

// Luhn算法校验
export const checkLuhn = (bankno) => {
	const lastNum = parseInt(bankno.charAt(bankno.length - 1)); // 取出最后一位
	const first15Num = bankno.substring(0, bankno.length - 1); // 前15或18位
	const newArr = [...first15Num].reverse(); // 前15或18位倒序存进数组

	let sumTotal = 0;
	for (let i = 0; i < newArr.length; i++) {
		let thisNum = parseInt(newArr[i]);
		if (i % 2 === 0) {
			thisNum *= 2;
			if (thisNum > 9) {
				thisNum -= 9;
			}
		}
		sumTotal += thisNum;
	}

	const k = sumTotal % 10 === 0 ? 10 : sumTotal % 10;
	const luhn = 10 - k;

	return lastNum === luhn;
};
