using System;
using System.Globalization;

namespace AgileConfig.Client.Utils
{
    internal class StringUtil
    {
        /// <summary>
        /// 字符串比较器（不区分文化&大小写）
        /// </summary>
        public static StringComparer StringComparerWithInvariantCulture { get; } = StringComparer.Create(CultureInfo.InvariantCulture, true);
    }
}
