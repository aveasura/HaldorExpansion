using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace HaldorExpansion.Features.Cestus
{
    /// <summary>
    /// Receive movement requests on the creature's ZNetView and apply physics on its actual owner.
    /// Never directly alter a remote creature's transform on the casting client.
    /// </summary>
    internal static class CestusFacebreakerNetwork
    {
        private const string PullRpc = "aveasura_he_cestus_facebreaker_pull_v1";
        private const float MaxTargetHeightDifference = 4.5f;
        private static readonly FieldInfo CharacterView = AccessTools.Field(typeof(Character), "m_nview");

        // Apply one-way velocity AFTER vanilla movement. Never chase a moving animation
        // waypoint or reverse direction near the player: that caused the end-of-pull jitter.

        private sealed class PullState
        {
            internal bool Active;
            internal float LastPullAt = -100f;
            internal float StartedAtFixedTime;
            internal Rigidbody Body;
            internal ZNetView View;
            internal Vector3 Axis;
            internal Vector3 StartPosition;
            internal Vector3 PullDirection;
            internal float PullDistance;
            internal bool Clash;
            internal bool ReachedStop;
            internal Vector3 ReboundDirection;
            internal Vector3 ReboundStartPosition;
            internal bool ReboundStarted;
            internal bool ReboundReachedStop;
        }

        private static readonly ConditionalWeakTable<Character, PullState> PullStates =
            new ConditionalWeakTable<Character, PullState>();
        // Character.Awake and ZNetView.Awake may run in either order. Register once
        // after the view becomes valid; do not permanently miss the creature's RPC.
        private static readonly ConditionalWeakTable<ZNetView, object> RegisteredViews =
            new ConditionalWeakTable<ZNetView, object>();

        private static ZNetView GetView(Character character)
            => character == null || CharacterView == null ? null : CharacterView.GetValue(character) as ZNetView;

        internal static void Register(Character character, ZNetView view = null)
        {
            if (character == null)
                return;
            if (view == null)
                view = GetView(character);
            if (view == null || !view.IsValid() || RegisteredViews.TryGetValue(view, out _))
                return;
            view.Register<ZPackage>(PullRpc, (sender, payload) => Receive(character, view, sender, payload));
            RegisteredViews.Add(view, new object());
        }

        internal static void Send(Player caster, Character target, Vector3 origin, Vector3 axis,
            bool frontSide, bool clash)
        {
            ZNetView view = GetView(target);
            if (view == null || !view.IsValid())
                return;
            ZPackage data = new ZPackage();
            data.Write(caster.GetPlayerID());
            data.Write(origin);
            data.Write(axis);
            data.Write(frontSide);
            data.Write(clash);
            // The creature's ZNetView routes RPCs to its owner, whether host, client, or dedicated.
            view.InvokeRPC(PullRpc, data);
        }

        private static void Receive(Character target, ZNetView view, long sender, ZPackage payload)
        {
            if (target == null || payload == null || view == null || !view.IsOwner() ||
                target.IsDead() || target.IsPlayer() || target.IsBoss() ||
                target.IsTeleporting() || target.InCutscene())
                return;

            try
            {
                long casterId = payload.ReadLong();
                Vector3 origin = payload.ReadVector3();
                Vector3 axis = payload.ReadVector3();
                bool frontSide = payload.ReadBool();
                bool clash = payload.ReadBool();
                Player caster = Player.GetPlayer(casterId);
                ZNetView casterView = GetView(caster);
                // An RPC must actually come from the caster's owning peer.
                if (caster == null || caster.IsDead() || casterView == null ||
                    !casterView.IsValid() || casterView.GetZDO() == null ||
                    casterView.GetZDO().GetOwner() != sender ||
                    !BaseAI.IsEnemy(caster, target))
                    return;

                // Reject stale/forged geometries, but leave tolerance for network movement.
                if (Vector3.Distance(caster.transform.position, origin) > 2.0f ||
                    Mathf.Abs(target.transform.position.y - origin.y) > MaxTargetHeightDifference)
                    return;
                axis.y = 0f;
                if (axis.sqrMagnitude < 0.5f || axis.sqrMagnitude > 1.5f)
                    return;
                axis.Normalize();
                Vector3 delta = target.transform.position - origin;
                delta.y = 0f;
                float axial = Vector3.Dot(delta, axis);
                float lateral = Mathf.Abs(Vector3.Dot(delta, Vector3.Cross(Vector3.up, axis)));
                float padding = Mathf.Max(0f, target.GetRadius()) + 1f; // network drift margin
                if (Mathf.Abs(axial) > CestusFacebreaker.FullLength * 0.5f + padding ||
                    lateral > CestusFacebreaker.Width * 0.5f + padding ||
                    (axial > 0f) != frontSide)
                    return;

                Rigidbody body = target.GetComponent<Rigidbody>();
                if (body == null || body.isKinematic)
                    return;

                PullState state = PullStates.GetValue(target, _ => new PullState());
                if (Time.time - state.LastPullAt < 0.30f)
                    return;
                state.LastPullAt = Time.time;
                state.StartedAtFixedTime = Time.fixedTime;
                state.Body = body;
                state.View = view;
                state.StartPosition = body.position;
                state.Axis = axis;
                state.Clash = clash;

                Vector3 toTarget = body.position - origin;
                toTarget.y = 0f;
                float startingDistance = toTarget.magnitude;
                // During a two-sided grab, pull a little closer before the recoil.
                // Do not force bodies through the player: the physical contact limit
                // still depends on the colliders of the actual creatures.
                float clearance = clash ? CestusFacebreaker.ClashContactClearance :
                    CestusFacebreaker.PullBodyClearance;
                float safeDistance = Mathf.Max(CestusFacebreaker.FinalDistanceFromPlayer,
                    Mathf.Max(0f, caster.GetRadius()) + Mathf.Max(0f, target.GetRadius()) + clearance);

                state.PullDirection = startingDistance > 0.001f ? -toTarget / startingDistance : Vector3.zero;
                state.PullDistance = Mathf.Max(0f, startingDistance - safeDistance);
                state.ReachedStop = state.PullDistance <= CestusFacebreaker.PullStopTolerance;
                // The bounce follows the opposite direction of the original grab,
                // including its lateral component, not a fixed world direction.
                state.ReboundDirection = startingDistance > 0.001f ? toTarget / startingDistance :
                    axis * (frontSide ? 1f : -1f);
                state.ReboundStarted = false;
                state.ReboundReachedStop = false;

                // Even targets already in range wait until the pull phase ends before
                // receiving control. This makes the whole cast stagger together instead
                // of instantly stunning the nearest creatures on RPC delivery.
                // No coroutine: Valheim's Character.CustomFixedUpdate/SyncVelocity can
                // overwrite velocity set before the vanilla movement pass.
                state.Active = true;
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogWarning("[Cestus Facebreaker] Pull RPC rejected: " + e.Message);
            }
        }

        /// <summary>
        /// Invoked after Character.CustomFixedUpdate, so the vanilla movement system cannot
        /// immediately replace the horizontal velocity set for this pull.
        /// Only network owners with an active pull enter the physics branch.
        /// </summary>
        internal static void AfterCharacterFixedUpdate(Character target)
        {
            if (target == null || !PullStates.TryGetValue(target, out PullState state) || !state.Active)
                return;

            Rigidbody body = state.Body;
            ZNetView view = state.View;
            if (target.IsDead() || body == null || body.isKinematic ||
                view == null || !view.IsValid() || !view.IsOwner())
            {
                state.Active = false;
                return;
            }

            float elapsed = Time.fixedTime - state.StartedAtFixedTime;

            // One-sided pulls keep their original timing and only apply slow.
            // Two-sided pulls have two phases: inward impact, then a short
            // half-metre recoil. Control is applied ON IMPACT, before the recoil.
            // This is a cast-wide clash decision; it does not require OnCollisionEnter.
            if (elapsed >= CestusFacebreaker.PullDurationSeconds)
            {
                if (!state.Clash)
                {
                    FinishPull(target, body, state);
                    return;
                }

                if (!state.ReboundStarted)
                {
                    // The end of the inward pull is the shared impact moment.
                    // Apply control only once, BEFORE moving targets back out.
                    state.ReboundStarted = true;
                    state.ReboundStartPosition = body.position;
                    CestusFacebreakerSlow.Apply(target);
                    target.Stagger(state.ReboundDirection);
                }

                if (elapsed >= CestusFacebreaker.PullDurationSeconds +
                    CestusFacebreaker.ClashReboundDurationSeconds)
                {
                    FinishPull(target, body, state);
                    return;
                }

                // Recoil is ONE WAY: measure outward progress from the position at
                // impact, then stop. Never steer the creature back to a waypoint.
                if (state.ReboundReachedStop)
                {
                    StopHorizontal(body);
                    return;
                }

                Vector3 recoilDelta = body.position - state.ReboundStartPosition;
                recoilDelta.y = 0f;
                float outwardTravelled = Vector3.Dot(recoilDelta, state.ReboundDirection);
                float recoilRemaining = CestusFacebreaker.ClashReboundDistance - outwardTravelled;
                if (recoilRemaining <= CestusFacebreaker.ClashReboundStopTolerance)
                {
                    state.ReboundReachedStop = true;
                    StopHorizontal(body);
                    return;
                }

                // Preserve vertical movement, respect physics/world obstacles and
                // clamp the last fixed step to avoid overshooting or oscillation.
                float recoilSpeed = Mathf.Min(CestusFacebreaker.ClashReboundSpeed,
                    recoilRemaining / Mathf.Max(0.01f, Time.fixedDeltaTime));
                Vector3 recoil = state.ReboundDirection * recoilSpeed;
                Vector3 velocity = body.linearVelocity;
                body.linearVelocity = new Vector3(recoil.x, velocity.y, recoil.z);
                return;
            }

            // Once a target reaches its stopping point, hold it there until the
            // common impact moment. Do not re-accelerate toward the player if
            // collisions nudge it out again: that would recreate the end jitter.
            if (state.ReachedStop)
            {
                Vector3 existing = body.linearVelocity;
                body.linearVelocity = new Vector3(0f, existing.y, 0f);
                return;
            }

            Vector3 fromStart = body.position - state.StartPosition;
            fromStart.y = 0f;
            float travelled = Vector3.Dot(fromStart, state.PullDirection);
            float remaining = state.PullDistance - travelled;
            if (remaining <= CestusFacebreaker.PullStopTolerance)
            {
                state.ReachedStop = true;
                Vector3 existing = body.linearVelocity;
                body.linearVelocity = new Vector3(0f, existing.y, 0f);
                return;
            }

            // An immediate, one-direction jerk. The time budget gives a brisk start;
            // the braking cap slows just the last approach without any position chasing.
            // This only writes horizontal velocity, keeping gravity and vertical motion.
            float remainingTime = Mathf.Max(Time.fixedDeltaTime,
                CestusFacebreaker.PullDurationSeconds - elapsed);
            float intendedSpeed = remaining / remainingTime * CestusFacebreaker.PullSpeedBoost;
            float brakingSpeed = Mathf.Sqrt(2f * CestusFacebreaker.PullBrakingAcceleration *
                Mathf.Max(0f, remaining));
            // Use two-argument Min overloads: the params overload allocates per FixedUpdate.
            float speed = Mathf.Min(Mathf.Min(CestusFacebreaker.PullMaxSpeed, intendedSpeed),
                Mathf.Min(brakingSpeed, remaining / Mathf.Max(0.01f, Time.fixedDeltaTime)));
            Vector3 current = body.linearVelocity;
            Vector3 horizontal = state.PullDirection * speed;
            body.linearVelocity = new Vector3(horizontal.x, current.y, horizontal.z);
        }

        private static void StopHorizontal(Rigidbody body)
        {
            Vector3 velocity = body.linearVelocity;
            body.linearVelocity = new Vector3(0f, velocity.y, 0f);
        }

        private static void FinishPull(Character target, Rigidbody body, PullState state)
        {
            state.Active = false;
            StopHorizontal(body);
            // Clashing targets already received slow + stagger at impact.
            // One-sided targets receive slow when their ordinary pull ends.
            if (!state.Clash)
                CestusFacebreakerSlow.Apply(target);
        }
    }
}
