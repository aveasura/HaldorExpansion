using HaldorExpansion.Compatibility;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace HaldorExpansion.Features.Cestus
{
    internal static class CestusAbility
    {

        internal const float CestusSetBonusMultiplier = 1.5f; // Множитель, если надета сетовая броня вместе с кастетом.

        internal const float CestusCooldown = 15f;

        internal const string CestusActivationEmote = "point";
        internal const float CestusCastDuration = 1.05f;

        // Урон активки (множитель урона)
        internal const float CestusNovaDamageMultiplier = 2.7f;
        internal const float CestusNovaRadius = 4f;
        internal const float CestusNovaPushForce = 20f;

        internal static bool TryActivateCestusShield(Player player)
        {
            if (HaldorExpansionPlugin.Instance == null || player == null)
                return false;

            if (!CestusRuntime.ShouldProcessCestusLocally(player))
                return false;

            if (!CestusEquipment.HasCestusEquipped(player))
                return false;

            CestusStateStore.CestusState state = CestusStateStore.GetOrCreateCestusState(player);

            if (state.IsCasting)
                return false;

            if (state.ShieldRemaining > 0.001f)
                return false;

            if (state.Charge < CestusChargeRuntime.CestusChargeMax - 0.001f)
                return false;

            if (Time.time < state.CooldownUntil)
                return false;

            float maxHealth = CharacterHealthAccess.GetCharacterMaxHealth(player);
            if (maxHealth <= 0f)
                return false;

            float baseShieldAmount = Mathf.Max(CestusShieldRuntime.CestusShieldMinFlat, maxHealth * CestusShieldRuntime.CestusShieldMaxHpRatio);
            bool hasSetBonus = CestusEquipment.HasCestusSetBonus(player);
            float shieldMultiplier = hasSetBonus ? CestusSetBonusMultiplier : 1f;
            float shieldAmount = baseShieldAmount * shieldMultiplier;

            state.Charge = 0f;
            state.CooldownUntil = Time.time + CestusCooldown;

            if (state.CastRoutine != null)
            {
                HaldorExpansionPlugin.Instance.StopCoroutine(state.CastRoutine);
                state.CastRoutine = null;
            }

            state.CastRoutine = HaldorExpansionPlugin.Instance.StartCoroutine(CestusCastRoutine(state, shieldAmount));

            HaldorExpansionPlugin.DebugLog(
                $"[HaldorExpansion] Cestus cast started: " +
                $"baseShield={baseShieldAmount:0.##}, " +
                $"shieldMultiplier={shieldMultiplier:0.##}, " +
                $"shield={shieldAmount:0.##}, " +
                $"setBonus={hasSetBonus}, " +
                $"maxHealth={maxHealth:0.##}, " +
                $"cooldownUntil={state.CooldownUntil:0.##}");

            return true;
        }

        private static IEnumerator CestusCastRoutine(CestusStateStore.CestusState state, float shieldAmount)
        {
            if (state == null)
                yield break;

            Player player = state.Player;
            long playerId = player != null ? CestusStateStore.GetCestusStateKey(player) : 0L;

            state.IsCasting = true;
            state.InputLockedUntil = Time.time + CestusCastDuration;
            state.PreparedExplosionAudios.Clear();

            try
            {
                if (player == null || player.IsDead())
                    yield break;

                player.StartEmote(CestusActivationEmote, true);
                ClearHumanoidBlockState(player);

                state.ShieldRemaining = shieldAmount;
                state.ShieldMax = shieldAmount;
                state.ShieldTicksLeft = CestusShieldRuntime.CestusShieldTicks;
                state.ShieldDecayPerTick = shieldAmount / CestusShieldRuntime.CestusShieldTicks;

                CestusNetworkState.SyncCestusShieldState(player, state, true);

                if (state.ShieldRoutine != null)
                {
                    HaldorExpansionPlugin.Instance.StopCoroutine(state.ShieldRoutine);
                    state.ShieldRoutine = null;
                }

                CestusEffects.PlayCestusCastFx(player, state, shieldAmount);
                CestusEffects.PlayCestusCastSfx(player);

                yield return new WaitForSeconds(CestusCastDuration);

                if (player == null || player.IsDead())
                    yield break;

                state.ShieldRoutine = HaldorExpansionPlugin.Instance.StartCoroutine(CestusShieldRuntime.CestusShieldRoutine(state));
                TriggerCestusNova(player, shieldAmount);
                CestusEffects.PlayPreparedCestusExplosionAudio(state);

                HaldorExpansionPlugin.DebugLog(
                    $"[HaldorExpansion] Cestus cast resolved: " +
                    $"shield={shieldAmount:0.##}, " +
                    $"duration={CestusCastDuration:0.##}");
            }
            finally
            {
                state.IsCasting = false;
                state.InputLockedUntil = 0f;
                state.CastRoutine = null;

                if (playerId != 0 &&
                    CestusStateStore.ActiveCestusStates.TryGetValue(playerId, out CestusStateStore.CestusState current) &&
                    ReferenceEquals(current, state))
                {
                    current.IsCasting = false;
                    current.InputLockedUntil = 0f;
                    current.CastRoutine = null;
                }
            }
        }

        private static void TriggerCestusNova(Player player, float shieldAmount)
        {
            if (player == null || shieldAmount <= 0f)
                return;

            float damage = shieldAmount * CestusNovaDamageMultiplier;
            if (damage <= 0f)
                return;

            Vector3 origin = GetCestusNovaOrigin(player);
            int hits = 0;

            List<Character> characters = Character.GetAllCharacters();
            if (characters == null)
                return;

            foreach (Character target in characters)
            {
                if (!ShouldHitCestusNovaTarget(player, target, origin))
                    continue;

                ApplyCestusNovaDamage(player, target, origin, damage);
                hits++;
            }

            HaldorExpansionPlugin.DebugLog(
                $"[HaldorExpansion] Cestus nova triggered: " +
                $"shield={shieldAmount:0.##}, " +
                $"damage={damage:0.##}, " +
                $"radius={CestusNovaRadius:0.##}, " +
                $"hits={hits}");
        }

        private static bool ShouldHitCestusNovaTarget(Player player, Character target, Vector3 origin)
        {
            if (player == null || target == null)
                return false;

            if (ReferenceEquals(player, target))
                return false;

            if (target.IsDead() || target.IsPlayer())
                return false;

            if (!BaseAI.IsEnemy(player, target))
                return false;

            float maxDistance = CestusNovaRadius + Mathf.Max(0f, target.GetRadius());
            float distance = Vector3.Distance(origin, GetCestusNovaTargetPoint(target));
            return distance <= maxDistance;
        }

        private static void ApplyCestusNovaDamage(Player player, Character target, Vector3 origin, float damage)
        {
            if (player == null || target == null || damage <= 0f)
                return;

            Vector3 targetPoint = GetCestusNovaTargetPoint(target);
            Vector3 dir = targetPoint - origin;
            if (dir.sqrMagnitude <= 0.0001f)
                dir = player.transform.forward;

            HitData hit = new HitData();
            hit.m_damage.m_blunt = damage;
            hit.m_point = targetPoint;
            hit.m_dir = dir.normalized;
            hit.m_pushForce = CestusNovaPushForce;
            hit.m_skill = Skills.SkillType.Unarmed;

            TryAssignHitAttacker(hit, player);
            target.Damage(hit);
        }

        private static Vector3 GetCestusNovaOrigin(Player player)
        {
            if (player == null)
                return Vector3.zero;

            return player.GetCenterPoint();
        }

        private static Vector3 GetCestusNovaTargetPoint(Character target)
        {
            if (target == null)
                return Vector3.zero;

            return target.GetCenterPoint();
        }

        private static void TryAssignHitAttacker(HitData hit, Character attacker)
        {
            if (hit == null || attacker == null)
                return;

            MethodInfo setAttackerMethod = typeof(HitData).GetMethod(
                "SetAttacker",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(Character) },
                null);

            if (setAttackerMethod != null)
            {
                setAttackerMethod.Invoke(hit, new object[] { attacker });
                return;
            }

            FieldInfo attackerField = typeof(HitData).GetField(
                "m_attacker",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (attackerField != null && attackerField.FieldType.IsAssignableFrom(attacker.GetType()))
                attackerField.SetValue(hit, attacker);
        }

        internal static void ClearHumanoidAttackState(Humanoid humanoid)
        {
            if (humanoid == null)
                return;

            ReflectionAccess.SetFieldIfExists(humanoid, "m_attack", false);
            ReflectionAccess.SetFieldIfExists(humanoid, "m_secondaryAttack", false);
            ReflectionAccess.SetFieldIfExists(humanoid, "m_attackHold", false);
        }

        internal static void ClearHumanoidBlockState(Humanoid humanoid)
        {
            if (humanoid == null)
                return;

            ReflectionAccess.SetFieldIfExists(humanoid, "m_blocking", false);
            ReflectionAccess.SetFieldIfExists(humanoid, "m_blockTimer", 0f);
        }
    }
}
