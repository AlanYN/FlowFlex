using System;
using System.Globalization;

namespace FlowFlex.Domain.Shared.Utils
{
    public static class WeekCalculator
    {
        /// <summary>
        /// Calculate current weekend time point in USA
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static DateTimeOffset GetCurrentWeekInUSA(this DateTime date)
        {
            Calendar calendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int dayOfWeek = (int)calendar.GetDayOfWeek(date);
            DateTimeOffset now = DateTimeOffset.Now;
            return new DateTimeOffset(now.Year, now.Month, now.Day, 23, 59, 59, TimeSpan.Zero).AddDays(7 - dayOfWeek - 1);
        }

        /// <summary>
        /// Calculate next week weekend time point in USA
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static DateTimeOffset GetNextWeekInUSA(this DateTime date)
        {
            Calendar calendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int dayOfWeek = (int)calendar.GetDayOfWeek(date);
            DateTimeOffset now = DateTimeOffset.Now;
            return new DateTimeOffset(now.Year, now.Month, now.Day, 23, 59, 59, TimeSpan.Zero).AddDays(14 - dayOfWeek - 1);
        }

        /// <summary>
        /// Calculate next week first day time point in USA
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static DateTimeOffset GetNextWeekOfDayInUSA(this DateTime date)
        {
            Calendar calendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int dayOfWeek = (int)calendar.GetDayOfWeek(date);
            DateTimeOffset now = DateTimeOffset.Now;
            return new DateTimeOffset(now.Year, now.Month, now.Day, 00, 00, 00, TimeSpan.Zero).AddDays(7 - dayOfWeek);
        }
    }
}
