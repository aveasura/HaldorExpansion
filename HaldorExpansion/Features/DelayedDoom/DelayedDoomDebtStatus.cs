using System;
using UnityEngine;

namespace HaldorExpansion.Features.DelayedDoom
{
    internal static class DelayedDoomDebtStatus
    {
        private static StatusEffect prototype;

        internal static void SetActive(Player player, bool enabled)
        {
            if (player == null || player != Player.m_localPlayer) return;
            try
            {
                var manager = player.GetSEMan();
                if (manager == null) return;
                if (!enabled)
                {
                    if (prototype != null) manager.RemoveStatusEffect(prototype.NameHash());
                    return;
                }

                if (prototype == null)
                {
                    prototype = ScriptableObject.CreateInstance<StatusEffect>();
                    prototype.name = DelayedDoomIds.DebtStatus;
                    prototype.m_name = DelayedDoomIds.DebtNameKey;
                    prototype.m_tooltip = DelayedDoomIds.DebtDescriptionKey;
                    // The debt coroutine owns the lifetime, including refreshed/pooled debt.
                    prototype.m_ttl = 0f;
                }

                var icons = DelayedDoomItemRegistration.DelayedDoomChestItemDrop?.m_itemData?.m_shared?.m_icons;
                if (icons != null && icons.Length > 0) prototype.m_icon = icons[0];
                var active = manager.GetStatusEffect(prototype.NameHash()) ?? manager.AddStatusEffect(prototype);
                if (active != null && prototype.m_icon != null) active.m_icon = prototype.m_icon;
            }
            catch (Exception error)
            {
                // A presentation failure must not change the damage/debt coroutine.
                HaldorExpansionPlugin.Log?.LogWarning("[HaldorExpansion] Debt status unavailable: " + error.Message);
            }
        }
    }
}