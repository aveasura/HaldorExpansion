using UnityEngine;

namespace HaldorExpansion.Combat
{
    internal static class HitDamageMath
    {

        internal static float GetHitTotalDamage(HitData hit)
        {
            if (hit == null)
                return 0f;

            HitData.DamageTypes damage = hit.m_damage;

            return Mathf.Max(0f,
                damage.m_damage +
                damage.m_blunt +
                damage.m_slash +
                damage.m_pierce +
                damage.m_fire +
                damage.m_frost +
                damage.m_lightning +
                damage.m_poison +
                damage.m_spirit
            );
        }
    }
}
