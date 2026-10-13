using System;
using System.Collections;
using System.Reflection;
using HaldorExpansion.Compatibility;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace HaldorExpansion.Features.Berserker
{
    internal static class BerserkerRagnarokRuntime
    {
        private const string MeteorPrefabName = "projectile_meteor";

        private const string CastEmote = "cheer";

        // Meteor spread and explosion radius are computed from the E-press Debt
        // snapshot by BerserkerRagnarokMath; no static gameplay radius here.
        private const float MeteorSpawnHeight = 38f;
        private const float MeteorHorizontalDistanceMin = 18f;
        private const float MeteorHorizontalDistanceMax = 24f;
        private const float MeteorLateralJitter = 4f;
        private const float MeteorVelocity = 30f;
        private const float MeteorInterval = 0.60f;
        private const float MeteorPushForce = 45f;

        private static readonly MethodInfo GetButtonDown =
            AccessTools.Method(AccessTools.TypeByName("ZInput"), "GetButtonDown", new[] { typeof(string) });

        private static readonly MethodInfo TakeInput = AccessTools.Method(typeof(Player), "TakeInput");

        private static readonly MethodInfo StopEmote =
            AccessTools.Method(typeof(Player), "StopEmote", Type.EmptyTypes);

        private static Coroutine castRoutine;
        private static bool casting;
        private static float inputLockedUntil;

        internal static bool IsCasting => casting;

        // Input locking and meteor release follow ONLY the configured cast duration.
        // Skyward aura delay/emission are cosmetic and never alter gameplay timing.
        internal static bool IsInputLocked(Player player)
        {
            return player != null && player == Player.m_localPlayer && casting && Time.time < inputLockedUntil;
        }

        internal static void Update(Player player)
        {
            if (player == null || player != Player.m_localPlayer || player.IsDead()) return;
            if (casting || BerserkerConfiguration.RagnarokButton == null || GetButtonDown == null ||
                TakeInput == null) return;
            if (!BerserkerEquipment.Equipped(player) || !BerserkerRuntime.State.Frenzy) return;
            if (!BerserkerCloakRuntime.HasEquipped(player)) return;
            if (BerserkerRuntime.Debt.Amount + 0.0001d < BerserkerConfiguration.RagnarokMinimumDebtValue) return;
            if (player.IsTeleporting() || player.InCutscene() || player.InAttack() || player.InDodge()) return;
            if (!(bool)TakeInput.Invoke(player, null)) return;
            if (!(bool)GetButtonDown.Invoke(null, new object[] { BerserkerConfiguration.RagnarokButton.Name })) return;

            StartCast(player);
        }

        internal static void Reset(Player player)
        {
            bool hadCast = casting || castRoutine != null;
            if (castRoutine != null && HaldorExpansionPlugin.Instance != null)
                HaldorExpansionPlugin.Instance.StopCoroutine(castRoutine);
            castRoutine = null;
            casting = false;
            inputLockedUntil = 0f;
            if (hadCast && player != null) BerserkerEffects.BroadcastRagnarokStop(player);
            if (player != null && player == Player.m_localPlayer && player.InEmote())
                StopCastEmote(player);
        }

        private static void StartCast(Player player)
        {
            if (HaldorExpansionPlugin.Instance == null) return;
            // Accepted E starts the cosmetic stopwatch immediately. This does NOT
            // extend the emote, meteor release, input lock or any damage timings.
            float castSeconds = BerserkerConfiguration.RagnarokCastDurationValue;
            // Freeze gameplay scaling at E. The telegraph RPC carries this same radius.
            double debtAtActivation = BerserkerRuntime.Debt.Amount;
            float auraStrength = BerserkerWrathEffects.CaptureInfernoStrength(player);
            casting = true;
            inputLockedUntil = Time.time + castSeconds;
            BerserkerRagnarokFx.BeginLocalSkywardAura(player, auraStrength);
            BerserkerRagnarokWeather.Begin(player);
            castRoutine = HaldorExpansionPlugin.Instance.StartCoroutine(
                CastRoutine(player, castSeconds, debtAtActivation, auraStrength));
        }

        private static IEnumerator CastRoutine(Player player, float castSeconds, double debtAtActivation,
            float auraStrength)
        {
            try
            {
                player.StartEmote(CastEmote, true);
                ClearHumanoidAttackState(player);
                ClearHumanoidBlockState(player);
                BerserkerEffects.BroadcastRagnarokCast(player, castSeconds,
                    BerserkerRagnarokMath.MeteorImpactRadius(debtAtActivation), auraStrength);

                // Once the activation conditions passed and E started the cast, Ragnarök is
                // committed. Do not re-check Wrath, Debt, equipment, attack/dodge state, etc.
                // during the animation. Only death can prevent resolution. This mirrors the
                // reliable commitment behaviour used by Cestus.
                yield return new WaitForSeconds(castSeconds);

                if (player == null || player != Player.m_localPlayer || player.IsDead()) yield break;

                GameObject meteorPrefab = GetMeteorPrefab();
                if (meteorPrefab == null) yield break;

                // Ragnarök is centered on the berserker. The telegraph follows the
                // player during the cast, then this exact position is frozen for the
                // entire meteor shower once the cast successfully completes.
                Vector3 center = player.transform.position;
                double consumed = BerserkerRuntime.Debt.ConsumeAll();
                if (consumed <= 0d) yield break;

                StopCastEmote(player);
                BerserkerRagnarokExhaustion.Apply(player);
                BerserkerEffects.BroadcastRagnarokRelease(player, center, debtAtActivation);
                // The unleashed storm consumes Wrath I-V as well as Blood Debt.
                // Reset on release (not activation) so interrupted casts do not
                // discard stacks, and sync the approved Wrath aura out for others.
                BerserkerRuntime.ConsumeWrathForRagnarok(player);
                player.Message(MessageHud.MessageType.Center, BerserkerIds.RagnarokCastKey);

                int count = BerserkerRagnarokMath.MeteorCount(debtAtActivation);
                float perMeteor = BerserkerRagnarokMath.PerMeteorDamage(debtAtActivation);
                float impactRadius = BerserkerRagnarokMath.MeteorImpactRadius(debtAtActivation);
                float explosionRadius = BerserkerRagnarokMath.MeteorExplosionRadius(debtAtActivation);
                if (count <= 0 || perMeteor <= 0f) yield break;

                for (int i = 0; i < count; i++)
                {
                    if (player == null || player.IsDead()) yield break;
                    SpawnMeteor(player, meteorPrefab, center, perMeteor, impactRadius, explosionRadius);
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
                inputLockedUntil = 0f;
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

        internal static void ClearHumanoidAttackState(Humanoid humanoid)
        {
            if (humanoid == null) return;
            ReflectionAccess.SetFieldIfExists(humanoid, "m_attack", false);
            ReflectionAccess.SetFieldIfExists(humanoid, "m_secondaryAttack", false);
            ReflectionAccess.SetFieldIfExists(humanoid, "m_attackHold", false);
        }

        internal static void ClearHumanoidBlockState(Humanoid humanoid)
        {
            if (humanoid == null) return;
            ReflectionAccess.SetFieldIfExists(humanoid, "m_blocking", false);
            ReflectionAccess.SetFieldIfExists(humanoid, "m_blockTimer", 0f);
        }

        private static GameObject GetMeteorPrefab()
        {
            if (ZNetScene.instance == null) return null;
            GameObject prefab = ZNetScene.instance.GetPrefab(MeteorPrefabName);
            if (prefab != null && prefab.GetComponent<Projectile>() != null) return prefab;

            HaldorExpansionPlugin.Log?.LogWarning(
                "[Berserker] Ragnarok meteor prefab missing or has no Projectile component: " + MeteorPrefabName);
            return null;
        }

        private static void SpawnMeteor(Player player, GameObject prefab, Vector3 center,
            float damage, float impactRadius, float explosionRadius)
        {
            if (player == null || prefab == null || damage <= 0f) return;

            Vector2 random = Random.insideUnitCircle * impactRadius;
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
                HaldorExpansionPlugin.Log?.LogWarning(
                    "[Berserker] Ragnarok meteor prefab has no Projectile component.");
                return;
            }

            HitData hit = new HitData();
            hit.m_damage.m_lightning = damage * 0.45f;
            hit.m_damage.m_blunt = damage * 0.45f;
            hit.m_damage.m_fire = damage * 0.10f;

            // Trees
            hit.m_damage.m_chop = damage;

            // Rocks and ore deposits
            hit.m_damage.m_pickaxe = damage;

            // Shared tool tier for both damage types
            hit.m_toolTier = 4;
            
            hit.m_pushForce = MeteorPushForce;
            hit.m_skill = Skills.SkillType.Axes;
            hit.m_blockable = true;
            hit.m_dodgeable = true;
            hit.SetAttacker(player);

            projectile.Setup(player, velocity, -1f, hit, player.GetCurrentWeapon(), null);

            // projectile_meteor has its own small vanilla splash. Override it so the
            // cinematic explosion and the gameplay hit area match the intended
            // crowd-control role of Ragnarök. Projectile.DoAOE then applies the same
            // custom HitData damage to every valid target inside this sphere.
            projectile.m_aoe = explosionRadius;

            // ZDO marker makes the unique cosmetics visible to every client,
            // while vanilla projectile physics and AoE remain unchanged.
            BerserkerRagnarokMeteorFx.MarkOwnedMeteor(instance,
                Mathf.Clamp(1f + Mathf.Log10(Mathf.Max(1f, damage / 100f)) * 0.24f, 1f, 1.75f),
                player, explosionRadius);
            BerserkerEffects.AttachMeteorVisual(instance, damage);
        }
    }
}