import dayjs from 'dayjs';

/**
 * 获取当前年的所有月和日
 * @param year 指定年份，默认为当前年
 * @returns 包含年份、月份和日期的结构化数据
 */
export const getYearMonthDays = (year?: number) => {
	// 如果没有传入年份，默认使用当前年份
	const currentYear = year || dayjs().year();

	// 创建最终的结构
	const yearMonthDays = {
		year: currentYear,
		months: Array.from({ length: 12 }, (_, month) => {
			const currentMonth = dayjs().year(currentYear).month(month);
			const daysInMonth = currentMonth.daysInMonth();

			return {
				month: month + 1, // 月份从1开始
				monthName: currentMonth.format('MMMM'), // 月份名称
				days: Array.from({ length: daysInMonth }, (_, index) => index + 1),
			};
		}),
	};

	return yearMonthDays;
};

/**
 * 将年月日数据转换为下拉框选项
 * @param yearMonthDays getYearMonthDays 返回的数据
 * @returns 转换后的选项
 */
export const convertToSelectOptions = (yearMonthDays) => {
	return {
		monthOptions: yearMonthDays.months.map((m) => ({
			label: m.monthName,
			value: m.month,
		})),
		getDaysForMonth: (month: number) => {
			const selectedMonth = yearMonthDays.months.find((m) => m.month === month);
			return selectedMonth
				? selectedMonth.days.map((day) => ({
						label: `${day}`,
						value: day,
				  }))
				: [];
		},
	};
};
