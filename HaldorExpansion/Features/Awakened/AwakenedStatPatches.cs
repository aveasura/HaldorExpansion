using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace HaldorExpansion.Features.Awakened
{
    internal static class AwakenedStats
    {
        private static readonly FieldInfo CharacterField = AccessTools.Field(typeof(SEMan), "m_character");

        private static readonly List<HitData.DamageModPair> SlashResistance = new List<HitData.DamageModPair>
        {
            new HitData.DamageModPair
            {
                m_type = HitData.DamageType.Slash, m_modifier = AwakenedBalance.ScorchedEnduranceCloak.SlashResistance
            }
        };

        internal static Player Owner(SEMan manager) =>
            manager == null ? null : CharacterField?.GetValue(manager) as Player;

        internal static void Food(Player player, ref float hp, ref float stamina)
        {
            if (AwakenedEquipment.Has(player, Artifact.WoundedBeastCloak))
                hp += AwakenedBalance.WoundedBeastCloak.Health;
            if (AwakenedEquipment.Has(player, Artifact.HelheimPelt)) stamina += AwakenedBalance.HelheimPelt.Stamina;
        }

        internal static void Skill(Player player, Skills.SkillType skill, ref float level)
        {
            float bonus = 0f;
            if (skill == Skills.SkillType.Unarmed && AwakenedEquipment.Has(player, Artifact.WoundedBeastCloak))
                bonus = AwakenedBalance.WoundedBeastCloak.Unarmed;
            if (AwakenedEquipment.Has(player, Artifact.ShadowCrossbow))
            {
                if (skill == Skills.SkillType.Sneak) bonus += AwakenedBalance.ShadowCrossbow.Sneak;
                if (skill == Skills.SkillType.Crossbows) bonus += AwakenedBalance.ShadowCrossbow.Crossbows;
            }

            if (bonus > 0f) level = Mathf.Min(AwakenedBalance.SkillCap, level + bonus);
        }

        internal static void Armor(Player player, ref HitData.DamageModifiers mods)
        {
            // Native Apply uses ApplyIfBetter: stronger resistance/immunity is retained.
            if (AwakenedEquipment.Has(player, Artifact.ScorchedEnduranceCloak)) mods.Apply(SlashResistance);
        }

        internal static void Fall(Player player, ref float damage)
        {
            if (AwakenedEquipment.Has(player, Artifact.HelheimPelt))
                damage *= 1f - AwakenedBalance.HelheimPelt.FallDamageReduction;
        }

        internal static void StaminaRegen(Player player, ref float multiplier)
        {
            if (AwakenedEquipment.Has(player, Artifact.Brisingamen))
                multiplier *= 1f + AwakenedBalance.Brisingamen.StaminaRegenBonus;
        }
    }

    [HarmonyPatch(typeof(Player), "GetTotalFoodValue")]
    internal static class AwakenedFoodPatch
    {
        // Run before Hel Oath's Priority.Last health sacrifice/stamina conversion.
        private static void Postfix(Player __instance, ref float hp, ref float stamina) =>
            AwakenedStats.Food(__instance, ref hp, ref stamina);
    }

    [HarmonyPatch(typeof(SEMan), "ModifySkillLevel")]
    internal static class AwakenedSkillPatch
    {
        // GetSkillLevel feeds both deterministic factors and random attack factors/ranges.
        // Only its local value changes; stored skill level and XP remain untouched.
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(SEMan __instance, Skills.SkillType skill, ref float level) =>
            AwakenedStats.Skill(AwakenedStats.Owner(__instance), skill, ref level);
    }

    [HarmonyPatch(typeof(Player), "ApplyArmorDamageMods")]
    internal static class AwakenedArmorPatch
    {
        private static void Postfix(Player __instance, ref HitData.DamageModifiers mods) =>
            AwakenedStats.Armor(__instance, ref mods);
    }

    [HarmonyPatch(typeof(SEMan), "ModifyFallDamage")]
    internal static class AwakenedFallPatch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(SEMan __instance, ref float damage) =>
            AwakenedStats.Fall(AwakenedStats.Owner(__instance), ref damage);
    }

    [HarmonyPatch(typeof(SEMan), "ModifyStaminaRegen")]
    internal static class AwakenedStaminaRegenPatch
    {
        // Multiply the native modifier inside its normal delay and action gates; never add stamina directly.
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(SEMan __instance, ref float staminaMultiplier) =>
            AwakenedStats.StaminaRegen(AwakenedStats.Owner(__instance), ref staminaMultiplier);
    }
}