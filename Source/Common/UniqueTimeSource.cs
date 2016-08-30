using System;

namespace Common
{
    public static class UniqueTimeSource
    {
        #region Private 

        private static object _lockobj = new object();
        private static long _last;
        private static long _offset;

        #endregion

        public static DateTime StartTime { get; private set; }

        static UniqueTimeSource()
        {
            StartTime = GetUniqueTime();
        }

        /// <summary>
        /// Returns the a current time unique for executing appdomain.
        /// </summary>
        /// <returns>A UTC datetime value.</returns>
        public static DateTime GetUniqueTime()
        {
            lock (_lockobj)
            {
                var t = DateTime.UtcNow.Ticks;
                if (t == _last)
                {
                    t = _last + ++_offset;
                }
                else
                {
                    _last = t;
                    _offset = 0;
                }

                return new DateTime(t, DateTimeKind.Utc);
            }
        }

        public static string ToElapsedTime(this DateTime t)
        {
            var d = (t - UniqueTimeSource.StartTime).TotalSeconds;
            string r;
            if (d >= 0)
            {
                r = d.ToString("0.0000000");
            }
            else
            {
                r = "(primord)";
            }

            r = "T+" + r.PadLeft(10);
            return r;
        }

    }
}
