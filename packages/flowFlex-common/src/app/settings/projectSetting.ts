import { ref } from 'vue';

export const defaultStr = '- -'; // 默认字符串

export const financialCategotyCode = 48;

export const customerTasgKey = 16;

export const pageOfNumber = [10, 15, 20, 25, 30, 40, 50, 100]; // table表格的分页可选字段

export const inputTextraAutosize = { minRows: 4, maxRows: 10 }; // 文本框为富文本的时候最小高度和最大高度

export const inputTextMaxLength = 50; // 文本框的最大长度

export const notesPageTextraMaxLength = 1000; // 富文本的输入长度

export const textraTwoHundredLength = 1000;

export const textraMaxLength = 100; // 普通富文本的输入长度

export const collapseUnfold = ref(['1', '2', '3']); //折叠面板默认展开哪几层

export const collapseRightUnfold = ref(['1', '2', '3', '4']); //折叠面板默认展开哪几层

export const tableMaxHeight = '700px'; // tbale表格的最大高度

export const receptionDate = 'YYYY-MM-DD HH:mm:ss'; // 入库日期

export const projectDate = 'MM/DD/YYYY'; // 项目默认年月日的日期

export const projectTenMinutesSsecondsDate = 'MM/DD/YYYY HH:mm:ss'; // 项目年月日十分秒日期

export const projectTenMinuteDate = 'MM/DD/YYYY HH:mm'; // 项目年月日十分日期

export const dialogWidth = '550'; // 项目弹窗的宽

export const smallDialogWidth = '344'; // 项目简单操作弹窗的宽

export const bigDialogWidth = '844'; // 项目复杂操作弹窗的宽

export const searchButton = false; // 是否显示搜索按钮

export const changeI18nButton = false; // 是否显示切换语言按钮

export const changeThemeButton = true; // 是否显示切换主题按钮

export const settingButton = false; // 是否显示设置按钮

export const changeTimeZoneButton = false; // 是否显示切换时区按钮
