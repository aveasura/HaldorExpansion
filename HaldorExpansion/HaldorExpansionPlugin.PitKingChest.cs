using System;
using System.Collections.Generic;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace HaldorExpansion
{
    public partial class HaldorExpansionPlugin
    {
        internal const string PitKingChestPrefabName = "ArmorPitKingChest";
        internal const string PitKingChestSourcePrefab = "ArmorMageChest";
        internal const int PitKingChestPrice = 4000;

        internal const float PitKingChestBonusMaxHealth = 70f;

        private const string PitKingChestItemKey = "$item_chest_pitking";
        private const string PitKingChestItemDescKey = "$item_chest_pitking_desc";

        private const string PitKingChestEffectNameKey = "$se_pitking_vigor";
        private const string PitKingChestEffectDescKey = "$se_pitking_vigor_desc";

        private const string SteelHeartSetNameKey = "$itemset_steelheart";
        private const string SteelHeartSetDescKey = "$itemset_steelheart_desc";

        private static readonly HashSet<string> PitKingChestItemKeys = new HashSet<string>
        {
            PitKingChestItemKey
        };

        internal static GameObject PitKingChestPrefab;
        internal static ItemDrop PitKingChestItemDrop;

        private static void ApplyPitKingChestVisualEffect(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null)
                return;

            SE_Stats se = ScriptableObject.CreateInstance<SE_Stats>();
            se.name = "SE_PitKingChestVisual";

            SetFieldIfExists(se, "m_name", PitKingChestEffectNameKey);
            SetFieldIfExists(se, "m_tooltip", PitKingChestEffectDescKey);

            shared.m_equipStatusEffect = se;
        }

        internal static void ApplySteelHeartSet(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null)
                return;

            SE_Stats se = ScriptableObject.CreateInstance<SE_Stats>();
            se.name = "SE_SteelHeartSet";

            SetFieldIfExists(se, "m_name", SteelHeartSetNameKey);
            SetFieldIfExists(se, "m_tooltip", SteelHeartSetDescKey);

            shared.m_setName = SteelHeartSetNameKey;
            shared.m_setSize = 2;
            shared.m_setStatusEffect = se;
        }

        internal static bool EnsurePitKingChestReady()
        {
            try
            {
                if (PitKingChestItemDrop != null && PitKingChestPrefab != null)
                    return true;

                if (ObjectDB.instance == null)
                    return false;

                GameObject existing = ObjectDB.instance.GetItemPrefab(PitKingChestPrefabName);
                if (existing != null)
                {
                    PitKingChestPrefab = existing;
                    PitKingChestItemDrop = existing.GetComponent<ItemDrop>();

                    if (PitKingChestItemDrop != null)
                    {
                        ConfigurePitKingChestShared(PitKingChestItemDrop.m_itemData.m_shared);
                        ApplyPitKingChestIcon(PitKingChestItemDrop.m_itemData.m_shared);
                    }

                    DebugLog($"[HaldorExpansion] Pit King chest already in ObjectDB: {PitKingChestPrefabName}");
                    return PitKingChestItemDrop != null;
                }

                GameObject source = ObjectDB.instance.GetItemPrefab(PitKingChestSourcePrefab);
                if (source == null)
                {
                    Log?.LogWarning($"[HaldorExpansion] Source chest prefab not found: {PitKingChestSourcePrefab}");
                    return false;
                }

                GameObject chestGo = PrefabManager.Instance.CreateClonedPrefab(PitKingChestPrefabName, source.name);
                if (chestGo == null)
                {
                    Log?.LogWarning("[HaldorExpansion] Failed to clone Pit King chest prefab");
                    return false;
                }

                ItemDrop drop = chestGo.GetComponent<ItemDrop>();
                if (drop == null)
                {
                    Log?.LogWarning("[HaldorExpansion] ItemDrop missing on cloned Pit King chest prefab");
                    return false;
                }

                ConfigurePitKingChestShared(drop.m_itemData.m_shared);
                ApplyPitKingChestIcon(drop.m_itemData.m_shared);
                drop.m_itemData.m_dropPrefab = chestGo;

                ItemManager.Instance.AddItem(new CustomItem(chestGo, true));

                PitKingChestPrefab = chestGo;
                PitKingChestItemDrop = drop;

                DebugLog(
                    $"[HaldorExpansion] Pit King chest registered: {PitKingChestPrefabName} (source={source.name})");
                return true;
            }
            catch (Exception e)
            {
                Log?.LogError($"[HaldorExpansion] EnsurePitKingChestReady error: {e}");
                return false;
            }
        }

        private static void ConfigurePitKingChestShared(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null)
                return;

            shared.m_name = PitKingChestItemKey;
            shared.m_description = PitKingChestItemDescKey;

            shared.m_armor = 12f;
            shared.m_maxQuality = 1;
            shared.m_weight = 5f;
            shared.m_value = 0;

            shared.m_maxDurability = 2100f;
            shared.m_durabilityPerLevel = 0f;

            shared.m_useDurability = true;
            shared.m_canBeReparied = true;

            shared.m_eitrRegenModifier = 0f;
            shared.m_movementModifier = -0.05f;

            // обнулить унаследованный набор
            shared.m_setName = "";
            shared.m_setSize = 0;
            shared.m_setStatusEffect = null;

            ApplyPitKingChestVisualEffect(shared);
            ApplySteelHeartSet(shared);
        }

        internal static bool HasPitKingChestEquipped(Player player)
        {
            if (player == null)
                return false;

            Inventory inventory = player.GetInventory();
            if (inventory == null)
                return false;

            List<ItemDrop.ItemData> items = inventory.GetAllItems();
            if (items == null)
                return false;

            foreach (ItemDrop.ItemData item in items)
            {
                if (item == null || !item.m_equipped || item.m_shared == null)
                    continue;

                string itemKey = item.m_shared.m_name;
                if (!string.IsNullOrEmpty(itemKey) && PitKingChestItemKeys.Contains(itemKey))
                    return true;
            }

            return false;
        }

        internal static void RegisterPitKingChestRecipe()
        {
            try
            {
                if (ItemManager.Instance.GetRecipe("Recipe_" + PitKingChestPrefabName) != null)
                {
                    DebugLog($"[HaldorExpansion] Recipe already registered: Recipe_{PitKingChestPrefabName}");
                    return;
                }

                GameObject sourcePrefab = ObjectDB.instance?.GetItemPrefab(PitKingChestSourcePrefab);
                if (sourcePrefab == null)
                {
                    Log?.LogWarning("[HaldorExpansion] Source prefab not found for Pit King recipe");
                    return;
                }

                ItemDrop sourceDrop = sourcePrefab.GetComponent<ItemDrop>();
                if (sourceDrop == null)
                {
                    Log?.LogWarning("[HaldorExpansion] Source ItemDrop not found for Pit King recipe");
                    return;
                }

                Recipe sourceRecipe = ObjectDB.instance.GetRecipe(sourceDrop.m_itemData);
                if (sourceRecipe == null)
                {
                    Log?.LogWarning("[HaldorExpansion] Source recipe not found for Pit King chest");
                    return;
                }

                string craftingStation = sourceRecipe.m_craftingStation != null
                    ? sourceRecipe.m_craftingStation.name
                    : null;

                string repairStation = "forge";

                var recipeConfig = new RecipeConfig
                {
                    Name = "Recipe_" + PitKingChestPrefabName,
                    Item = PitKingChestPrefabName,
                    Amount = 1,
                    CraftingStation = craftingStation,
                    RepairStation = repairStation,
                    MinStationLevel = 1,
                    Enabled = false,
                    Requirements = Array.Empty<RequirementConfig>()
                };

                ItemManager.Instance.AddRecipe(new CustomRecipe(recipeConfig));
                DebugLog($"[HaldorExpansion] Registered recipe via Jotunn for {PitKingChestPrefabName}");
            }
            catch (Exception e)
            {
                Log?.LogError($"[HaldorExpansion] RegisterPitKingChestRecipe error: {e}");
            }
        }

        [HarmonyPatch(typeof(Player), "GetTotalFoodValue")]
        private static class PitKingChestMaxHealthPatch
        {
            private static void Postfix(Player __instance, ref float hp, ref float stamina, ref float eitr)
            {
                if (__instance == null)
                    return;

                bool equipped = HasPitKingChestEquipped(__instance);
                if (equipped)
                    hp += PitKingChestBonusMaxHealth;

                DebugLog($"[HaldorExpansion] Pit King chest HP bonus: equipped={equipped}, hpAfterBonus={hp:0.##}");
            }
        }
        
        private static void ApplyPitKingChestIcon(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null)
                return;

            if (!TrySetEmbeddedIcon(shared, "pit_king_chest.png"))
            {
                Log?.LogWarning("[HaldorExpansion] Embedded icon missing for Pit King Chest: pit_king_chest.png");
            }
        }
    }
}