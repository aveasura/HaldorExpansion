using System.Globalization;
using HaldorExpansion.Features.Upgrades;

namespace HaldorExpansion.Features.Brisingamen
{
    internal sealed class BrisingamenCarryStatus : SE_Stats
    {
        private int tooltipQuality = 1;

        public override void SetLevel(int itemLevel, float skillLevel)
        {
            // The item tooltip supplies its preview level. Equipment supplies 0 in Valheim;
            // active effects therefore obtain their level from their own wearer's inventory.
            tooltipQuality = itemLevel;
        }

        private float CarryBonus()
        {
            if (m_character == null) return ItemUpgradeBalance.CarryWeight(tooltipQuality);
            Humanoid wearer = m_character as Humanoid;
            if (wearer == null) return 0f;
            foreach (ItemDrop.ItemData item in wearer.GetInventory().GetAllItems())
            {
                if (item.m_equipped && item.m_dropPrefab != null &&
                    item.m_dropPrefab.name == BrisingamenItemRegistration.RingPrefabName)
                    return ItemUpgradeBalance.CarryWeight(item.m_quality);
            }

            return 0f;
        }

        public override void ModifyMaxCarryWeight(float baseLimit, ref float limit)
        {
            limit += CarryBonus();
        }

        public override string GetTooltipString()
        {
            return string.Format(CultureInfo.InvariantCulture,
                Localization.instance.Localize(BrisingamenIds.EffectDescriptionKey), CarryBonus());
        }
    }
}