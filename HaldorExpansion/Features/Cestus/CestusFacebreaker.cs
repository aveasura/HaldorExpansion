using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

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
        internal const float FinalDistanceFromPlayer = 0.3f;
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
        internal const float SlowFraction = 0.90f;
        internal const float SlowDurationSeconds = 4f;
        internal const float CooldownSeconds = 2f;
        internal const float StaminaCost = 15f;

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

        internal static bool IsAiming(Player player) => aiming && player != null && player == currentPlayer;

        internal static void UpdateLocal(Player player)
        {
            if (player == null || player != Player.m_localPlayer)
                return;

            if (currentPlayer != player)
            {
                Reset();
                currentPlayer = player;
            }

            bool eligible = !player.IsDead() &&
                            CestusEquipment.HasCestusEquipped(player) &&
                            !CestusRuntime.IsCestusCasting(player) &&
                            CanTakeInput(player);
            if (!eligible)
            {
                CancelAim();
                return;
            }

            // BlockOtherInputs is only queried while the ability can be used.
            // This avoids stealing vanilla E outside of Bone Crushers combat.
            if (!aiming && (Time.time < nextUseTime ||
                            player.GetStamina() < StaminaCost ||
                            CestusInputRegistration.FacebreakerButton == null))
                return;

            // The registered Jotunn button uses BlockOtherInputs to suppress vanilla E/Use.
            // Its ZInput state can be cleared by another binding on the same key, though,
            // so it is NOT reliable for detecting a continuously held keyboard key.
            // Read the configured physical key separately, before querying the custom button.
            bool holding = CestusInputRegistration.FacebreakerKeyConfig != null &&
                           ZInput.GetKey(CestusInputRegistration.FacebreakerKeyConfig.Value);
            if (holding)
            {
                // Preserve the Jotunn-side blocking of vanilla interaction while aiming.
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
                player.IsDead() || CestusRuntime.IsCestusCasting(player) ||
                player.GetStamina() < StaminaCost || Time.time < nextUseTime)
                return;

            axis.y = 0f;
            if (axis.sqrMagnitude < 0.01f)
                return;
            axis.Normalize();
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

            player.UseStamina(StaminaCost);
            nextUseTime = Time.time + CooldownSeconds;

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
            try { return TakeInputMethod.Invoke(player, null) is bool allowed && allowed; }
            catch { return false; }
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
            CancelAim();
            Candidates.Clear();
            currentPlayer = null;
            nextUseTime = 0f;
        }
    }
}
