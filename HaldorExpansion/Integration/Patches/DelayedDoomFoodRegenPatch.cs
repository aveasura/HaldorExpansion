using HaldorExpansion.Compatibility;
using HaldorExpansion.Diagnostics;
using HaldorExpansion.Features.DelayedDoom;
using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace HaldorExpansion.Patches
{
    [HarmonyPatch(typeof(Player), "UpdateFood", new Type[] { typeof(float), typeof(bool) })]
    internal static class DelayedDoomFoodRegenPatch
    {
        private sealed class FoodRegenSnapshot
        {
            public Player Player;
            public bool Enabled;
            public float BeforeHealth;
            public float BaseFoodRegen;
            public float Dt;
            public bool ForceUpdate;
        }

        private static readonly Stack<FoodRegenSnapshot> Pending = new Stack<FoodRegenSnapshot>();

        static void Prefix(Player __instance, float dt, bool forceUpdate)
        {
            FoodRegenSnapshot snapshot = new FoodRegenSnapshot
            {
                Player = __instance,
                Dt = dt,
                ForceUpdate = forceUpdate,
                Enabled = false,
                BeforeHealth = 0f,
                BaseFoodRegen = 0f
            };

            if (__instance != null &&
                DelayedDoomEquipment.HasDelayedDoomEquipped(__instance) &&
                dt > 0f &&
                !forceUpdate)
            {
                snapshot.Enabled = true;
                snapshot.BeforeHealth = CharacterHealthAccess.GetCharacterHealth(__instance);
                snapshot.BaseFoodRegen = DelayedDoomFoodRegen.GetPlayerBaseFoodRegen(__instance);
            }

            Pending.Push(snapshot);
        }

        static void Postfix(Player __instance, float dt, bool forceUpdate)
        {
            if (Pending.Count == 0)
                return;

            FoodRegenSnapshot snapshot = Pending.Pop();

            if (!snapshot.Enabled || __instance == null)
                return;

            if (snapshot.BaseFoodRegen <= 0.001f)
                return;

            float afterHealth = CharacterHealthAccess.GetCharacterHealth(__instance);
            float healedByFoodTick = afterHealth - snapshot.BeforeHealth;

            if (healedByFoodTick <= 0.001f)
                return;

            float regenScale = healedByFoodTick / snapshot.BaseFoodRegen;
            regenScale = Mathf.Clamp(regenScale, 0f, 10f);

            float armorBonus = DelayedDoomItemRegistration.DelayedDoomChestFoodRegenBonus * regenScale;
            if (armorBonus <= 0.001f)
                return;

            float maxHealth = CharacterHealthAccess.GetCharacterMaxHealth(__instance);
            float newHealth = Mathf.Min(maxHealth, afterHealth + armorBonus);

            if (newHealth <= afterHealth + 0.001f)
                return;

            CharacterHealthAccess.SetCharacterHealth(__instance, newHealth);

            HaldorExpansionPlugin.DebugLog(
                $"[HaldorExpansion] Food regen bonus applied: " +
                $"baseFoodRegen={snapshot.BaseFoodRegen:0.##}, " +
                $"foodHeal={healedByFoodTick:0.##}, " +
                $"scale={regenScale:0.##}, " +
                $"bonus={armorBonus:0.##}, " +
                $"after={afterHealth:0.##}, " +
                $"new={newHealth:0.##}, " +
                $"{PlayerDiagnostics.GetCestusDebugPlayerTag(__instance)}");
        }
    }
}