namespace HaldorExpansion.Features.Awakened
{
    internal enum Artifact
    {
        None,
        DelayedDoomChest,
        BerserkerHelmet,
        BerserkerCloak,
        ScorchedEnduranceCloak,
        WoundedBeastCloak,
        HelheimPelt,
        PitKingChest,
        BerserkerAxe,
        BoneCrushers,
        HelOath,
        ShadowCrossbow,
        Brisingamen
    }

    internal static class AwakenedEquipment
    {
        internal static Artifact Identify(ItemDrop.ItemData item)
        {
            if (item == null) return Artifact.None;
            string prefab = item.m_dropPrefab != null ? item.m_dropPrefab.name : null;
            Artifact artifact = IdentifyName(prefab);
            return artifact != Artifact.None
                ? artifact
                : IdentifyName(item.m_shared != null ? item.m_shared.m_name : null);
        }

        // Published prefab IDs and localization keys; display names are never identifiers.
        private static Artifact IdentifyName(string name)
        {
            switch (name)
            {
                case "ArmorDelayedDoomChest":
                case "$item_chest_delayeddoom": return Artifact.DelayedDoomChest;
                case "aveasura_he_berserker_helmet":
                case "$item_aveasura_he_berserker_helmet": return Artifact.BerserkerHelmet;
                case "aveasura_he_berserker_cloak":
                case "$item_aveasura_he_berserker_cloak": return Artifact.BerserkerCloak;
                case "CapeFracturedBreath":
                case "$item_cape_fracturedbreath": return Artifact.ScorchedEnduranceCloak;
                case "CapeWoundedBeast":
                case "$item_cape_woundedbeast": return Artifact.WoundedBeastCloak;
                case "CapePeltOfHelheim":
                case "$item_cape_peltofhelheim": return Artifact.HelheimPelt;
                case "ArmorPitKingChest":
                case "$item_chest_pitking": return Artifact.PitKingChest;
                case "aveasura_he_berserker_axe":
                case "$item_aveasura_he_berserker_axe": return Artifact.BerserkerAxe;
                case "Bone_crushers":
                case "$item_weapon_gritcestus": return Artifact.BoneCrushers;
                case "BowHelOath":
                case "$item_bow_heloath": return Artifact.HelOath;
                case "CrossbowSilentHunt":
                case "$item_crossbow_silenthunt": return Artifact.ShadowCrossbow;
                case "BrisingamenRing":
                case "$item_brisingamen": return Artifact.Brisingamen;
                default: return Artifact.None;
            }
        }

        internal static bool IsAwakened(ItemDrop.ItemData item)
        {
            return item != null && item.m_quality >= AwakenedBalance.QualityThreshold &&
                   Identify(item) != Artifact.None;
        }

        // Same equipped/quality/identifier rules as Has, in one traversal for both passives.
        // Recomputed each refresh so upgrades of the same equipped instance remain visible.
        internal static int StatRefreshSignature(Player player)
        {
            Inventory inventory = player != null ? player.GetInventory() : null;
            var items = inventory != null ? inventory.GetAllItems() : null;
            if (items == null) return 0;
            int signature = 0;
            foreach (ItemDrop.ItemData item in items)
            {
                if (item == null || !item.m_equipped || item.m_quality < AwakenedBalance.QualityThreshold)
                    continue;
                Artifact artifact = Identify(item);
                if (artifact == Artifact.WoundedBeastCloak) signature |= 1;
                else if (artifact == Artifact.HelheimPelt) signature |= 2;
                if (signature == 3) break;
            }

            return signature;
        }

        internal static bool Has(Player player, Artifact artifact)
        {
            if (player == null || artifact == Artifact.None) return false;
            if (artifact == Artifact.BerserkerAxe || artifact == Artifact.BoneCrushers ||
                artifact == Artifact.HelOath || artifact == Artifact.ShadowCrossbow)
            {
                ItemDrop.ItemData weapon = player.GetCurrentWeapon();
                return IsAwakened(weapon) && Identify(weapon) == artifact;
            }

            Inventory inventory = player.GetInventory();
            if (inventory == null) return false;
            var items = inventory.GetAllItems();
            if (items == null) return false;
            foreach (ItemDrop.ItemData item in items)
                if (item != null && item.m_equipped && IsAwakened(item) && Identify(item) == artifact)
                    return true;
            return false;
        }
    }
}