namespace HaldorExpansion.Features.HelOath
{
    /// <summary>
    /// Reads vanilla world boss keys and applies the pure Hel's Embrace progression curve.
    /// The configured damage value is the Elder-tier baseline (x1.00).
    /// </summary>
    internal static class HelOathWorldProgression
    {
        internal static float CurrentMultiplier
        {
            get
            {
                ZoneSystem zone = ZoneSystem.instance;
                if (zone == null)
                    return HelOathProgressionMath.ElderMultiplier;

                try
                {
                    return HelOathProgressionMath.ResolveMultiplier(key => zone.GetGlobalKey(key));
                }
                catch
                {
                    // During world load/unload keep the configured baseline rather than
                    // temporarily making charge either much cheaper or more expensive.
                    return HelOathProgressionMath.ElderMultiplier;
                }
            }
        }

        internal static float ScaleRequiredDamage(float baseDamage)
        {
            return HelOathProgressionMath.ScaleRequiredDamage(baseDamage, CurrentMultiplier);
        }
    }
}
