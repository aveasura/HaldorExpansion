using System.Reflection;
using HarmonyLib;

namespace HaldorExpansion.Features.HelOath
{
    internal static class HelOathDotTracking
    {
        private static readonly MethodInfo NativeDamage = AccessTools.Method(typeof(Character), "RPC_Damage");

        // Both compiled callers use null creditSource: outgoing payload credit already
        // includes DoT damage. Keep the native Harmony-patched owner-side damage route,
        // without global status-effect hooks or dormant ownership/tick state.
        internal static void Apply(Character target, long sender, HitData hit, string creditSource)
        {
            NativeDamage.Invoke(target, new object[] { sender, hit });
        }
    }
}