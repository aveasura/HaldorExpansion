using HaldorExpansion.Compatibility;
using UnityEngine;

namespace HaldorExpansion.Features.SteelHeart
{
    internal static class SteelHeartSet
    {
        internal const string SteelHeartSetNameKey = "$itemset_steelheart";
        internal const string SteelHeartSetDescKey = "$itemset_steelheart_desc";

        internal static void ApplySteelHeartSet(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null)
                return;

            SE_Stats se = ScriptableObject.CreateInstance<SE_Stats>();
            se.name = "SE_SteelHeartSet";

            ReflectionAccess.SetFieldIfExists(se, "m_name", SteelHeartSetNameKey);
            ReflectionAccess.SetFieldIfExists(se, "m_tooltip", SteelHeartSetDescKey);

            shared.m_setName = SteelHeartSetNameKey;
            shared.m_setSize = 2;
            shared.m_setStatusEffect = se;
        }
    }
}