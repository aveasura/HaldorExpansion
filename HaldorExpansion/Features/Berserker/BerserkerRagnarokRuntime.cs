using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace HaldorExpansion.Features.Berserker
{
    internal static class BerserkerRagnarokRuntime
    {
        private const string MeteorPrefabName = "projectile_meteor";
        private const string CastEmote = "cheer";
        private const float TargetRange = 35f;
        private const float TargetDotMinimum = 0.40f;
        // Single source of truth for both gameplay meteor spread and the cast telegraph.
        internal const float MeteorImpactRadius = 7.5f;
        private const float MeteorSpawnHeight = 38f;
        private const float MeteorHorizontalDistanceMin = 18f;
        private const float MeteorHorizontalDistanceMax = 24f;
        private const float MeteorLateralJitter = 4f;
        private const float MeteorVelocity = 30f;
        private const float MeteorInterval = 0.20f;
        private const float MeteorPushForce = 45f;

        private static readonly MethodInfo GetButtonDown =
            AccessTools.Method(AccessTools.TypeByName("ZInput"), "GetButtonDown", new[] { typeof(string) });
        private static readonly MethodInfo TakeInput = AccessTools.Method(typeof(Player), "TakeInput");
        private static readonly MethodInfo StopEmote = AccessTools.Method(typeof(Player), "StopEmote", System.Type.EmptyTypes);

        private static Coroutine castRoutine;
        private static bool casting;

        internal static bool IsCasting => casting;

        internal static void Update(Player player)
        {
            if (player == null || player != Player.m_localPlayer || player.IsDead()) return;
            if (casting || BerserkerConfiguration.RagnarokButton == null || GetButtonDown == null || TakeInput == null) return;
            if (!BerserkerEquipment.Equipped(player) || !BerserkerRuntime.State.Frenzy) return;
            if (BerserkerRuntime.Debt.Amount + 0.0001d < BerserkerConfiguration.RagnarokMinimumDebtValue) return;
            if (player.IsTeleporting() || player.InCutscene() || player.InAttack() || player.InDodge()) return;
            if (!(bool)TakeInput.Invoke(player, null)) return;
            if (!(bool)GetButtonDown.Invoke(null, new object[] { BerserkerConfiguration.RagnarokButton.Name })) return;

            Character target = AcquireTarget(player);
            if (target == null)
            {
                player.Message(MessageHud.MessageType.Center, "$msg_berserker_ragnarok_no_target");
                return;
            }

            StartCast(player, target);
        }

        internal static void Reset(Player player)
        {
            bool hadCast = casting || castRoutine != null;
            if (castRoutine != null && HaldorExpansionPlugin.Instance != null)
                HaldorExpansionPlugin.Instance.StopCoroutine(castRoutine);
            castRoutine = null;
            casting = false;
            if (hadCast && player != null) BerserkerEffects.BroadcastRagnarokStop(player);
            if (player != null && player == Player.m_localPlayer && player.InEmote())
                StopCastEmote(player);
        }

        private static void StartCast(Player player, Character target)
        {
            if (HaldorExpansionPlugin.Instance == null) return;
            casting = true;
            castRoutine = HaldorExpansionPlugin.Instance.StartCoroutine(CastRoutine(player, target, target.GetCenterPoint()));
        }

        private static IEnumerator CastRoutine(Player player, Character target, Vector3 initialTargetPoint)
        {
            try
            {
                player.StartEmote(CastEmote, true);
                BerserkerEffects.BroadcastRagnarokCast(player, target, BerserkerConfiguration.RagnarokCastDurationValue);
                float finishAt = Time.time + BerserkerConfiguration.RagnarokCastDurationValue;
                while (Time.time < finishAt)
                {
                    if (!CanContinueCast(player)) yield break;
                    yield return null;
                }

                if (!CanContinueCast(player)) yield break;
                double debt = BerserkerRuntime.Debt.Amount;
                if (debt + 0.0001d < BerserkerConfiguration.RagnarokMinimumDebtValue) yield break;

                GameObject meteorPrefab = GetMeteorPrefab();
                if (meteorPrefab == null) yield break;

                Vector3 center = target != null && !target.IsDead() ? target.GetCenterPoint() : initialTargetPoint;
                double consumed = BerserkerRuntime.Debt.ConsumeAll();
                if (consumed <= 0d) yield break;

                StopCastEmote(player);
                BerserkerRagnarokExhaustion.Apply(player);
                BerserkerEffects.BroadcastRagnarokRelease(player, center, consumed);
                player.Message(MessageHud.MessageType.Center, "$msg_berserker_ragnarok_cast");

                int count = BerserkerRagnarokMath.MeteorCount(consumed);
                float perMeteor = BerserkerRagnarokMath.PerMeteorDamage(consumed);
                if (count <= 0 || perMeteor <= 0f) yield break;

                for (int i = 0; i < count; i++)
                {
                    if (player == null || player.IsDead()) yield break;
                    SpawnMeteor(player, meteorPrefab, center, perMeteor);
                    if (i + 1 < count) yield return new WaitForSeconds(MeteorInterval);
                }
            }
            finally
            {
                if (player != null) BerserkerEffects.BroadcastRagnarokStop(player);
                if (player != null && player == Player.m_localPlayer && player.InEmote())
                    StopCastEmote(player);
                castRoutine = null;
                casting = false;
                }
        }


        private static void StopCastEmote(Player player)
        {
            if (player == null || StopEmote == null) return;
            try
            {
                StopEmote.Invoke(player, null);
            }
            catch (TargetInvocationException ex)
            {
                HaldorExpansionPlugin.Log?.LogWarning("[Berserker] Ragnarok could not stop cast emote: " +
                    (ex.InnerException?.Message ?? ex.Message));
            }
        }

        private static bool CanContinueCast(Player player)
        {
            return player != null && player == Player.m_localPlayer && !player.IsDead() &&
                   !player.IsTeleporting() && !player.InCutscene() && !player.InAttack() && !player.InDodge() &&
                   BerserkerEquipment.Equipped(player) && BerserkerRuntime.State.Frenzy;
        }

        private static Character AcquireTarget(Player player)
        {
            List<Character> characters = Character.GetAllCharacters();
            if (characters == null) return null;

            Vector3 origin = player.GetCenterPoint();
            Vector3 look = player.GetLookDir();
            if (look.sqrMagnitude <= 0.0001f) look = player.transform.forward;
            look.Normalize();

            Character best = null;
            float bestScore = float.NegativeInfinity;
            foreach (Character target in characters)
            {
                if (target == null || target == player || target.IsPlayer() || target.IsDead()) continue;
                if (!BaseAI.IsEnemy(player, target)) continue;

                Vector3 delta = target.GetCenterPoint() - origin;
                float distance = delta.magnitude;
                if (distance <= 0.01f || distance > TargetRange) continue;

                float dot = Vector3.Dot(look, delta / distance);
                if (dot < TargetDotMinimum) continue;

                // Aim alignment dominates; distance breaks ties so the intended target wins naturally.
                float score = dot * 2f - distance / TargetRange * 0.35f;
                if (score <= bestScore) continue;
                bestScore = score;
                best = target;
            }
            return best;
        }

        private static GameObject GetMeteorPrefab()
        {
            if (ZNetScene.instance == null) return null;
            GameObject prefab = ZNetScene.instance.GetPrefab(MeteorPrefabName);
            if (prefab != null && prefab.GetComponent<Projectile>() != null) return prefab;

            HaldorExpansionPlugin.Log?.LogWarning("[Berserker] Ragnarok meteor prefab missing or has no Projectile component: " + MeteorPrefabName);
            return null;
        }

        private static void SpawnMeteor(Player player, GameObject prefab, Vector3 center, float damage)
        {
            if (player == null || prefab == null || damage <= 0f) return;

            Vector2 random = Random.insideUnitCircle * MeteorImpactRadius;
            Vector3 impact = center + new Vector3(random.x, 0f, random.y);

            // Bring the whole shower in on a cinematic diagonal instead of dropping it
            // vertically. The player's horizontal look direction defines the common
            // approach, while a small lateral jitter keeps the meteors from looking
            // like copies travelling on one rail.
            Vector3 approach = player.GetLookDir();
            approach.y = 0f;
            if (approach.sqrMagnitude <= 0.0001f)
            {
                approach = player.transform.forward;
                approach.y = 0f;
            }
            approach.Normalize();

            Vector3 lateral = Vector3.Cross(Vector3.up, approach).normalized;
            float horizontalDistance = Random.Range(MeteorHorizontalDistanceMin, MeteorHorizontalDistanceMax);
            float lateralOffset = Random.Range(-MeteorLateralJitter, MeteorLateralJitter);
            Vector3 spawn = impact - approach * horizontalDistance +
                            lateral * lateralOffset + Vector3.up * MeteorSpawnHeight;
            Vector3 velocity = (impact - spawn).normalized * MeteorVelocity;

            GameObject instance = Object.Instantiate(prefab, spawn, Quaternion.LookRotation(velocity));
            Projectile projectile = instance != null ? instance.GetComponent<Projectile>() : null;
            if (projectile == null)
            {
                if (instance != null) Object.Destroy(instance);
                HaldorExpansionPlugin.Log?.LogWarning("[Berserker] Ragnarok meteor prefab has no Projectile component.");
                return;
            }

            HitData hit = new HitData();
            hit.m_damage.m_slash = damage * 0.45f;
            hit.m_damage.m_blunt = damage * 0.45f;
            hit.m_damage.m_fire = damage * 0.10f;
            hit.m_pushForce = MeteorPushForce;
            hit.m_skill = Skills.SkillType.Axes;
            hit.m_blockable = true;
            hit.m_dodgeable = true;
            hit.SetAttacker(player);

            projectile.Setup(player, velocity, -1f, hit, player.GetCurrentWeapon(), null);
            BerserkerEffects.AttachMeteorVisual(instance, damage);
        }
    }
}
