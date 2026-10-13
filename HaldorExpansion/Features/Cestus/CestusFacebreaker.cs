using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace HaldorExpansion.Features.Cestus
{
    /// <summary>
    /// Client-only targeting and target selection for Bone Crushers' Facebreaker.
    /// Movement and debuffs are applied on each monster's network owner.
    /// </summary>
    internal static class CestusFacebreaker
    {
        // Full rectangle length. 12 means SIX metres on either side of the player.
        internal const float FullLength = 12f;
        internal const float Width = 3f;

        internal const float FinalDistanceFromPlayer = 0.2f;

        // Short, punchy pull: immediate movement, with controlled braking near the target.
        internal const float PullDurationSeconds = 0.42f;
        internal const float PullMaxSpeed = 26f;
        internal const float PullSpeedBoost = 1.15f;
        internal const float PullBrakingAcceleration = 75f;

        internal const float PullStopTolerance = 0.15f;

        // Keep large bodies outside the player's collider instead of pushing them through it.
        internal const float PullBodyClearance = 0.15f;

        // A two-sided grab closes the gap slightly more than a normal pull,
        // then briefly recoils away from the centre before stagger.
        // Colliders are NEVER disabled: actual contact is still limited by physics.
        internal const float ClashContactClearance = 0f;
        internal const float ClashReboundDistance = 0.5f;
        internal const float ClashReboundStopTolerance = 0.02f;
        internal const float ClashReboundDurationSeconds = 0.14f;
        internal const float ClashReboundSpeed = 5f;
        internal const float SlowFraction = 0.60f;
        internal const float SlowDurationSeconds = 4f;
        internal const float CooldownSeconds = 5f;

        internal const float StaminaCost = 20f;

        // Custom clip: frame 14 at 24 FPS starts approximately 0.54 s after frame 1.
        // Tune this freely to align the hand-closing frame with the actual pull.
        internal const float PullTriggerDelaySeconds = 0.54f;

        internal const float CastAnimationDurationSeconds = 19f / 24f;

        // The Facebreaker strip runs forward/backward, but the custom clip opens
        // the hands to the character's LEFT/RIGHT. Turn sideways at cast time
        // so the hands follow the strip. Change to -90f for opposite facing.
        internal const float CastFacingOffsetDegrees = 90f;

        // Emergency regression toggle: false = previous instant Facebreaker.
        internal const bool EnableFacebreakerAnimation = true;

        private const float MinimumSideDistance = 0.45f;

        // A transient false input sample must not cast a hold-to-aim ability.
        private const float ReleaseConfirmationSeconds = 0.06f;

        private sealed class Candidate
        {
            internal Character Target;
            internal float Side;
        }

        private static readonly MethodInfo TakeInputMethod = AccessTools.Method(typeof(Player), "TakeInput");
        private static readonly List<Candidate> Candidates = new List<Candidate>(12);
        private static Player currentPlayer;
        private static bool aiming;
        private static Vector3 aimAxis = Vector3.forward;
        private static float nextUseTime;
        private static float releaseCandidateAt = -1f;
        private static CestusFacebreakerPreview preview;
        private static Coroutine castRoutine;
        private static bool casting;
        private static Quaternion castFacing = Quaternion.identity;

        internal static bool IsCastLocked(Player player) => casting && player != null &&
                                                            player == currentPlayer;

        internal static bool IsAiming(Player player) => aiming && player != null && player == currentPlayer;

        // Do not suppress vanilla block for other Facebreaker key binds (such as E).
        // Cooldown/low stamina/not equipped never set aiming, so RMB remains vanilla.
        internal static bool OverridesVanillaBlock(Player player) => IsAiming(player) &&
            CestusInputRegistration.FacebreakerKeyConfig != null &&
            CestusInputRegistration.FacebreakerKeyConfig.Value == KeyCode.Mouse1;

        internal static void UpdateLocal(Player player)
        {
            if (player == null || player != Player.m_localPlayer)
                return;

            if (currentPlayer != player)
            {
                Reset();
                currentPlayer = player;
            }

            if (casting) return;

            bool eligible = !player.IsDead() &&
                            CestusEquipment.HasCestusEquipped(player) &&
                            !CestusRuntime.IsCestusCasting(player) &&
                            CanTakeInput(player);
            if (!eligible)
            {
                CancelAim();
                return;
            }

            // Never poll Jotunn's blocking input unless we are actually aiming.
            // A custom Facebreaker bind (E, Mouse0, etc.) must remain available to
            // vanilla while unequipped, on cooldown or unable to afford the cast.
            if (CestusInputRegistration.FacebreakerButton == null ||
                CestusInputRegistration.FacebreakerKeyConfig == null ||
                CestusInputRegistration.FacebreakerKeyConfig.Value == KeyCode.None ||
                player.GetStamina() < StaminaCost)
            {
                CancelAim();
                return;
            }

            KeyCode key = CestusInputRegistration.FacebreakerKeyConfig.Value;
            if (!aiming)
            {
                // Require a FRESH press. When a cooldown ends while RMB is still
                // held for blocking, Facebreaker must not steal that ongoing hold.
                // This also avoids activating from a key held before equipping.
                if (Time.time < nextUseTime || !ZInput.GetKeyDown(key))
                    return;
            }

            // Physical input is independent of Jotunn's virtual button resets.
            bool holding = ZInput.GetKey(key);
            if (holding)
            {
                // Jotunn's BlockOtherInputs resets other actions on this exact
                // binding. Poll it ONLY on an eligible Facebreaker aiming frame;
                // otherwise vanilla actions (including E/attack/block) work.
                ZInput.GetButton(CestusInputRegistration.FacebreakerButton.Name);
                releaseCandidateAt = -1f;
                aiming = true;
                aimAxis = CameraDirection(player);
                if (preview == null)
                    preview = new CestusFacebreakerPreview();
                preview.Set(player.transform.position, aimAxis, FullLength, Width);
                return;
            }

            if (!aiming)
                return;

            // Confirm release across several frames. A single false key sample must
            // never consume stamina and start the cooldown without an actual cast.
            if (releaseCandidateAt < 0f)
            {
                releaseCandidateAt = Time.unscaledTime;
                return;
            }

            if (Time.unscaledTime - releaseCandidateAt < ReleaseConfirmationSeconds)
                return;

            // The last visible direction is the direction that gets cast on release.
            CancelAim();
            Execute(player, aimAxis);
        }

        private static void Execute(Player player, Vector3 axis)
        {
            if (player == null || !CestusEquipment.HasCestusEquipped(player) ||
                player.IsDead() || casting || CestusRuntime.IsCestusCasting(player) ||
                player.GetStamina() < StaminaCost || Time.time < nextUseTime)
                return;

            axis.y = 0f;
            if (axis.sqrMagnitude < 0.01f)
                return;
            axis.Normalize();
            // Commit stamina and cooldown on release, not at the later animation hit.
            player.UseStamina(StaminaCost);
            nextUseTime = Time.time + CooldownSeconds;

            // If a client cannot load/play the custom clip, its original instant pull
            // remains fully functional (including its original net/physics behaviour).
            if (EnableFacebreakerAnimation && HaldorExpansionPlugin.Instance != null &&
                CestusFacebreakerAnimation.TryBegin(player))
            {
                // Keep the spell's axis exactly as aimed. Only rotate the
                // player's body sideways for the hands-open animation.
                castFacing = Quaternion.LookRotation(axis, Vector3.up) *
                             Quaternion.Euler(0f, CastFacingOffsetDegrees, 0f);
                casting = true;
                currentPlayer = player;
                MaintainCastFacing(player);
                CestusAbility.ClearHumanoidAttackState(player);
                CestusAbility.ClearHumanoidBlockState(player);
                castRoutine = HaldorExpansionPlugin.Instance.StartCoroutine(CastRoutine(player, axis));
                return;
            }

            ResolvePull(player, axis);
        }

        // Vanilla turning and the fixed physics loop may overwrite a single
        // transform.rotation assignment. Apply the locked facing after both
        // update paths only while this local Facebreaker cast is active.
        internal static void MaintainCastFacing(Player player)
        {
            if (!casting || player == null || player != Player.m_localPlayer ||
                player != currentPlayer || player.IsDead())
                return;
            player.transform.rotation = castFacing;
        }

        private static IEnumerator CastRoutine(Player player, Vector3 axis)
        {
            try
            {
                yield return new WaitForSeconds(PullTriggerDelaySeconds);
                if (player == null || player != Player.m_localPlayer || player.IsDead()) yield break;
                // The original physics/network pull starts at the hand-closing pose.
                // Committed casts aren't revalidated for movement/equipment/stamina.
                ResolvePull(player, axis);
                yield return new WaitForSeconds(Mathf.Max(0f,
                    CastAnimationDurationSeconds - PullTriggerDelaySeconds));
            }
            finally
            {
                CestusFacebreakerAnimation.EndLocal(player);
                casting = false;
                castRoutine = null;
            }
        }

        private static void ResolvePull(Player player, Vector3 axis)
        {
            if (player == null || player.IsDead()) return;
            Vector3 sideways = Vector3.Cross(Vector3.up, axis);
            Vector3 origin = player.transform.position;
            Candidates.Clear();

            // One target query on RELEASE only; there is no enemy scan during aiming.
            List<Character> characters = Character.GetAllCharacters();
            for (int i = 0; i < characters.Count; i++)
            {
                Character target = characters[i];
                if (target == null || target.IsDead() || target.IsPlayer() || target.IsBoss() ||
                    !BaseAI.IsEnemy(player, target))
                    continue;

                Vector3 difference = target.transform.position - origin;
                difference.y = 0f;
                float side = Vector3.Dot(difference, axis);
                float lateral = Vector3.Dot(difference, sideways);
                float radius = Mathf.Max(0f, target.GetRadius());
                if (Mathf.Abs(side) > FullLength * 0.5f + radius ||
                    Mathf.Abs(lateral) > Width * 0.5f + radius ||
                    Mathf.Abs(side) < MinimumSideDistance)
                    continue;

                Candidates.Add(new Candidate { Target = target, Side = side });
            }

            // Like Sett's Facebreaker: if the cast captures enemies on both sides,
            // ALL captured targets qualify for stagger once successfully pulled.
            // This is a single cast-wide decision, not a per-target physics collision.
            bool hasFront = false;
            bool hasBack = false;
            for (int i = 0; i < Candidates.Count; i++)
            {
                if (Candidates[i].Side > 0f)
                    hasFront = true;
                else
                    hasBack = true;
                if (hasFront && hasBack)
                    break;
            }

            bool clash = hasFront && hasBack;

            for (int i = 0; i < Candidates.Count; i++)
            {
                Candidate candidate = Candidates[i];
                CestusFacebreakerNetwork.Send(
                    player, candidate.Target, origin, axis, candidate.Side >= 0f, clash);
            }

            Candidates.Clear();
        }

        private static bool CanTakeInput(Player player)
        {
            // This is non-public in some Valheim builds: do not make a direct call
            // that would break compilation against those assemblies.
            if (TakeInputMethod == null) return false;
            try
            {
                return TakeInputMethod.Invoke(player, null) is bool allowed && allowed;
            }
            catch
            {
                return false;
            }
        }

        private static Vector3 CameraDirection(Player player)
        {
            Vector3 forward = Camera.main != null ? Camera.main.transform.forward : player.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                forward = player.transform.forward;
            forward.y = 0f;
            return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
        }

        private static void CancelAim()
        {
            aiming = false;
            releaseCandidateAt = -1f;
            if (preview != null)
            {
                preview.Dispose();
                preview = null;
            }
        }

        internal static void Reset()
        {
            if (castRoutine != null && HaldorExpansionPlugin.Instance != null)
                HaldorExpansionPlugin.Instance.StopCoroutine(castRoutine);
            castRoutine = null;
            casting = false;
            CestusFacebreakerAnimation.EndLocal(currentPlayer);
            CancelAim();
            Candidates.Clear();
            currentPlayer = null;
            nextUseTime = 0f;
        }
    }
}