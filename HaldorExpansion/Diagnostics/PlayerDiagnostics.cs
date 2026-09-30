using HaldorExpansion.Features.Cestus;

namespace HaldorExpansion.Diagnostics
{
    internal static class PlayerDiagnostics
    {

        internal static string GetCestusDebugPlayerTag(Player player)
        {
            if (player == null)
                return "player=null";

            ItemDrop.ItemData currentWeapon = player.GetCurrentWeapon();
            ItemDrop.ItemData rightItem = CestusEquipment.GetPlayerHandItem(player, "m_rightItem");
            ItemDrop.ItemData leftItem = CestusEquipment.GetPlayerHandItem(player, "m_leftItem");

            bool isLocal = player == Player.m_localPlayer;
            ZNetView nview = CestusNetworkState.GetPlayerNView(player);
            bool isOwner = nview != null && nview.IsValid() && nview.IsOwner();

            return
                $"name={player.name}, " +
                $"local={isLocal}, " +
                $"owner={isOwner}, " +
                $"pid={player.GetPlayerID()}, " +
                $"iid={player.GetInstanceID()}, " +
                $"current={GetCestusItemDebugName(currentWeapon)}, " +
                $"right={GetCestusItemDebugName(rightItem)}, " +
                $"left={GetCestusItemDebugName(leftItem)}";
        }

        private static string GetCestusItemDebugName(ItemDrop.ItemData item)
        {
            if (item == null)
                return "null";

            if (item.m_dropPrefab != null && !string.IsNullOrWhiteSpace(item.m_dropPrefab.name))
                return item.m_dropPrefab.name;

            if (item.m_shared != null && !string.IsNullOrWhiteSpace(item.m_shared.m_name))
                return item.m_shared.m_name;

            return "unknown";
        }
    }
}
