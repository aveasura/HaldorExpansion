using HaldorExpansion.Features.PitKing;
using HarmonyLib;

namespace HaldorExpansion
{
    public partial class HaldorExpansionPlugin
    {

        [HarmonyPatch(typeof(Player), "GetTotalFoodValue")]
        private static class PitKingChestMaxHealthPatch
        {
            private static void Postfix(Player __instance, ref float hp, ref float stamina, ref float eitr)
            {
                if (__instance == null)
                    return;

                bool equipped = PitKingEquipment.HasPitKingChestEquipped(__instance);
                if (equipped)
                    hp += PitKingItemRegistration.PitKingChestBonusMaxHealth;

                DebugLog($"[HaldorExpansion] Pit King chest HP bonus: equipped={equipped}, hpAfterBonus={hp:0.##}");
            }
        }
    }
}