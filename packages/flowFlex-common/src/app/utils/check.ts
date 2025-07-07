//底层校验规则

// 率数
const rateRex = /^0\.[0-9]{1,2}$|^0{1}$|^1{1}$|^1\.[0]{1,2}$/;
// 数字
const numRex = /-[0-9]+(.[0-9]+)?|[0-9]+(.[0-9]+)?/;
// 数字正则 只能输入
export const NUMBER_REG = /^(([1-9]{1}\d*)|(0{1}))$/;
// 金钱（不含0，正数、两位小数）
const trueNum = /(^[1-9]([0-9]+)?(\.[0-9]{1,2})?$)|(^(0){1}$)|(^[0-9]\.[0-9]([0-9])?$)/;
// 校验网址(url,支持端口和"?+参数"和"#+参数")
const urlRex = /^(((ht|f)tps?):\/\/)?[\w-]+(\.[\w-]+)+([\w.,@?^=%&:/~+#-]*[\w@?^=%&/~+#-])?$/;
//邮箱
const regEmail = /^[A-Za-z\d]+([-_.][A-Za-z\d]+)*@([A-Za-z\d]+[-.])+[A-Za-z\d]{2,4}$/;

/**
 * 校验正数
 * @param {string} str
 */
export function checkTrueNum(str: string | number) {
	return trueNum.test(str as string);
}

/**
 * 校验网址
 * @param {*} str
 */
export function checkUrl(str: string) {
	return urlRex.test(str);
}

/**
 * 校验邮箱格式
 * @param {string} str
 */
export function checkEmail(str: string) {
	return regEmail.test(str);
}

/**
 * 校验数字
 * @param {string} str
 */
export function checkNumber(str: string | number) {
	return numRex.test(str as string);
}

/**
 * 校验率数
 * @param {string} str
 */
export function checkRate(str: string | number) {
	return rateRex.test(str as string);
}
