using System.Collections.Generic;
using System.Reflection;
using System;
using UnityEngine;

namespace HaldorExpansion.Features.StaminaCape
{
    internal static class StaminaCapeRuntime
    {
        [ThreadStatic] private static int ForcedStaminaSpendDepth;
        internal static bool IsForcedStaminaSpend => ForcedStaminaSpendDepth > 0;

        internal const float DamageToStaminaRatio = 0.25f;
        internal const float OverflowDamageMultiplier = 1.75f;
        internal const float StaminaCostMultiplier = 2.0f;

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

                if (item.m_shared.m_name == StaminaCapeItemRegistration.ItemKey)
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
                    ForcedStaminaSpendDepth++;
                    try
                    {
                        method.Invoke(player, new object[] { amount });
                    }
                    finally
                    {
                        if (ForcedStaminaSpendDepth > 0) ForcedStaminaSpendDepth--;
                    }
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
}
