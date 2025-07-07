import _ from 'lodash';

// 深度遍历删除数据中空值的属性
export const deepOmitBy = (value: any, iteratee: (value: any) => boolean): any => {
	if (_.isArray(value)) {
		return value.map((v) => deepOmitBy(v, iteratee)).filter((v) => !_.isEmpty(v));
	} else if (_.isObject(value)) {
		value = _.omitBy(value, iteratee);
		return _.mapValues(value, (v) => deepOmitBy(v, iteratee));
	}
	return value;
};
