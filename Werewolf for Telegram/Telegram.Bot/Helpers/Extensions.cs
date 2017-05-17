﻿using System;
using System.Linq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Helpers
{
    public static class Extensions
    {
#if NET45
        private static readonly DateTime UnixStart = new DateTime(1970, 1, 1);
        /// <summary>
        ///   Convert a long into a DateTime
        /// </summary>
        public static DateTime FromUnixTime(this long dateTime) => UnixStart.AddSeconds(dateTime);
        
        /// <summary>
        ///   Convert a DateTime into a long
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static long ToUnixTime(this DateTime dateTime)
        {
            if (dateTime == DateTime.MinValue)
                return 0;

            var delta = dateTime - UnixStart;

            if (delta.TotalSeconds < 0) throw new ArgumentOutOfRangeException(nameof(dateTime), "Unix epoc starts January 1st, 1970");

            return (long) delta.TotalSeconds;
        }
#endif
    }
}
