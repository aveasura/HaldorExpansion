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
            internal Vector3 Origin;
            internal Vector3 Axis;
            internal Vector3 StartPosition;
            internal Vector3 PullDirection;
            internal float PullDistance;
            internal float StopDistance;
            internal bool Clash;
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
                state.Origin = origin;
                state.StartPosition = body.position;
                state.Axis = axis;
                state.Clash = clash;

                Vector3 toTarget = body.position - origin;
                toTarget.y = 0f;
                float startingDistance = toTarget.magnitude;
                float safeDistance = Mathf.Max(CestusFacebreaker.FinalDistanceFromPlayer,
                    Mathf.Max(0f, caster.GetRadius()) + Mathf.Max(0f, target.GetRadius()) +
                    CestusFacebreaker.PullBodyClearance);

                // Pull toward the player, but respect the combined collider radii.
                // The cast-wide clash flag is decided from targets on BOTH sides;
                // neither a physical collision nor a crossing of the player is needed.
                state.PullDirection = startingDistance > 0.001f ? -toTarget / startingDistance : Vector3.zero;
                state.StopDistance = safeDistance;
                state.PullDistance = Mathf.Max(0f, startingDistance - safeDistance);

                if (state.PullDistance <= CestusFacebreaker.PullStopTolerance)
                {
                    // Already in range: nothing to physically pull, but the ability still
                    // applies its normal slow / clash control to an eligible target.
                    state.Active = false;
                    CestusFacebreakerSlow.Apply(target);
                    if (clash)
                        target.Stagger(-axis);
                    return;
                }

                // No coroutine: Valheim's Character.CustomFixedUpdate/SyncVelocity can overwrite
                // velocity set before the vanilla movement pass. The Postfix owns each physics tick.
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
            Vector3 fromStart = body.position - state.StartPosition;
            fromStart.y = 0f;
            float travelled = Vector3.Dot(fromStart, state.PullDirection);
            float remaining = state.PullDistance - travelled;

            // Complete as soon as the target reaches its safe stop radius. In particular,
            // do NOT pull it back if collisions or a fast physics tick carry it past the
            // destination: reversing velocity every frame was the main jitter source.
            if (remaining <= CestusFacebreaker.PullStopTolerance ||
                elapsed >= CestusFacebreaker.PullDurationSeconds)
            {
                state.Active = false;
                Vector3 existing = body.linearVelocity;
                body.linearVelocity = new Vector3(0f, existing.y, 0f);

                // Check arrival against the REAL safe stop radius, not only the
                // configurable offset: the stop radius includes both collider sizes.
                // This prevents false negatives when FinalDistanceFromPlayer is 0.3f.
                // Targets blocked farther away by a wall still receive no control.
                Vector3 fromCentre = target.transform.position - state.Origin;
                fromCentre.y = 0f;
                float allowedDistance = state.StopDistance +
                    Mathf.Max(CestusFacebreaker.PullStopTolerance, 0.30f);
                if (fromCentre.sqrMagnitude <= allowedDistance * allowedDistance)
                {
                    CestusFacebreakerSlow.Apply(target);
                    if (state.Clash)
                        target.Stagger(-state.Axis);
                }
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
    }
}
