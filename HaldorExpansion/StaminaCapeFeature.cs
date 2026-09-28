using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace HaldorExpansion
{
    internal static class StaminaCapeFeature
    {
        internal const string PrefabName = "CapeFracturedBreath";
        internal const string SourcePrefab = "CapeLox";
        internal const int Price = 2500;

        internal const float DamageToStaminaRatio = 0.15f;
        internal const float OverflowDamageMultiplier = 1.30f;
        internal const float StaminaCostMultiplier = 1.5f;

        internal const string ItemKey = "$item_cape_fracturedbreath";
        internal const string ItemDescKey = "$item_cape_fracturedbreath_desc";
        internal const string EffectNameKey = "$se_cape_fracturedbreath";
        internal const string EffectDescKey = "$se_cape_fracturedbreath_desc";

        internal static GameObject CapePrefab;
        internal static ItemDrop CapeItemDrop;
        

        internal static void ResetCaches()
        {
            CapePrefab = null;
            CapeItemDrop = null;
        }

        internal static bool HasEquipped(Player player)
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

                if (item.m_shared.m_name == ItemKey)
                    return true;
            }

            return false;
        }

        internal static float GetPlayerStamina(Player player)
        {
            return InvokeFloatMethod(player, "GetStamina");
        }

        internal static void SpendPlayerStamina(Player player, float amount)
        {
            if (player == null || amount <= 0f)
                return;

            amount = Mathf.Max(0f, amount);

            Type type = player.GetType();
            while (type != null)
            {
                MethodInfo method = type.GetMethod(
                    "UseStamina",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(float) },
                    null);

                if (method != null)
                {
                    method.Invoke(player, new object[] { amount });
                    return;
                }

                type = type.BaseType;
            }

            FieldInfo staminaField = player.GetType().GetField(
                "m_stamina",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (staminaField != null)
            {
                object currentObj = staminaField.GetValue(player);
                if (currentObj is float current)
                {
                    staminaField.SetValue(player, Mathf.Max(0f, current - amount));
                }
            }
        }

        internal static bool EnsureReady()
        {
            try
            {

                if (ObjectDB.instance == null)
                    return false;

                GameObject existing = ObjectDB.instance.GetItemPrefab(PrefabName);
                if (existing != null)
                {
                    CapePrefab = existing;
                    CapeItemDrop = existing.GetComponent<ItemDrop>();

                    if (CapeItemDrop != null)
                    {
                        ItemDrop.ItemData.SharedData shared = CapeItemDrop.m_itemData.m_shared;
                        ConfigureShared(shared);

                        CapeItemDrop.m_itemData.m_dropPrefab = existing;
                        shared.m_canBeReparied = true;
                        shared.m_useDurability = true;
                    }

                    HaldorExpansionPlugin.DebugLog($"[HaldorExpansion] Stamina cape already in ObjectDB: {PrefabName}");
                    return CapeItemDrop != null;
                }

                GameObject source = ObjectDB.instance.GetItemPrefab(SourcePrefab);
                if (source == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning(
                        $"[HaldorExpansion] Source cape prefab not found: {SourcePrefab}");
                    return false;
                }

                GameObject capeGo = PrefabManager.Instance.CreateClonedPrefab(PrefabName, source.name);
                if (capeGo == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning(
                        "[HaldorExpansion] Failed to clone stamina cape prefab");
                    return false;
                }

                ItemDrop drop = capeGo.GetComponent<ItemDrop>();
                if (drop == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning(
                        "[HaldorExpansion] ItemDrop missing on stamina cape");
                    return false;
                }

                ItemDrop.ItemData.SharedData sharedNew = drop.m_itemData.m_shared;

                ConfigureShared(sharedNew);
                drop.m_itemData.m_dropPrefab = capeGo;

                ItemManager.Instance.AddItem(new CustomItem(capeGo, true));

                CapePrefab = capeGo;
                CapeItemDrop = drop;

                HaldorExpansionPlugin.DebugLog(
                    $"[HaldorExpansion] Stamina cape registered: {PrefabName} (source={source.name})");
                return true;
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogError($"[HaldorExpansion] EnsureReady stamina cape error: {e}");
                return false;
            }
        }

        internal static void RegisterRecipe()
        {
            try
            {
                string recipeName = "Recipe_" + PrefabName;

                if (ItemManager.Instance.GetRecipe(recipeName) != null)
                {
                    HaldorExpansionPlugin.DebugLog(
                        $"[HaldorExpansion] Recipe already registered: {recipeName}");
                    return;
                }

                GameObject sourcePrefab = ObjectDB.instance?.GetItemPrefab(SourcePrefab);
                if (sourcePrefab == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning(
                        "[HaldorExpansion] Source cape prefab not found for recipe");
                    return;
                }

                ItemDrop sourceDrop = sourcePrefab.GetComponent<ItemDrop>();
                if (sourceDrop == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning(
                        "[HaldorExpansion] Source cape ItemDrop not found for recipe");
                    return;
                }

                Recipe sourceRecipe = ObjectDB.instance.GetRecipe(sourceDrop.m_itemData);
                if (sourceRecipe == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning(
                        "[HaldorExpansion] Source cape recipe not found");
                    return;
                }

                string craftingStation = sourceRecipe.m_craftingStation != null
                    ? sourceRecipe.m_craftingStation.name
                    : null;

                string repairStation = "forge";
                
                var recipeConfig = new RecipeConfig
                {
                    Name = recipeName,
                    Item = PrefabName,
                    Amount = 1,
                    CraftingStation = craftingStation,
                    RepairStation = repairStation,
                    MinStationLevel = 1,
                    Enabled = false,
                    Requirements = Array.Empty<RequirementConfig>()
                };

                ItemManager.Instance.AddRecipe(new CustomRecipe(recipeConfig));
                HaldorExpansionPlugin.DebugLog(
                    $"[HaldorExpansion] Registered recipe via Jotunn for {PrefabName}");
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogError(
                    $"[HaldorExpansion] RegisterRecipe stamina cape error: {e}");
            }
        }

        internal static bool FixItemsInInventory(Player player)
        {
            try
            {
                if (player == null || ObjectDB.instance == null)
                    return false;

                GameObject capePrefab = ObjectDB.instance.GetItemPrefab(PrefabName);
                if (capePrefab == null)
                    return false;

                ItemDrop prefabDrop = capePrefab.GetComponent<ItemDrop>();
                if (prefabDrop == null)
                    return false;

                Inventory inventory = player.GetInventory();
                if (inventory == null)
                    return false;

                bool found = false;
                bool changed = false;

                List<ItemDrop.ItemData> items = inventory.GetAllItems();
                if (items == null)
                    return false;

                foreach (ItemDrop.ItemData item in items)
                {
                    if (item == null || item.m_shared == null)
                        continue;

                    if (item.m_shared.m_name != ItemKey)
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

                    float maxDurability = item.m_shared.m_maxDurability +
                                          item.m_shared.m_durabilityPerLevel * (item.m_quality - 1);
                    item.m_durability = Mathf.Clamp(oldDurability, 0f, Mathf.Max(1f, maxDurability));

                    item.m_shared.m_canBeReparied = true;
                    item.m_shared.m_useDurability = true;

                    changed = true;
                }

                if (found)
                {
                    Recipe recipe = ObjectDB.instance.GetRecipe(prefabDrop.m_itemData);

                    HaldorExpansionPlugin.DebugLog(
                        $"[HaldorExpansion] Stamina cape inventory rebind checked: " +
                        $"changed={changed}, " +
                        $"recipe={(recipe != null ? recipe.name : "null")}, " +
                        $"craft={(recipe != null ? recipe.m_craftingStation?.name : "null")}, " +
                        $"repair={(recipe != null ? recipe.m_repairStation?.name : "null")}, " +
                        $"dropPrefab={capePrefab.name}");
                }

                return found;
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogError(
                    $"[HaldorExpansion] FixItemsInInventory stamina cape error: {e}");
                return false;
            }
        }

        private static void ConfigureShared(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null)
                return;

            shared.m_name = ItemKey;
            shared.m_description = ItemDescKey;

            shared.m_armor = 4f;
            shared.m_maxQuality = 1;
            shared.m_weight = 1f;
            
            shared.m_maxDurability = 1800f;
            shared.m_durabilityPerLevel = 0f;
            
            shared.m_value = 0;
            shared.m_canBeReparied = true;
            shared.m_useDurability = true;

            shared.m_setName = "";
            shared.m_setSize = 0;
            shared.m_setStatusEffect = null;

            // Убрать морозостойкость
            if (shared.m_damageModifiers != null)
            {
                shared.m_damageModifiers.RemoveAll(mod => mod.m_type == HitData.DamageType.Frost);
            }
            
            ApplyVisualEffect(shared);
        }

        private static void ApplyVisualEffect(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null)
                return;

            SE_Stats se = ScriptableObject.CreateInstance<SE_Stats>();
            se.name = "SE_FracturedBreathCapeVisual";

            SetFieldIfExists(se, "m_name", EffectNameKey);
            SetFieldIfExists(se, "m_tooltip", EffectDescKey);

            shared.m_equipStatusEffect = se;
        }

        private static void SetFieldIfExists(object obj, string fieldName, object value)
        {
            FieldInfo f = obj.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (f != null)
            {
                f.SetValue(obj, value);
            }
        }

        private static float InvokeFloatMethod(object target, string methodName)
        {
            if (target == null)
                return 0f;

            Type type = target.GetType();
            while (type != null)
            {
                MethodInfo method = type.GetMethod(
                    methodName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    Type.EmptyTypes,
                    null);

                if (method != null && method.ReturnType == typeof(float))
                {
                    object result = method.Invoke(target, null);
                    if (result is float value)
                        return value;
                }

                type = type.BaseType;
            }

            return 0f;
        }
    }

    [HarmonyPatch(typeof(ObjectDB), "Awake")]
    internal static class StaminaCapeObjectDBAwakePatch
    {
        private static void Postfix()
        {
            StaminaCapeFeature.ResetCaches();

            if (HaldorExpansionPlugin.Instance != null)
            {
                HaldorExpansionPlugin.Instance.StartCoroutine(DelayedEnsure());
            }
        }

        private static IEnumerator DelayedEnsure()
        {
            yield return null;

            yield return new WaitUntil(() =>
                ObjectDB.instance != null &&
                ObjectDB.instance.m_items != null &&
                ObjectDB.instance.m_items.Count > 0 &&
                ObjectDB.instance.m_recipes != null &&
                ObjectDB.instance.m_recipes.Count > 0);
            
            StaminaCapeFeature.EnsureReady();
            StaminaCapeFeature.RegisterRecipe();
        }
    }

    [HarmonyPatch(typeof(Player), "Update")]
    internal static class StaminaCapeInventoryFixPatch
    {
        private static readonly HashSet<int> ProcessedPlayers = new HashSet<int>();

        static void Postfix(Player __instance)
        {
            if (__instance == null || ObjectDB.instance == null)
                return;

            int id = __instance.GetInstanceID();
            if (ProcessedPlayers.Contains(id))
                return;

            StaminaCapeFeature.FixItemsInInventory(__instance);
            ProcessedPlayers.Add(id);
        }
    }

    [HarmonyPatch]
    internal static class HaldorStaminaCapeShopPatch
    {
        static MethodBase TargetMethod()
        {
            MethodInfo fallback = null;

            MethodInfo[] methods =
                typeof(Trader).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (MethodInfo method in methods)
            {
                if (method.Name != "Interact")
                    continue;

                if (fallback == null)
                    fallback = method;

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length >= 1 && typeof(Humanoid).IsAssignableFrom(parameters[0].ParameterType))
                    return method;
            }

            return fallback;
        }

        static void Prefix(Trader __instance)
        {
            if (!IsHaldor(__instance))
                return;

            if (__instance.m_items == null)
                __instance.m_items = new List<Trader.TradeItem>();
            
            StaminaCapeFeature.EnsureReady();
            TryAddCape(__instance);
        }

        private static void TryAddCape(Trader trader)
        {
            ItemDrop capeDrop = StaminaCapeFeature.CapeItemDrop;
            if (capeDrop == null)
                return;

            if (HasPrefab(trader, StaminaCapeFeature.PrefabName))
                return;

            AddTradeItem(trader, capeDrop, StaminaCapeFeature.Price, 1);
            HaldorExpansionPlugin.DebugLog("[HaldorExpansion] Stamina cape added to Haldor shop");
        }

        private static bool IsHaldor(Trader trader)
        {
            return trader != null
                   && trader.gameObject != null
                   && trader.gameObject.name.StartsWith("Haldor");
        }

        private static bool HasPrefab(Trader trader, string prefabName)
        {
            string requiredGlobalKey = HaldorExpansionPlugin.GetShopRequiredGlobalKey(prefabName);

            foreach (Trader.TradeItem tradeItem in trader.m_items)
            {
                if (tradeItem.m_prefab != null && tradeItem.m_prefab.name == prefabName)
                {
                    // Keep already-added entries in sync as well. This matters when a
                    // server config is synchronized/changed after the TradeItem exists.
                    tradeItem.m_requiredGlobalKey = requiredGlobalKey;
                    return true;
                }
            }

            return false;
        }

        private static void AddTradeItem(Trader trader, ItemDrop itemDrop, int price, int stack)
        {
            ItemDrop.ItemData itemData = itemDrop != null ? itemDrop.m_itemData : null;
            ItemDrop.ItemData.SharedData shared = itemData != null ? itemData.m_shared : null;

            // Valheim 1.0 expanded Trader.TradeItem with UI/progression fields.
            // StoreGui.FillList expects these reference fields to be initialized;
            // leaving them null (the pre-1.0 behavior) causes a NullReferenceException.
            trader.m_items.Add(new Trader.TradeItem
            {
                m_prefab = itemDrop,
                m_price = price,
                m_stack = stack,
                m_requiredGlobalKey = HaldorExpansionPlugin.GetShopRequiredGlobalKey(
                    itemDrop != null ? itemDrop.name : string.Empty),
                m_levelUpEffect = false,
                m_buyPlayerEffects = new EffectList(),
                m_icon = null,
                m_name = shared != null ? shared.m_name ?? string.Empty : string.Empty,
                m_tooltip = shared != null ? shared.m_description ?? string.Empty : string.Empty,
                m_buyKey = string.Empty,
                m_incrementKey = string.Empty,
                m_incrementAmount = 0
            });
        }
    }
}