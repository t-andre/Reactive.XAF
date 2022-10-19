using System;

namespace Xpand.Extensions.Numeric {
    public static partial class NumericExtensions {
        public static TimeSpan Seconds(this int seconds) => TimeSpan.FromSeconds(seconds);
    }
}