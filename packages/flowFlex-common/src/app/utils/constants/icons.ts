import {
	Document,
	Calendar,
	List,
	Operation,
	User,
	Link,
	Files,
	Phone,
} from '@element-plus/icons-vue';
import NumberIcon from '@assets/svg/global/number.svg';
import EmailIcon from '@assets/svg/publicPage/email_notification.svg';
import { propertyTypeEnum } from '@/enums/appEnum';

export const FIELD_TYPE_ICONS: Record<string, any> = {
	[propertyTypeEnum.SingleLineText]: Document,
	[propertyTypeEnum.Number]: NumberIcon,
	[propertyTypeEnum.DatePicker]: Calendar,
	[propertyTypeEnum.DropdownSelect]: List,
	[propertyTypeEnum.Pepole]: User,
	[propertyTypeEnum.Connection]: Link,
	[propertyTypeEnum.FileList]: Files,
	[propertyTypeEnum.Phone]: Phone,
	[propertyTypeEnum.Email]: EmailIcon,
	default: Document,
} as const;

export const OPERATION_ICON = Operation;
