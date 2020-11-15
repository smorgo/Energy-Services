using System;
using System.Globalization;

namespace OctopusAgileMonitor.Extensions
{
    public static class DateTimeExtensions
    {
        public static string ToIso8601(this DateTime t)
        {
            return t.ToString("yyyy-MM-dd'T'HH:mmK", CultureInfo.InvariantCulture);
        }
    }
}