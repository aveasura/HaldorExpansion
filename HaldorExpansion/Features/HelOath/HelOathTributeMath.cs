using System;
using System.Globalization;

namespace HaldorExpansion.Features.HelOath
{
    // UTC expiry is the authority; the HUD effect never controls availability.
    internal static class HelOathTributeMath
    {
        internal const float CooldownSeconds = 180f;
        internal const float GraceSeconds = 3f;

        internal static float SavedHealth(float beforeHealth)
        {
            return float.IsNaN(beforeHealth) || float.IsInfinity(beforeHealth) || beforeHealth <= 0f
                ? 1f
                : Math.Max(1f, beforeHealth * 0.25f);
        }

        internal static long Expiry(long nowTicks)
        {
            return checked(nowTicks + 180L * TimeSpan.TicksPerSecond);
        }

        internal static float Remaining(string expiry, long nowTicks)
        {
            long ticks;
            if (!long.TryParse(expiry, NumberStyles.Integer, CultureInfo.InvariantCulture, out ticks) ||
                ticks <= nowTicks)
                return 0f;
            return (float)((ticks - (double)nowTicks) / TimeSpan.TicksPerSecond);
        }
    }
}