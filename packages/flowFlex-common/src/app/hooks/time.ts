import { projectDate, projectTenMinutesSsecondsDate } from '@/settings/projectSetting';
import dayjs from 'dayjs';
import utc from 'dayjs/plugin/utc';
import timezone from 'dayjs/plugin/timezone';
import { useUserStore } from '@/stores/modules/user';

dayjs.extend(utc);
dayjs.extend(timezone);

/**
 * Converts a time string to a specified format and timezone, handling both incoming and outgoing transformations.
 * @param dateString The time string to be processed. The format must be a valid date format and can be from any timezone.
 * @param shift Indicates whether the time is being processed for sending to the backend (true) or from the backend (false).
 * @param format The desired format for the converted time, default is MM/DD/YYYY.
 * @returns The time string converted into the specified format.
 */
export function timeZoneConvert(
	dateString: string,
	shift: boolean = false,
	format: string = projectDate
): string {
	if (!dateString) return dateString;
	if (!isValidDate(dateString)) return dateString;
	if (!shift) {
		const toTimeZone = getTimeZoneInfo();
		// console.log('Current timezone:', toTimeZone.timeZone, 'Current timezone offset:', toTimeZone.offset);
		if (isTimeZone(dateString)) {
			// If the backend's returned time includes a timezone, convert it to the current timezone
			// First convert it to a timestamp, then convert it to the specified timezone time
			const parsedDatetime = dayjs(dateString).valueOf();
			const convertedTime = dayjs(parsedDatetime).tz(toTimeZone.timeZone);
			// console.log('convertedTime:', convertedTime.format());
			return convertedTime.format(format);
		} else {
			// If it does not include a timezone, return directly
			return dayjs(dateString).format(format);
		}
	} else {
		// Processing time to send to the backend, convert to UTC time
		const date = parseDateWithDefaultTime(dateString);
		return date.format();
	}
}

/**
 * Gets the current timezone and offset.
 * @returns The current timezone and its offset, e.g., {timeZone: 'Asia/Shanghai', offset: '+08:00'}.
 */
export function getTimeZoneInfo() {
	const userStore = useUserStore();
	const userTimeZone = userStore.getUserInfo.defaultTimeZone;
	if (userTimeZone)
		return { timeZone: userTimeZone, offset: getTimezoneOffsetForTimezone(userTimeZone) };

	const timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;
	return { timeZone, offset: getTimezoneOffsetForTimezone(timeZone) };
}

/**
 * Checks if the provided string is a valid date.
 * @param dateString The date string to check.
 * @returns true if the date is valid, false otherwise.
 */
export function isValidDate(dateString: string | Date): boolean {
	// Parse the date string
	const parsedDate = dayjs(dateString);
	// Check if it is a valid date
	return parsedDate.isValid();
}

/**
 * Treats the input time as local time and converts it to UTC time.
 * @param dateString The time string to convert.
 * @returns The time string converted to UTC.
 */
function parseDateWithDefaultTime(dateString) {
	// Check if it contains time part
	if (!isValidDate(dateString)) return dateString;

	// If the time part is incomplete, add "00:00:00"
	if (!/ \d{2}:\d{2}:\d{2}$/.test(dateString)) {
		// If the time part is completely missing, complete it with "00:00:00"
		if (!/ \d{2}:\d{2}$/.test(dateString)) {
			dateString += ' 00:00:00';
		}
		// If seconds are missing, complete with ":00"
		else if (!/ \d{2}:\d{2}:\d{2}$/.test(dateString)) {
			dateString += ':00';
		}
	}

	const toTimeZone = getTimeZoneInfo().timeZone;
	// Use dayjs to parse the date string
	const date = dayjs.tz(dateString, projectTenMinutesSsecondsDate, toTimeZone).utc();
	return date;
}

// Regular expression to check for timezone in time format
const timezonePattern =
	/^\d{4}-\d{2}-\d{2}(T|\s)\d{2}:\d{2}:\d{2}(\.\d{3})?\s?(Z|([+-]\d{2}:\d{2}))$/;
function isTimeZone(dateString) {
	// Use regular expression to check if it matches the ISO 8601 UTC format
	if (!timezonePattern.test(dateString)) {
		return false;
	}

	// Try to parse with dayjs and check if it is valid
	const date = dayjs(dateString);
	return date.isValid() && date.utc();
}

/**
 * Gets the offset for a specified timezone.
 * @param timeZone The timezone string.
 * @returns The offset for the specified timezone.
 */
function getTimezoneOffsetForTimezone(timeZone) {
	// Current time
	const date = new Date();

	// Get the timestamp for the specified timezone
	const tzDate = new Date(date.toLocaleString('en-US', { timeZone }));

	// Get the UTC timestamp for the current time
	const utcDate = new Date(date.toLocaleString('en-US', { timeZone: 'UTC' }));

	// Calculate the difference between the specified timezone time and UTC time, and convert to hours and minutes
	const timezoneOffsetMinutes = (utcDate.getTime() - tzDate.getTime()) / 60000;
	const offsetHours = Math.floor(Math.abs(timezoneOffsetMinutes) / 60);
	const offsetMinutes = Math.abs(timezoneOffsetMinutes) % 60;
	const sign = timezoneOffsetMinutes >= 0 ? '-' : '+';

	const timezoneOffset = `${sign}${String(offsetHours).padStart(2, '0')}:${String(
		offsetMinutes
	).padStart(2, '0')}`;

	// console.log('The timezone is:', timeZone, 'Offset:', timezoneOffset);
	return timezoneOffset;
}

/**
 * Gets the current time in the specified timezone.
 * @param timeZone The timezone.
 * @returns The current time in the specified timezone.
 */
export function getTimeZoneOffsetForTimezone(): Date {
	const timeZone = getTimeZoneInfo().timeZone;
	const date = new Date();
	const tzDate = new Date(date.toLocaleString('en-US', { timeZone }));
	const currentTime = dayjs(tzDate).tz(timeZone).toDate();
	return currentTime;
}

export function timeExpiredornot(dayString) {
	const toTimeZone = getTimeZoneInfo();
	const data = dayjs().tz(toTimeZone.timeZone);
	const dayStringInTimeZone = dayjs(dayString).tz(toTimeZone.timeZone);
	return data.isAfter(dayStringInTimeZone);
}

/**
 * Format date to US format (MM/dd/yyyy HH:mm:ss)
 * @param dateString - The date string to format
 * @returns Formatted date string in US format
 */
export function formatDateUS(dateString: string | Date): string {
	if (!dateString) return '';
	try {
		const date = dayjs(dateString);
		if (!date.isValid()) {
			return String(dateString);
		}
		// Format as MM/dd/yyyy HH:mm:ss (US format)
		return date.format(projectTenMinutesSsecondsDate);
	} catch {
		return String(dateString);
	}
}

/**
 * Format date to US format without time (MM/dd/yyyy)
 * @param dateString - The date string to format
 * @returns Formatted date string in US format (date only)
 */
export function formatDateUSOnly(dateString: string | Date): string {
	if (!dateString) return '';
	try {
		const date = dayjs(dateString);
		if (!date.isValid()) {
			return String(dateString);
		}
		// Format as MM/dd/yyyy (US format, date only)
		return date.format(projectDate);
	} catch {
		return String(dateString);
	}
}
