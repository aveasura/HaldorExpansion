using HaldorExpansion.Features.Cestus;
using HarmonyLib;
using UnityEngine;

namespace HaldorExpansion
{
    public partial class HaldorExpansionPlugin
    {

        [HarmonyPatch(typeof(Player), "GetTotalFoodValue")]
        private static class CestusMaxHealthPatch
        {
            private static void Postfix(Player __instance, ref float hp, ref float stamina, ref float eitr)
            {
                if (__instance == null)
                    return;

                if (CestusEquipment.HasEquippedCestus(__instance))
                    hp += CestusItemRegistration.GritCestusBonusMaxHealth;

                DebugLog(
                    $"[HaldorExpansion] Cestus HP bonus: equipped={CestusEquipment.HasEquippedCestus(__instance)}, hpBeforeBonus={hp}");
            }
        }
        
        [HarmonyPatch(typeof(Player), "GetBodyArmor")]
        private static class CestusArmorPenaltyPatch
        {
            private static void Postfix(Player __instance, ref float __result)
            {
                if (__instance == null)
                    return;

                bool equipped = CestusEquipment.HasEquippedCestus(__instance);
                if (equipped)
                    __result = Mathf.Max(0f, __result - CestusItemRegistration.GritCestusArmorPenalty);

                DebugLog($"[HaldorExpansion] Cestus armor penalty: equipped={equipped}, armorAfterPenalty={__result}");
            }
        }
    }
}