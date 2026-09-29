using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NGKBusi.Helpers
{
    public class Convertion
    {
        public static string ConvertSecondsToDHMS(int seconds)
        {
            int oneDay = 24 * 60 * 60;
            int oneHour = 60 * 60;
            int oneMinute = 60;

            int days = seconds / oneDay;
            int hours = (seconds % oneDay) / oneHour;
            int minutes = (seconds % oneHour) / oneMinute;
            int remainingSeconds = seconds % oneMinute;

            string result = "";
            if (days > 0)
            {
                result += days + " days, ";
            }
            if (hours > 0 || (days == 0 && minutes == 0))
            {
                result += hours + " hours, ";
            }
            if (minutes > 0 || (days == 0 && hours == 0))
            {
                result += minutes + " minutes, ";
            }
            result += remainingSeconds + " seconds";

            return result;
        }
    }
}