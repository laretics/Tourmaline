using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tourmaline.Common
{
    internal class Conversions
    {
    }


    /// <summary>
    /// Class to convert various quantities (so a value with a unit) into nicely formatted strings for display
    /// </summary>
    public static class FormatStrings
    {
        //public static GettextResourceManager Catalog = new GettextResourceManager("ORTS.Common");

        /// <summary>
        /// Converts duration in floating-point seconds to whole hours, minutes and seconds (rounded down).
        /// </summary>
        /// <param name="clockTimeSeconds"></param>
        /// <returns>The time in HH:MM:SS format.</returns>
        public static string FormatTime(double clockTimeSeconds)
        {
            var hour = (int)(clockTimeSeconds / (60 * 60));
            clockTimeSeconds -= hour * 60 * 60;
            var minute = (int)(clockTimeSeconds / 60);
            clockTimeSeconds -= minute * 60;
            var seconds = (int)clockTimeSeconds;

            // Reset clock before and after midnight
            if (hour >= 24)
                hour %= 24;
            if (hour < 0)
                hour += 24;
            if (minute < 0)
                minute += 60;
            if (seconds < 0)
                seconds += 60;

            return string.Format("{0:D2}:{1:D2}:{2:D2}", hour, minute, seconds);
        }

        /// <summary>
        /// Converts duration in floating-point seconds to whole hours, minutes and seconds and 2 decimal places of seconds.
        /// </summary>
        /// <param name="clockTimeSeconds"></param>
        /// <returns>The time in HH:MM:SS.SS format.</returns>
        public static string FormatPreciseTime(double clockTimeSeconds)
        {
            var hour = (int)(clockTimeSeconds / (60 * 60));
            clockTimeSeconds -= hour * 60 * 60;
            var minute = (int)(clockTimeSeconds / 60);
            clockTimeSeconds -= minute * 60;
            var seconds = clockTimeSeconds;

            // Reset clock before and after midnight
            if (hour >= 24)
                hour %= 24;
            if (hour < 0)
                hour += 24;
            if (minute < 0)
                minute += 60;
            if (seconds < 0)
                seconds += 60;

            return string.Format("{0:D2}:{1:D2}:{2:00.00}", hour, minute, seconds);
        }
        public static string FormatPreciseTime(DateTime rhs)
        {
            return string.Format("{0:D2}:{1:D2}:{2:00.00}", rhs.Hour, rhs.Minute, rhs.Second);
        }


        /// <summary>
        /// Converts duration in floating-point seconds to whole hours and minutes (rounded to nearest).
        /// </summary>
        /// <param name="clockTimeSeconds"></param>
        /// <returns>The time in HH:MM format.</returns>
        public static string FormatApproximateTime(double clockTimeSeconds)
        {
            var hour = (int)(clockTimeSeconds / (60 * 60));
            clockTimeSeconds -= hour * 60 * 60;
            var minute = (int)Math.Round(clockTimeSeconds / 60);
            clockTimeSeconds -= minute * 60;

            // Reset clock before and after midnight
            if (hour >= 24)
                hour %= 24;
            if (hour < 0)
                hour += 24;
            if (minute < 0)
                minute += 60;

            return string.Format("{0:D2}:{1:D2}", hour, minute);
        }
    }
}
