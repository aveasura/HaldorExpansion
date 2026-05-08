using System;
using System.Collections.Generic;
using System.Reflection;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace HaldorExpansion
{
    public partial class HaldorExpansionPlugin
    {
        // С Кирасой безмолвной расплаты эффект хила от плаща в 2 раза слабее
        private const float WoundedBeastCapeRegenWithDelayedDoomMultiplier = 1f / 2f; 
        
        internal const string WoundedBeastCapePrefabName = "CapeWoundedBeast";
        internal const string WoundedBeastCapeSourcePrefab = "CapeWolf";
        internal const int WoundedBeastCapePrice = 2500;

        internal const string WoundedBeastCapeItemKey = "$item_cape_woundedbeast";
        private const string WoundedBeastCapeItemDescKey = "$item_cape_woundedbeast_desc";
        private const string WoundedBeastCapeEffectNameKey = "$se_cape_woundedbeast";
        private const string WoundedBeastCapeEffectDescKey = "$se_cape_woundedbeast_desc";

        internal const float WoundedBeastCapeIncomingDamageMultiplier = 1.15f;
        internal const float WoundedBeastCapeDotRegenSuppressDuration = 2f;
        private const float WoundedBeastCapeRegenTickInterval = 1f;

        internal static GameObject WoundedBeastCapePrefab;
        internal static ItemDrop WoundedBeastCapeItemDrop;

        private static readonly Dictionary<long, float> WoundedBeastCapeNextRegenTickTimes = new Dictionary<long, float>();
        private static readonly Dictionary<long, float> WoundedBeastCapeRegenSuppressedUntil = new Dictionary<long, float>();

        internal static void ResetWoundedBeastCapeState()
        {
            WoundedBeastCapePrefab = null;
            WoundedBeastCapeItemDrop = null;
            WoundedBeastCapeNextRegenTickTimes.Clear();
            WoundedBeastCapeRegenSuppressedUntil.Clear();
        }

        internal static bool HasWoundedBeastCapeEquipped(Player player)
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

                if (item.m_shared.m_name == WoundedBeastCapeItemKey)
                    return true;
            }

            return false;
        }

        internal static bool EnsureWoundedBeastCapeReady()
        {
            try
            {
                if (ObjectDB.instance == null)
                    return false;

                GameObject existing = ObjectDB.instance.GetItemPrefab(WoundedBeastCapePrefabName);
                if (existing != null)
                {
                    WoundedBeastCapePrefab = existing;
                    WoundedBeastCapeItemDrop = existing.GetComponent<ItemDrop>();

                    if (WoundedBeastCapeItemDrop != null)
                    {
                        ConfigureWoundedBeastCapeShared(WoundedBeastCapeItemDrop.m_itemData.m_shared);
                        WoundedBeastCapeItemDrop.m_itemData.m_dropPrefab = existing;
                    }

                    DebugLog($"[HaldorExpansion] Wounded Beast cape already in ObjectDB: {WoundedBeastCapePrefabName}");
                    return WoundedBeastCapeItemDrop != null;
                }

                GameObject source = ObjectDB.instance.GetItemPrefab(WoundedBeastCapeSourcePrefab);
                if (source == null)
                {
                    Log?.LogWarning($"[HaldorExpansion] Source cape prefab not found: {WoundedBeastCapeSourcePrefab}");
                    return false;
                }

                GameObject capeGo = PrefabManager.Instance.CreateClonedPrefab(WoundedBeastCapePrefabName, source.name);
                if (capeGo == null)
                {
                    Log?.LogWarning("[HaldorExpansion] Failed to clone Wounded Beast cape prefab");
                    return false;
                }

                ItemDrop drop = capeGo.GetComponent<ItemDrop>();
                if (drop == null)
                {
                    Log?.LogWarning("[HaldorExpansion] ItemDrop missing on Wounded Beast cape");
                    return false;
                }

                ConfigureWoundedBeastCapeShared(drop.m_itemData.m_shared);
                drop.m_itemData.m_dropPrefab = capeGo;

                ItemManager.Instance.AddItem(new CustomItem(capeGo, true));
                EnsureWoundedBeastCapeRegisteredInObjectDB(capeGo);

                WoundedBeastCapePrefab = capeGo;
                WoundedBeastCapeItemDrop = drop;

                DebugLog($"[HaldorExpansion] Wounded Beast cape registered: {WoundedBeastCapePrefabName} (source={source.name})");
                return true;
            }
            catch (Exception e)
            {
                Log?.LogError($"[HaldorExpansion] EnsureWoundedBeastCapeReady error: {e}");
                return false;
            }
        }

        internal static void RegisterWoundedBeastCapeRecipe()
        {
            try
            {
                string recipeName = "Recipe_" + WoundedBeastCapePrefabName;
                if (ItemManager.Instance.GetRecipe(recipeName) != null)
                {
                    DebugLog($"[HaldorExpansion] Recipe already registered: {recipeName}");
                    return;
                }

                GameObject sourcePrefab = ObjectDB.instance?.GetItemPrefab(WoundedBeastCapeSourcePrefab);
                if (sourcePrefab == null)
                {
                    Log?.LogWarning("[HaldorExpansion] Source cape prefab not found for Wounded Beast recipe");
                    return;
                }

                ItemDrop sourceDrop = sourcePrefab.GetComponent<ItemDrop>();
                if (sourceDrop == null)
                {
                    Log?.LogWarning("[HaldorExpansion] Source cape ItemDrop not found for Wounded Beast recipe");
                    return;
                }

                Recipe sourceRecipe = ObjectDB.instance.GetRecipe(sourceDrop.m_itemData);
                if (sourceRecipe == null)
                {
                    Log?.LogWarning("[HaldorExpansion] Source cape recipe not found for Wounded Beast cape");
                    return;
                }

                string craftingStation = sourceRecipe.m_craftingStation != null
                    ? sourceRecipe.m_craftingStation.name
                    : null;

                string repairStation = sourceRecipe.m_repairStation != null
                    ? sourceRecipe.m_repairStation.name
                    : (craftingStation ?? "forge");

                var recipeConfig = new RecipeConfig
                {
                    Name = recipeName,
                    Item = WoundedBeastCapePrefabName,
                    Amount = 1,
                    CraftingStation = craftingStation,
                    RepairStation = repairStation,
                    MinStationLevel = 1,
                    Enabled = false,
                    Requirements = Array.Empty<RequirementConfig>()
                };

                ItemManager.Instance.AddRecipe(new CustomRecipe(recipeConfig));
                DebugLog($"[HaldorExpansion] Registered recipe via Jotunn for {WoundedBeastCapePrefabName}");
            }
            catch (Exception e)
            {
                Log?.LogError($"[HaldorExpansion] RegisterWoundedBeastCapeRecipe error: {e}");
            }
        }

        internal static bool FixWoundedBeastCapeItemsInInventory(Player player)
        {
            try
            {
                if (player == null || ObjectDB.instance == null)
                    return false;

                GameObject capePrefab = ObjectDB.instance.GetItemPrefab(WoundedBeastCapePrefabName);
                if (capePrefab == null)
                    return false;

                ItemDrop prefabDrop = capePrefab.GetComponent<ItemDrop>();
                if (prefabDrop == null)
                    return false;

                Inventory inventory = player.GetInventory();
                if (inventory == null)
                    return false;

                bool found = false;
                List<ItemDrop.ItemData> items = inventory.GetAllItems();
                if (items == null)
                    return false;

                foreach (ItemDrop.ItemData item in items)
                {
                    if (item == null || item.m_shared == null)
                        continue;

                    if (item.m_shared.m_name != WoundedBeastCapeItemKey)
                        continue;

                    found = true;

                    int oldQuality = Mathf.Max(1, item.m_quality);
                    float oldDurability = item.m_durability;
                    bool oldEquipped = item.m_equipped;
                    int oldVariant = item.m_variant;
                    long oldCrafterID = item.m_crafterID;
                    string oldCrafterName = item.m_crafterName;

                    item.m_dropPrefab = capePrefab;
                    item.m_shared = prefabDrop.m_itemData.m_shared;

                    item.m_quality = Mathf.Clamp(oldQuality, 1, Mathf.Max(1, item.m_shared.m_maxQuality));
                    item.m_variant = oldVariant;
                    item.m_crafterID = oldCrafterID;
                    item.m_crafterName = oldCrafterName;
                    item.m_equipped = oldEquipped;

                    float maxDurability = item.m_shared.m_maxDurability + item.m_shared.m_durabilityPerLevel * (item.m_quality - 1);
                    item.m_durability = Mathf.Clamp(oldDurability, 0f, Mathf.Max(1f, maxDurability));

                    item.m_shared.m_canBeReparied = true;
                    item.m_shared.m_useDurability = true;
                }

                return found;
            }
            catch (Exception e)
            {
                Log?.LogError($"[HaldorExpansion] FixWoundedBeastCapeItemsInInventory error: {e}");
                return false;
            }
        }

        internal static void UpdateWoundedBeastCapeRegen(Player player)
        {
            if (player == null)
                return;

            long playerId = player.GetPlayerID();

            if (!HasWoundedBeastCapeEquipped(player) || player.IsDead())
            {
                WoundedBeastCapeNextRegenTickTimes.Remove(playerId);
                return;
            }

            float now = Time.time;
            if (WoundedBeastCapeRegenSuppressedUntil.TryGetValue(playerId, out float suppressedUntil) && now < suppressedUntil)
                return;

            if (!WoundedBeastCapeNextRegenTickTimes.TryGetValue(playerId, out float nextTick) || now >= nextTick)
            {
                WoundedBeastCapeNextRegenTickTimes[playerId] = now + WoundedBeastCapeRegenTickInterval;
                TryApplyWoundedBeastCapeRegenTick(player);
            }
        }

        internal static void ApplyWoundedBeastCapeIncomingDamagePenalty(Player player, HitData hit)
        {
            if (player == null || hit == null || !HasWoundedBeastCapeEquipped(player))
                return;

            ScaleWoundedBeastCapeDamageTypes(ref hit.m_damage, WoundedBeastCapeIncomingDamageMultiplier);

            if (HasWoundedBeastCapeSuppressionDamage(hit))
            {
                long playerId = player.GetPlayerID();
                WoundedBeastCapeRegenSuppressedUntil[playerId] = Time.time + WoundedBeastCapeDotRegenSuppressDuration;
            }
        }

        private static void TryApplyWoundedBeastCapeRegenTick(Player player)
        {
            float currentHealth = GetCharacterHealth(player);
            float maxHealth = GetCharacterMaxHealth(player);

            if (currentHealth <= 0f || maxHealth <= 0f || currentHealth >= maxHealth)
                return;

            float regenAmount = GetWoundedBeastCapeRegenPerSecond(currentHealth, maxHealth);
            if (regenAmount <= 0f)
                return;

            if (HasDelayedDoomEquipped(player))
            {
                regenAmount *= WoundedBeastCapeRegenWithDelayedDoomMultiplier;
            }

            float newHealth = Mathf.Min(maxHealth, currentHealth + regenAmount);
            if (newHealth <= currentHealth + 0.001f)
                return;

            SetCharacterHealth(player, newHealth);

            DebugLog(
                $"[HaldorExpansion] Wounded Beast regen tick: " +
                $"current={currentHealth:0.##}, " +
                $"max={maxHealth:0.##}, " +
                $"regen={regenAmount:0.##}, " +
                $"new={newHealth:0.##}, " +
                $"delayedDoom={HasDelayedDoomEquipped(player)}");
        }

        private static float GetWoundedBeastCapeRegenPerSecond(float currentHealth, float maxHealth)
        {
            float hpFraction = currentHealth / maxHealth;

            if (hpFraction > 0.80f) return 0f;
            if (hpFraction > 0.70f) return 1f;
            if (hpFraction > 0.60f) return 2f;
            if (hpFraction > 0.55f) return 3f;
            if (hpFraction > 0.40f) return 5f;
            if (hpFraction > 0.20f) return 7f;
            return 10f;
        }

        private static bool HasWoundedBeastCapeSuppressionDamage(HitData hit)
        {
            HitData.DamageTypes damage = hit.m_damage;
            return damage.m_fire > 0f || damage.m_poison > 0f || damage.m_spirit > 0f;
        }

        private static void ScaleWoundedBeastCapeDamageTypes(ref HitData.DamageTypes damage, float multiplier)
        {
            damage.m_damage *= multiplier;
            damage.m_blunt *= multiplier;
            damage.m_slash *= multiplier;
            damage.m_pierce *= multiplier;
            damage.m_fire *= multiplier;
            damage.m_frost *= multiplier;
            damage.m_lightning *= multiplier;
            damage.m_poison *= multiplier;
            damage.m_spirit *= multiplier;
        }


        private static void EnsureWoundedBeastCapeRegisteredInObjectDB(GameObject itemPrefab)
        {
            try
            {
                if (ObjectDB.instance == null || itemPrefab == null)
                    return;

                if (ObjectDB.instance.m_items != null && !ObjectDB.instance.m_items.Contains(itemPrefab))
                    ObjectDB.instance.m_items.Add(itemPrefab);

                MethodInfo updateItemHashes = typeof(ObjectDB).GetMethod(
                    "UpdateItemHashes",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                updateItemHashes?.Invoke(ObjectDB.instance, null);
            }
            catch (Exception e)
            {
                Log?.LogError($"[HaldorExpansion] EnsureWoundedBeastCapeRegisteredInObjectDB error: {e}");
            }
        }

        private static void ConfigureWoundedBeastCapeShared(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null)
                return;

            shared.m_name = WoundedBeastCapeItemKey;
            shared.m_description = WoundedBeastCapeItemDescKey;

            shared.m_armor = 1f;
            shared.m_maxQuality = 1;
            shared.m_weight = 1f;
            shared.m_value = 0;

            shared.m_maxDurability = 1800f;
            shared.m_durabilityPerLevel = 0f;
            shared.m_canBeReparied = true;
            shared.m_useDurability = true;

            shared.m_setName = "";
            shared.m_setSize = 0;
            shared.m_setStatusEffect = null;

            if (shared.m_damageModifiers != null)
            {
                shared.m_damageModifiers.RemoveAll(mod => mod.m_type == HitData.DamageType.Frost);
            }

            ApplyWoundedBeastCapeVisualEffect(shared);
        }

        private static void ApplyWoundedBeastCapeVisualEffect(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null)
                return;

            SE_Stats se = ScriptableObject.CreateInstance<SE_Stats>();
            se.name = "SE_WoundedBeastCapeVisual";

            SetFieldIfExists(se, "m_name", WoundedBeastCapeEffectNameKey);
            SetFieldIfExists(se, "m_tooltip", WoundedBeastCapeEffectDescKey);

            shared.m_equipStatusEffect = se;
        }
    }
}
