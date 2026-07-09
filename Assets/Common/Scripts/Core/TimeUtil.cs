using System;

namespace MergeCafe.Core
{
    public static class TimeUtil
    {
        /// <summary>
        /// Wall-clock Unix seconds. Used as the generator-energy clock so recovery
        /// keeps working across page reloads (webGL_game.md §11, §14).
        /// </summary>
        public static double NowUnixSeconds()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
        }
    }
}
