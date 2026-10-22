using System;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace HaldorExpansion.Features.HelOath
{
    internal sealed class HelOathGhostVisual : IDisposable
    {
        private const float FadeInSeconds = 1.25f;
        private const float MistLeadFraction = 0.22f;
        private const float SpawnLift = 0.14f;
        private const float FadeOutSeconds = 2.4f;

        private static readonly int GroundMask =
            LayerMask.GetMask("terrain", "piece", "static_solid", "Default", "Default_small", "vehicle");

        // Complete circle: the Guardian may even occupy the far side of an enemy.
        private static readonly float[] GuardianAngles = { 0f, 45f, -45f, 90f, -90f, 135f, -135f, 180f };
        private static readonly float[] FloorRecallRadii = { 2.8f, 4.1f, 5.5f, 1.8f };
        private readonly HelOathGhostAppearance appearance = new HelOathGhostAppearance();
        private readonly HelOathGhostModel model = new HelOathGhostModel();
        private readonly HelOathGhostIntent intent = new HelOathGhostIntent();
        private readonly HelOathGhostDeparture departure = new HelOathGhostDeparture();
        private readonly RaycastHit[] groundHits = new RaycastHit[32];
        private GameObject root;
        private HelOathGhostCompanions companions;
        private float nextPeerTarget;
        private float nextGuardianChoice;
        private Vector3 guardianGoal;
        private bool hasGuardianGoal;
        private bool guardianPositioning;
        private bool initialMaterializing;
        private bool IsGuardian => index == HelOathGhostMotion.GuardianIndex;
        private int index;
        private float opacity;
        private float speed;
        private Vector3 destination;
        private Vector3 followOffset, focusDirection, combatAim, ownerVelocity;
        private float nextLaneCheck, nextAimCheck, blockedSeconds;
        private bool repositioning;
        private float detourSide, detourUntil, nextSteer;
        private Vector3 lastSteerGoal, steering;
        private float nextRoam;
        private float nextRetarget;
        private bool hasDestination;
        private bool resting;
        private bool finalShotStarted;
        private bool recalling;
        private bool floorRecall;
        private float floorMismatchSeconds;
        private float nextFloorRecallSearch;
        private bool disposed;
        internal bool Exists => root != null && !disposed;
        internal Vector3 Position => root.transform.position;
        internal Vector3 ReservedTarget => destination;
        internal bool HasReservedTarget => hasDestination && !resting && !departure.Retiring;

        internal bool ReservesSpace => Exists && (!recalling || opacity > 0.08f)
                                              && (!departure.Fading || opacity > 0.08f);

        internal bool Retiring => departure.Retiring;
        internal int Index => index;
        internal string Token { get; private set; }
        internal bool Recalling => recalling;

        internal bool CanFire => Exists && !recalling && departure.CanFire;

        // Recall pauses the animation, not the FIFO or its accepted arrows.
        internal bool CanKeepShot => Exists && departure.CanFire;

        // A different dungeon level is not a valid firing position. Keep FIFO
        // intact until the owner and this visual are on the same floor again.
        internal bool OnOwnerLevel(Vector3 ownerPosition) =>
            Exists && Mathf.Abs(Position.y - ownerPosition.y) <= HelOathGhostMotion.VerticalFloorGap;

        internal float ReactionDelay => model.ReactionDelay;
        internal bool HasQueuedShots => model.HasQueuedShots;
        internal Vector3 ShotOrigin => root.transform.position + Vector3.up * 1.35f + root.transform.forward * 0.35f;

        internal bool TrySwapLanding(out Vector3 position)
        {
            position = Vector3.zero;
            // Only actual active, fully materialized souls may participate.
            // They were placed and moved with obstacle/terrain checks already.
            // A second set of ground/height/capsule requirements here could
            // refuse EVERY soul during a roll, jump, slope or terrain streaming.
            if (!Exists || departure.Retiring || departure.Fading || opacity < 0.9f) return false;
            position = Position - Vector3.up * HelOathGhostMotion.GroundClearance;
            return true;
        }

        // Do NOT cancel/recreate the model: its FIFO cycle and release pose must survive swapping.
        internal void RelocateForSwap(Vector3 playerOrigin)
        {
            if (!Exists || departure.Retiring) return;
            // Owner can swap from a cliff, while falling or during a dodge.
            // A visual-only ghost has no gravity: putting it at the owner's old
            // airborne Y makes it hang there until an unrelated recall.
            // Always anchor the ghost to the ground directly BELOW the old X/Z.
            // This also finds elevated walkways that are >32m below the jump.
            float support;
            if (TrySwapGroundBelow(playerOrigin, out support))
                root.transform.position = new Vector3(playerOrigin.x, support, playerOrigin.z);
            else
                // Terrain not streamed in yet: retain an already-grounded body
                // rather than materializing a ghost in empty air.
                root.transform.position = Position;
            destination = root.transform.position;
            resting = true;
            repositioning = false;
            speed = 0f;
            blockedSeconds = 0f;
            nextSteer = 0f;
            nextGuardianChoice = 0f;
            hasGuardianGoal = false;
            guardianPositioning = IsGuardian;
            // A manual soul swap immediately establishes a new arrangement.
            // A previous floor-recovery attempt must not teleport it again.
            recalling = false;
            floorRecall = false;
            floorMismatchSeconds = 0f;
            nextFloorRecallSearch = 0f;
        }

        internal void PlaySoulSwapAura() => appearance.PlaySoulSwapAura();

        internal void Reappear() => departure.Reappear();

        internal void Retire(float grace = 0f)
        {
            bool wasRetiring = departure.Retiring;
            departure.Retire(Time.time, grace);
            if (!wasRetiring)
            {
                // Departing bodies finish their own queued action without
                // copying a newer owner draw during the final release.
                finalShotStarted = true;
                model.Detach();
            }
        }

        internal int QueueShot(float due, Vector3 direction) => model.QueueShot(due, direction);
        internal void CancelShot(int cycle) => model.CancelShot(cycle);
        internal void InterruptForUltimate() => model.InterruptForUltimate();
        internal void HoldFinalShot(float seconds) => departure.HoldRelease(Time.time + seconds);

        private bool GuardianDrawBlocked => IsGuardian && guardianPositioning && !departure.Retiring;

        internal bool Fired(Vector3 direction, int cycle)
        {
            // The Guardian must FIRST obtain his outer position. Ordinary FIFO
            // draws wait here (without loss/timeout); the ultimate retires the
            // soul and explicitly bypasses this positioning priority.
            if (!CanFire || GuardianDrawBlocked || !model.Fired(direction, cycle)) return false;
            focusDirection = direction;
            // The projectile is created before the next Tick/Move. Finish the
            // turn synchronously so even a lagged frame cannot fire backwards.
            Vector3 horizontal = direction;
            horizontal.y = 0f;
            if (horizontal.sqrMagnitude > 0.0001f)
                root.transform.rotation = Quaternion.LookRotation(horizontal);
            intent.Focus(Time.time);
            finalShotStarted = departure.Retiring;
            departure.HoldRelease(Time.time + model.ReleaseDuration);
            return true;
        }

        internal void PrepareShot(Vector3 aim)
        {
            if (!CanKeepShot) return;
            combatAim = aim;
            // A newer owner shot must not rotate an already drawing/releasing body.
            if (!model.IsAiming) focusDirection = aim - ShotOrigin;
            nextAimCheck = Time.time + ReactionDelay + 0.75f;
            nextLaneCheck = 0f;
            intent.Focus(Time.time);
        }

        internal void FinishRecall(Player owner, Quaternion formation, bool finalVolley = false)
        {
            if (!recalling || !Exists) return;
            Vector3 target = Vector3.zero;
            bool sameFloorLanding = false;
            // For a floor mismatch, a far orbit or a point on the old upper
            // landing is not a valid NORMAL follow destination. Fade back in
            // near the owner on the owner's actual floor.
            if (floorRecall)
            {
                // Tight dungeons can have no free landing spot momentarily.
                // Retry periodically instead of raycasting 48 candidates/frame.
                if (!finalVolley && Time.time < nextFloorRecallSearch) return;
                sameFloorLanding = TrySameFloorRecall(owner, formation, out target);
                if (!sameFloorLanding && !finalVolley)
                {
                    nextFloorRecallSearch = Time.time + 0.3f;
                    return;
                }
                // The terminal ultimate must retain its original recall fallback
                // if no same-floor spot is available within its short deadline.
            }

            if (!sameFloorLanding)
            {
                if (!TryGuardianGoal(owner, formation, out target))
                    target = PickDestination(owner, formation, true);
                // In cramped interiors a wide orbit may be unreachable. Prefer
                // a verified owner-level fallback instead of recalling upstairs.
                if ((!Room(target, true) || !BodyClear(target)
                                         || Mathf.Abs(target.y - owner.transform.position.y) >
                                         HelOathGhostMotion.VerticalFloorGap)
                    && TrySameFloorRecall(owner, formation, out Vector3 nearby)) target = nearby;
            }

            if (!Room(target, true) || !BodyClear(target))
            {
                float support;
                // When the new formation is crowded, an already separated body
                // can finish its volley here within the network's 30m FX bound.
                if (finalVolley && (Position - owner.transform.position).sqrMagnitude < 27f * 27f
                                && (companions == null ||
                                    companions.HasRoom(this, Position, false, HelOathGhostSpacing.Minimum))
                                && BodyClear(Position) && TryGround(Position, Position.y, out support)
                                && Mathf.Abs(support - Position.y) < 0.5f)
                {
                    recalling = false;
                    speed = 0f;
                    opacity = Mathf.Max(opacity, 0.4f);
                }

                return;
            }

            // For cross-floor recall, target already has an independently
            // verified support Y. Do not run the old nearest-to-ghost sampler
            // again, which could pick the ceiling/old upper-floor landing.
            if (sameFloorLanding)
                root.transform.position = target;
            else
            {
                float ground;
                if (!TryGround(target, owner.transform.position.y, out ground))
                    ground = owner.transform.position.y + HelOathGhostMotion.GroundClearance;
                root.transform.position = new Vector3(target.x, ground, target.z);
            }

            destination = root.transform.position;
            resting = false;
            repositioning = false;
            guardianPositioning = IsGuardian;
            hasGuardianGoal = false;
            nextGuardianChoice = 0f;
            floorRecall = false;
            floorMismatchSeconds = 0f;
            // A final volley during recall emerges from mist at the new position,
            // rather than silently consuming the soul or firing from the old location.
            opacity = 0f;
            speed = 0f;
            blockedSeconds = 0f;
            nextSteer = 0f;
            recalling = false;
        }

        // Narrow-room, same-floor recall target. Require actual walkable
        // support close to the player's feet (not world terrain far below a
        // dungeon, and not a ceiling above the player). Never bypass walls.
        private bool TrySameFloorRecall(Player owner, Quaternion formation, out Vector3 result)
        {
            result = Vector3.zero;
            Vector3 center = owner.transform.position;
            if (!TryLevelGround(center, center.y, out _)) return false;
            for (int radiusIndex = 0; radiusIndex < FloorRecallRadii.Length; radiusIndex++)
            for (int angleIndex = 0; angleIndex < 12; angleIndex++)
            {
                Vector3 direction = Quaternion.Euler(0f, index * 60f + angleIndex * 30f, 0f)
                                    * (formation * Vector3.back);
                Vector3 candidate = center + direction * FloorRecallRadii[radiusIndex];
                if (!TryLevelGround(candidate, center.y, out float support)) continue;
                candidate.y = support;
                if (!BodyClear(candidate) || !Room(candidate, true)
                                          || !RouteClear(center, candidate)) continue;
                result = candidate;
                return true;
            }

            return false;
        }

        internal static HelOathGhostVisual Create(Player owner, int index, Quaternion formation, string token,
            HelOathGhostCompanions companions)
        {
            var ghost = new HelOathGhostVisual { index = index, Token = token, companions = companions };
            try
            {
                ghost.root = new GameObject("HE_HelGhost_" + (index + 1));
                ghost.root.SetActive(false);
                ghost.destination = ghost.PickDestination(owner, formation, true);
                if (!ghost.Room(ghost.destination, true) || !BodyClear(ghost.destination))
                {
                    ghost.Dispose();
                    return null;
                }

                ghost.hasDestination = true;
                float ground;
                if (!ghost.TryGround(ghost.destination, owner.transform.position.y, out ground))
                    ground = owner.transform.position.y + HelOathGhostMotion.GroundClearance;
                ghost.root.transform.position = new Vector3(ghost.destination.x, ground, ghost.destination.z);
                ghost.root.transform.rotation = owner.transform.rotation;
                ghost.resting = true;
                ghost.nextRoam = Time.time + Random.Range(HelOathGhostMotion.IdleMinSeconds,
                    HelOathGhostMotion.IdleMaxSeconds);
                ghost.model.Build(owner, ghost.root.transform, ghost.appearance);
                ghost.appearance.AddAura(ghost.root.transform, owner.transform);
                ghost.root.SetActive(true);
                // Let the first soul be SEEN materializing beside the owner
                // before sprinting to the Guardian orbit.
                ghost.initialMaterializing = ghost.IsGuardian;
                ghost.model.Start();
                ghost.appearance.StartEffects();
                return ghost;
            }
            catch
            {
                ghost.Dispose();
                throw;
            }
        }

        private Vector3 PickDestination(Player owner, Quaternion formation, bool initial)
        {
            Vector3 center = owner.transform.position;
            Vector3 forward = owner.GetLookDir();
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) forward = formation * Vector3.forward;
            forward.Normalize();
            Vector3 current = hasDestination ? root.transform.position : center;
            Vector3 candidate = center;
            if (initial)
            {
                // Materialize the Guardian right beside the owner, like the
                // other spirits. Only AFTER he appears does his outer-orbit
                // behavior take over: players should actually see the summon.
                Vector3 local = IsGuardian
                    ? new Vector3(0f, 0f, -HelOathGhostMotion.GuardianSpawnDistance)
                    : new Vector3(index == 1 ? -HelOathGhostMotion.EscortSide : HelOathGhostMotion.EscortSide,
                        0f, -HelOathGhostMotion.EscortRear);
                for (int turn = 0; turn < 7; turn++)
                {
                    candidate = center + Quaternion.Euler(0f, (turn % 2 == 0 ? 1 : -1) *
                                                              ((turn + 1) / 2) * 28f, 0f) * (formation * local);
                    float height;
                    if (!TryGround(candidate, center.y, out height) || Mathf.Abs(height - center.y) > 2.5f) continue;
                    candidate.y = height;
                    if (Room(candidate, true) && BodyClear(candidate) && RouteClear(center, candidate))
                        return candidate;
                }
            }

            for (int attempt = 0; attempt < 16; attempt++)
            {
                Vector3 direction = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * forward;
                // Full circle is available, but the forward firing lane is chosen less often.
                if (Vector3.Dot(direction, forward) > 0.45f && Random.value < 0.75f) continue;
                float radius = initial
                    ? Random.Range(HelOathGhostMotion.SpawnFallbackMinRadius,
                        HelOathGhostMotion.SpawnFallbackMaxRadius)
                    : Random.Range(HelOathGhostMotion.PatrolMinRadius,
                        HelOathGhostMotion.PatrolMaxRadius);
                candidate = center + direction * radius;
                float support;
                if (!TryGround(candidate, center.y, out support) || Mathf.Abs(support - center.y) > 2.5f) continue;
                candidate.y = support;
                if (!Room(candidate, true) || !BodyClear(candidate) ||
                    !RouteClear(initial ? center : current, candidate)) continue;
                if (initial) return candidate;
                Vector3 leg = candidate - current;
                leg.y = 0f;
                if (leg.sqrMagnitude < HelOathGhostMotion.MinPatrolLeg * HelOathGhostMotion.MinPatrolLeg) continue;
                // Prefer routes around the owner instead of walking through their body.
                Vector3 toCenter = center - current;
                toCenter.y = 0f;
                float t = Mathf.Clamp01(Vector3.Dot(toCenter, leg) / leg.sqrMagnitude);
                if ((toCenter - leg * t).sqrMagnitude < 2.25f) continue;
                return candidate;
            }

            // Deterministic short-range alternatives also separate simultaneous
            // spawns and recalls in places where the normal patrol ring cannot fit.
            if (initial)
                for (int attempt = 0; attempt < 24; attempt++)
                {
                    float radius = HelOathGhostSpacing.Preferred * (1 + attempt / 8);
                    candidate = center + Quaternion.Euler(0f, index * 120f + (attempt % 8) * 45f, 0f) *
                        Vector3.forward * radius;
                    float ground;
                    if (!TryGround(candidate, center.y, out ground) || Mathf.Abs(ground - center.y) > 2.5f) continue;
                    candidate.y = ground;
                    if (Room(candidate, true) && BodyClear(candidate) && RouteClear(center, candidate))
                        return candidate;
                }

            return initial ? center : current;
        }

        // Slot 0 is permanently a distant anchor, not a follower with an
        // occasional special task. Pick a walkable point on a 360-degree ring.
        // Visibility is a preference, NEVER a reason to collapse next to owner.
        private bool TryGuardianGoal(Player owner, Quaternion formation, out Vector3 result)
        {
            result = Vector3.zero;
            if (!IsGuardian || companions == null) return false;
            bool combat = companions.HasThreat;
            Vector3 center = owner.transform.position;
            Vector3 rear = combat ? center - companions.ThreatPosition : -(formation * Vector3.forward);
            rear.y = 0f;
            if (rear.sqrMagnitude < 0.01f) rear = Vector3.back;
            rear.Normalize();
            Vector3 aim = model.HasQueuedShots ? combatAim :
                combat ? companions.ThreatPosition : Vector3.zero;
            float close = combat ? HelOathGhostMotion.GuardianCombatNear : HelOathGhostMotion.GuardianTravelNear;
            float far = combat ? HelOathGhostMotion.GuardianCombatFar : HelOathGhostMotion.GuardianTravelFar;

            if (hasGuardianGoal)
            {
                Vector3 offset = guardianGoal - center;
                offset.y = 0f;
                float radius = offset.magnitude;
                // World-space position stays stable when the owner wiggles or
                // aims. Even a blocked firing lane does not force a retreat.
                if (Mathf.Abs(guardianGoal.y - center.y) <= HelOathGhostMotion.GuardianLevelTolerance
                    && radius >= close - 1.5f && radius <= far + 2f && BodyClear(guardianGoal))
                {
                    result = guardianGoal;
                    return true;
                }

                if (Mathf.Abs(guardianGoal.y - center.y) <= HelOathGhostMotion.GuardianLevelTolerance
                    && Time.time < nextGuardianChoice && radius > 9f && radius < 23f)
                {
                    result = guardianGoal;
                    return true;
                }
            }

            // A failed outer-orbit search must also respect the retry interval.
            // Without a goal, cramped rooms otherwise repeat all 16 probes each frame.
            if (!hasGuardianGoal && Time.time < nextGuardianChoice) return false;
            nextGuardianChoice = Time.time + 0.8f;

            Vector3 fromOwner = Position - center;
            fromOwner.y = 0f;
            float currentDistance = fromOwner.magnitude;
            if (Mathf.Abs(Position.y - center.y) <= HelOathGhostMotion.GuardianLevelTolerance
                && currentDistance >= close - 1f && currentDistance <= far + 1f
                && BodyClear(Position) && Room(Position, false))
            {
                guardianGoal = Position;
                hasGuardianGoal = true;
                result = guardianGoal;
                return true;
            }

            float best = float.PositiveInfinity;
            Vector3 bestGoal = Vector3.zero;
            bool found = false;
            for (int angle = 0; angle < GuardianAngles.Length; angle++)
            for (int distanceIndex = 0; distanceIndex < 2; distanceIndex++)
            {
                // Examine the outer orbit first; reaching it has priority over
                // being behind the owner or immediately having line of fire.
                float radius = distanceIndex == 0 ? far : close;
                Vector3 direction = Quaternion.Euler(0f, GuardianAngles[angle], 0f) * rear;
                Vector3 candidate = center + direction * radius;
                float support;
                if (!TryGround(candidate, center.y, out support)
                    || Mathf.Abs(support - center.y) > HelOathGhostMotion.GuardianLevelTolerance) continue;
                candidate.y = support;
                if (!Room(candidate, true) || !BodyClear(candidate)) continue;
                Vector3 step = candidate - Position;
                step.y = 0f;
                // Local steering can navigate an obstacle. Do not require a
                // perfectly straight 19m corridor to accept an outer goal.
                if (step.sqrMagnitude > 1f && !RouteClear(Position, candidate)
                                           && !StepRouteClear(Position, step.normalized, 1.3f)) continue;
                bool line = !combat || ClearLane(candidate + Vector3.up * 1.35f, aim);
                float score = (candidate - Position).sqrMagnitude * 0.05f
                              + (distanceIndex == 0 ? 0f : 7f)
                              + (line ? 0f : 5f);
                if (score >= best) continue;
                best = score;
                bestGoal = candidate;
                found = true;
            }

            if (found)
            {
                guardianGoal = bestGoal;
                hasGuardianGoal = true;
            }
            else if (hasGuardianGoal)
            {
                // Retain the distant role when terrain/LOS temporarily blocks
                // re-planning. If the owner moves too far, retry next scan.
                Vector3 offset = guardianGoal - center;
                offset.y = 0f;
                if (offset.sqrMagnitude > 26f * 26f || !BodyClear(guardianGoal)
                                                    || Mathf.Abs(guardianGoal.y - center.y) >
                                                    HelOathGhostMotion.GuardianLevelTolerance)
                    hasGuardianGoal = false;
            }

            if (!hasGuardianGoal) return false;
            result = guardianGoal;
            return true;
        }

        private Vector3 Target(Player owner, Quaternion formation)
        {
            Vector3 offset = root.transform.position - owner.transform.position;
            offset.y = 0f;
            bool wasFollowing = intent.Following;
            if (IsGuardian)
                intent.Update(Time.time, ownerVelocity.magnitude, offset.magnitude, model.IsAiming);
            else
                intent.UpdateEscort(Time.time, ownerVelocity.magnitude, offset.magnitude, model.IsAiming);
            if (model.IsAiming) focusDirection = model.AimDirection;
            Vector3 guardianPosition;
            if (TryGuardianGoal(owner, formation, out guardianPosition))
            {
                Vector3 remain = guardianPosition - Position;
                remain.y = 0f;
                repositioning = remain.sqrMagnitude > 0.55f * 0.55f;
                resting = !repositioning;
                Vector3 fromOwner = Position - owner.transform.position;
                fromOwner.y = 0f;
                float minimum = companions.HasThreat
                    ? HelOathGhostMotion.GuardianCombatNear
                    : HelOathGhostMotion.GuardianTravelNear;
                guardianPositioning = fromOwner.sqrMagnitude < (minimum - 0.5f) * (minimum - 0.5f);
                return destination = guardianPosition;
            }

            // Rare cramped terrain fallback: allow firing rather than blocking
            // a FIFO indefinitely if no outer formation point can be found.
            guardianPositioning = false;
            if (intent.Following)
            {
                if (ownerVelocity.sqrMagnitude < 0.36f
                    && offset.sqrMagnitude >=
                    HelOathGhostMotion.EscortHoldMinRadius * HelOathGhostMotion.EscortHoldMinRadius
                    && offset.sqrMagnitude <
                    HelOathGhostMotion.EscortHoldMaxRadius * HelOathGhostMotion.EscortHoldMaxRadius)
                {
                    // Brake in place rather than reversing toward a slot behind our feet.
                    resting = true;
                    repositioning = false;
                    return destination = root.transform.position;
                }

                if (!wasFollowing)
                {
                    // Fixed left/right slots keep escorts from drifting onto
                    // the same side while the player sprints or pivots.
                    followOffset = new Vector3(index == 1
                        ? -HelOathGhostMotion.EscortSide
                        : HelOathGhostMotion.EscortSide, 0f, -HelOathGhostMotion.EscortRear);
                }

                resting = false;
                repositioning = false;
                Vector3 lead = Vector3.ClampMagnitude(ownerVelocity, HelOathGhostMotion.EscortMaxLeadSpeed)
                               * HelOathGhostMotion.EscortLeadSeconds;
                // A detour slot chosen around a wall/peer must not become a
                // permanent wide orbit. Return to the assigned flank as soon
                // as that nearby destination is available again.
                Vector3 regularOffset = new Vector3(index == 1
                    ? -HelOathGhostMotion.EscortSide
                    : HelOathGhostMotion.EscortSide, 0f, -HelOathGhostMotion.EscortRear);
                Vector3 regularGoal = owner.transform.position + formation * regularOffset + lead;
                if (Room(regularGoal, true) && BodyClear(regularGoal)
                                            && TryGround(regularGoal, Position.y, out float regularSupport)
                                            && Mathf.Abs(regularSupport - Position.y) < 1.5f)
                    followOffset = regularOffset;
                Vector3 goal = owner.transform.position + formation * followOffset + lead;
                if (!Room(goal, true) && Time.time >= nextPeerTarget)
                {
                    nextPeerTarget = Time.time + 0.4f;
                    Vector3 alternate;
                    if (FindRoomNear(owner, goal, out alternate))
                    {
                        followOffset = Quaternion.Inverse(formation) * (alternate - owner.transform.position - lead);
                        followOffset.y = 0f;
                        goal = alternate;
                    }
                }

                return destination = goal;
            }

            if (wasFollowing)
            {
                destination = root.transform.position;
                resting = true;
                nextRoam = Time.time + Random.Range(HelOathGhostMotion.IdleMinSeconds,
                    HelOathGhostMotion.IdleMaxSeconds);
            }

            if (intent.Focused(Time.time))
            {
                if (!model.HasQueuedShots && Time.time >= nextAimCheck)
                {
                    nextAimCheck = Time.time + 0.25f;
                    combatAim = LookPoint(owner);
                }

                if (Time.time >= nextLaneCheck)
                {
                    nextLaneCheck = Time.time + 0.4f;
                    if (ClearLane(ShotOrigin, combatAim)) repositioning = false;
                    else if (!repositioning || !ClearLane(destination + Vector3.up * 1.35f, combatAim))
                        repositioning = FindFiringPosition(owner, combatAim, out destination);
                }

                if (!repositioning) destination = root.transform.position;
                resting = !repositioning;
                nextRoam = Time.time + HelOathGhostMotion.IdleMinSeconds;
                return destination;
            }

            repositioning = false;
            Vector3 remaining = destination - root.transform.position;
            remaining.y = 0f;
            if (!resting && remaining.sqrMagnitude < 0.01f && speed < 0.15f)
            {
                resting = true;
                nextRoam = Time.time + Random.Range(HelOathGhostMotion.IdleMinSeconds,
                    HelOathGhostMotion.IdleMaxSeconds);
            }

            if (resting && Time.time >= nextRoam && Time.time >= nextRetarget)
            {
                destination = PickDestination(owner, formation, false);
                nextRetarget = Time.time + 0.8f;
                resting = false;
            }

            return destination;
        }

        private Vector3 LookPoint(Player owner)
        {
            Vector3 origin = owner.transform.position + Vector3.up * 1.5f;
            Vector3 direction = owner.GetLookDir().normalized;
            int hits = Physics.RaycastNonAlloc(origin, direction, groundHits, 120f,
                HelOathGhostBolt.CollisionMask, QueryTriggerInteraction.Ignore);
            float distance = 120f;
            for (int h = 0; h < hits; h++)
                if (groundHits[h].collider.GetComponentInParent<Character>() != owner)
                    distance = Mathf.Min(distance, groundHits[h].distance);
            return origin + direction * distance;
        }

        private static bool BodyClear(Vector3 position) => !Physics.CheckCapsule(
            position + Vector3.up * 0.55f, position + Vector3.up * 1.35f,
            0.25f, GroundMask, QueryTriggerInteraction.Ignore);

        private static bool RouteClear(Vector3 from, Vector3 to)
        {
            Vector3 leg = to - from;
            return leg.sqrMagnitude < 0.001f || !Physics.SphereCast(from + Vector3.up * 0.75f,
                0.25f, leg.normalized, out _, leg.magnitude, GroundMask, QueryTriggerInteraction.Ignore);
        }

        private static bool ClearLane(Vector3 origin, Vector3 aim)
        {
            Vector3 leg = aim - origin;
            float distance = Mathf.Max(0f, leg.magnitude - 0.35f);
            return !Physics.CheckSphere(origin, 0.06f, GroundMask, QueryTriggerInteraction.Ignore)
                   && (distance < 0.001f || !Physics.SphereCast(origin, 0.06f, leg.normalized,
                       out _, distance, GroundMask, QueryTriggerInteraction.Ignore));
        }

        private bool FindFiringPosition(Player owner, Vector3 aim, out Vector3 result)
        {
            Vector3 current = root.transform.position;
            result = current;
            float best = float.PositiveInfinity;
            Vector3 forward = aim - owner.transform.position;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) forward = owner.transform.forward;
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            for (int i = 0; i < 14; i++)
            {
                Vector3 candidate = i < 6
                    ? current + right * ((i % 2 == 0 ? 1f : -1f) * (1f + i / 2))
                    : owner.transform.position + Quaternion.Euler(0f, (i - 6) * 45f, 0f) * forward * 3.5f;
                Vector3 offset = candidate - owner.transform.position;
                offset.y = 0f;
                if (offset.sqrMagnitude > 64f || offset.sqrMagnitude < 1.4f) continue;
                float ground;
                if (!TryGround(candidate, current.y, out ground) || Mathf.Abs(ground - current.y) > 1.5f) continue;
                candidate.y = ground;
                if (!Room(candidate, true) || !BodyClear(candidate) || !RouteClear(current, candidate)
                    || !ClearLane(candidate + Vector3.up * 1.35f, aim)) continue;
                float score = (candidate - current).sqrMagnitude;
                if (Vector3.Dot(offset.normalized, forward) > 0.7f) score += 12f;
                if (score >= best) continue;
                best = score;
                result = candidate;
            }

            return !float.IsPositiveInfinity(best);
        }

        internal bool Tick(Player owner, Quaternion formation, bool wanted, bool releaseSoul, Vector3 velocity,
            float dt)
        {
            if (root == null) return false;
            ownerVelocity = velocity;
            if (releaseSoul) Retire();
            if (!wanted && departure.Retiring && departure.Waiting(Time.time)) wanted = true;
            if (departure.Fading && departure.Retiring) wanted = false;
            if (!wanted)
            {
                if (!departure.Fading) model.CancelActions();
                departure.BeginFade(releaseSoul);
                opacity = Mathf.MoveTowards(opacity, 0f, dt / FadeOutSeconds);
                Vector3 motion = Move(owner, root.transform.position, true, false, dt);
                model.Animate(root.transform.InverseTransformDirection(motion), false, dt);
                appearance.SetFade(BodyFade(opacity), Time.time, index, departure.Retiring, opacity);
                return opacity > 0f;
            }

            departure.Reappear();
            Vector3 target = departure.Retiring ? root.transform.position : Target(owner, formation);
            Vector3 separation = owner.transform.position - root.transform.position;
            separation.y = 0f;
            if (!departure.Retiring && !recalling)
            {
                // A ghost standing directly above/below the owner has nearly
                // zero horizontal separation, so the old 30m recall never fired.
                // A verified landing at the owner's feet suppresses false
                // recalls mid-jump, mid-fall and across unloaded interior cells.
                if (Mathf.Abs(owner.transform.position.y - Position.y) > HelOathGhostMotion.VerticalFloorGap
                    && TryLevelGround(owner.transform.position, owner.transform.position.y, out _))
                    floorMismatchSeconds += dt;
                else floorMismatchSeconds = 0f;
                if (floorMismatchSeconds >= HelOathGhostMotion.VerticalRecallDelay)
                {
                    floorRecall = true;
                    recalling = true;
                }
                else if (separation.sqrMagnitude > (IsGuardian
                             ? HelOathGhostMotion.RecallDistance
                             : HelOathGhostMotion.EscortRecallDistance)
                         * (IsGuardian ? HelOathGhostMotion.RecallDistance : HelOathGhostMotion.EscortRecallDistance)
                         || (blockedSeconds > (IsGuardian ? 3f : HelOathGhostMotion.EscortBlockedRecallSeconds)
                             && separation.sqrMagnitude > (IsGuardian
                                 ? 100f
                                 : HelOathGhostMotion.EscortBlockedRecallDistance *
                                   HelOathGhostMotion.EscortBlockedRecallDistance)))
                {
                    // Horizontal recall should also target the correct dungeon
                    // level if the owner has descended since the ghost got stuck.
                    floorRecall = !OnOwnerLevel(owner.transform.position);
                    recalling = true;
                }
            }

            if (recalling)
            {
                opacity = Mathf.MoveTowards(opacity, 0f, dt / 0.25f);
                if (opacity <= 0f) FinishRecall(owner, formation);
                appearance.SetFade(BodyFade(opacity), Time.time, index, false, opacity);
                return true;
            }

            opacity = Mathf.MoveTowards(opacity, 1f, dt / FadeInSeconds);
            bool appearing = initialMaterializing && opacity < 0.95f;
            if (!appearing) initialMaterializing = false;
            bool focused = intent.Focused(Time.time) || model.IsAiming;
            // Follow movement heading except during the final draw and release.
            // A newer owner shot never changes the current shot's facing.
            bool shotFacing = model.ShouldFaceShot && (!IsGuardian || !guardianPositioning
                                                                   || departure.Retiring || model.ReleasingShot);
            if (shotFacing)
                focusDirection = model.ReleasingShot ? model.AimDirection : combatAim - ShotOrigin;
            bool stop = appearing || shotFacing || departure.Retiring || resting
                        || (focused && !intent.Following && !repositioning);
            Vector3 movement = Move(owner, target, stop, focused, dt, shotFacing);
            // Block normal drawing and firing until the Guardian reaches its orbit.
            // Pending FIFO shots wait; the ultimate retains priority.
            bool allowDraw = (!IsGuardian || (!appearing && !GuardianDrawBlocked))
                             && (departure.Retiring || OnOwnerLevel(owner.transform.position));
            model.Animate(root.transform.InverseTransformDirection(movement),
                !departure.Retiring || !finalShotStarted, dt, allowDraw);
            float bodyFade = BodyFade(opacity);
            model.SetRise(-SpawnLift * (1f - bodyFade));
            appearance.SetFade(bodyFade, Time.time, index, false, opacity);
            return true;
        }

        private bool Room(Vector3 point, bool reservations) =>
            companions == null || companions.HasRoom(this, point, reservations);

        private bool PeerStep(Vector3 from, Vector3 to) => companions == null || companions.CanStep(this, from, to);

        private bool FindRoomNear(Player owner, Vector3 goal, out Vector3 result)
        {
            result = goal;
            for (int i = 0; i < 8; i++)
            {
                Vector3 candidate = goal + Quaternion.Euler(0f, index * 120f + i * 45f, 0f) * Vector3.forward *
                    HelOathGhostSpacing.Preferred;
                Vector3 fromOwner = candidate - owner.transform.position;
                fromOwner.y = 0f;
                Vector3 settledOffset = fromOwner - ownerVelocity * 0.3f;
                if (fromOwner.sqrMagnitude > 121f || settledOffset.sqrMagnitude > 81f) continue;
                float ground;
                if (!TryGround(candidate, Position.y, out ground) || Mathf.Abs(ground - Position.y) > 2.5f) continue;
                candidate.y = ground;
                if (!Room(candidate, true) || !BodyClear(candidate) || !RouteClear(Position, candidate)) continue;
                result = candidate;
                return true;
            }

            return false;
        }

        private Vector3 Move(Player owner, Vector3 target, bool stop, bool aiming, float dt, bool shotFacing = false)
        {
            Vector3 old = root.transform.position;
            // Existing overlaps (e.g. a returning body) separate with real walking
            // animation. Departing bodies finish their release without being shoved.
            if (!shotFacing && !departure.Retiring && !departure.Fading && companions != null)
            {
                Vector3 separation = companions.Separation(this);
                if (separation.sqrMagnitude > 0.001f)
                {
                    target = old + separation.normalized * 0.65f;
                    stop = false;
                }
            }

            Vector3 delta = target - old;
            delta.y = 0f;
            float distance = delta.magnitude;
            Vector3 desiredDirection = !stop && distance > 0.001f
                ? Steer(old, delta / distance, Mathf.Max(1f, speed * 0.35f))
                : Vector3.zero;
            bool hasRoute = !stop && desiredDirection.sqrMagnitude > 0.001f;
            // The Guardian runs to its distant orbit after materializing.
            bool follow = !departure.Retiring && (intent.Following
                                                  || (IsGuardian && guardianPositioning));

            // One facing for locomotion and the actual translation. Previously
            // the body aimed at the shot target while a separate heading kept
            // following the owner; this is what produced sideways skating.
            // Complete the braking step before rotating to aim at rest.
            Vector3 face = shotFacing ? focusDirection
                : hasRoute ? desiredDirection
                : speed > HelOathGhostMotion.StopBeforeAimingSpeed ? root.transform.forward
                : aiming ? focusDirection : Vector3.zero;
            face.y = 0f;
            if (face.sqrMagnitude > 0.001f)
                root.transform.rotation = Quaternion.RotateTowards(root.transform.rotation,
                    Quaternion.LookRotation(face), (shotFacing ? HelOathGhostMotion.ShotTurnDegrees
                        : hasRoute ? follow
                            ? HelOathGhostMotion.FollowTurnDegrees
                            : HelOathGhostMotion.PatrolTurnDegrees
                        : HelOathGhostMotion.AimTurnDegrees) * Mathf.Max(0f, dt));

            if (shotFacing)
            {
                // Rotate in place, never translate along the shot heading.
                // This prevents backwards pursuit and sideways skating while
                // braking for the brief shot-facing window.
                speed = HelOathGhostMotion.FacingSpeed(speed, 0f, 1f, dt, follow, true);
                blockedSeconds = 0f;
                return Vector3.zero;
            }

            Vector3 heading = root.transform.forward;
            heading.y = 0f;
            heading.Normalize();
            float alignment = hasRoute ? Mathf.Clamp(Vector3.Dot(heading, desiredDirection), -1f, 1f) : 1f;
            Vector3 ownerOffset = owner.transform.position - old;
            ownerOffset.y = 0f;
            bool escortCatchUp = !IsGuardian && follow
                                             && ownerOffset.sqrMagnitude > HelOathGhostMotion.EscortCatchUpDistance *
                                             HelOathGhostMotion.EscortCatchUpDistance;
            float followMaximum = escortCatchUp
                ? HelOathGhostMotion.EscortCatchUpMaxSpeed
                : HelOathGhostMotion.FollowSpeed;
            float wantedSpeed = !hasRoute
                ? 0f
                : follow && ownerVelocity.sqrMagnitude > 0.36f
                    ? HelOathGhostMotion.PursuitSpeed(distance,
                        distance > 0.01f ? Vector3.Dot(ownerVelocity, delta / distance) : 0f, followMaximum)
                    : HelOathGhostMotion.DesiredSpeed(distance,
                        follow ? followMaximum : HelOathGhostMotion.WalkSpeed);
            speed = HelOathGhostMotion.FacingSpeed(speed, wantedSpeed, alignment, dt, follow, !hasRoute);

            // Always move forward in the rendered model's coordinates. Even
            // during an active bow draw, the owner's aim cannot make a runner
            // strafe. When standing still, independent bow aiming is unchanged.
            Vector3 next = old + heading * (speed * Mathf.Max(0f, dt));
            float support;
            if (TryGround(next, old.y, out support) && support - old.y <= 0.65f && old.y - support < 1.5f)
            {
                next.y = HelOathGhostMotion.GroundY(old.y, support, dt);
                if (!BodyClear(next) || !RouteClear(old, next) || !PeerStep(old, next))
                {
                    next = old;
                    speed = 0f;
                }
            }
            else
            {
                next = old;
                speed = 0f;
            }

            if (!stop && distance > 1f && (next - old).sqrMagnitude < 0.000001f) blockedSeconds += dt;
            else blockedSeconds = 0f;
            root.transform.position = next;
            Vector3 motion = dt > 0.0001f ? (next - old) / dt : Vector3.zero;
            motion.y = 0f;
            return motion;
        }

        private Vector3 Steer(Vector3 origin, Vector3 desired, float distance)
        {
            if (Time.time < nextSteer && Vector3.Dot(lastSteerGoal, desired) > 0.96f
                                      && PeerStep(origin, origin + steering * distance)) return steering;
            nextSteer = Time.time + 0.12f;
            lastSteerGoal = desired;
            steering = Vector3.zero;
            if (StepRouteClear(origin, desired, distance)) return steering = desired;
            for (int angle = 35; angle <= 105; angle += 35)
            {
                for (int sideIndex = 0; sideIndex < 2; sideIndex++)
                {
                    float side = Time.time < detourUntil ? detourSide : (sideIndex == 0 ? 1f : -1f);
                    if (Time.time < detourUntil && sideIndex > 0) break;
                    Vector3 candidate = Quaternion.Euler(0f, angle * side, 0f) * desired;
                    if (!StepRouteClear(origin, candidate, distance)) continue;
                    detourSide = side;
                    detourUntil = Time.time + 0.8f;
                    return steering = candidate;
                }
            }

            return steering;
        }

        private bool StepRouteClear(Vector3 origin, Vector3 direction, float distance)
        {
            Vector3 end = origin + direction * distance;
            float ground;
            if (!TryGround(end, origin.y, out ground) || Mathf.Abs(ground - origin.y) > distance * 1.2f) return false;
            end.y = ground;
            return BodyClear(end) && RouteClear(origin, end) && PeerStep(origin, end);
        }

        private static float BodyFade(float amount) => Mathf.SmoothStep(0f, 1f,
            Mathf.InverseLerp(MistLeadFraction, 1f, amount));

        // Swap-specific probe: unlike normal footstep sampling, it searches
        // DOWN from the airborne owner's previous position (up to 256 metres).
        // Never accept a roof/platform ABOVE the point where the owner swapped.
        private bool TrySwapGroundBelow(Vector3 airborne, out float support)
        {
            support = float.NegativeInfinity;
            for (int i = 0; i < 5; i++)
            {
                Vector3 point = airborne;
                float radius = HelOathGhostMotion.FootprintRadius;
                if (i == 1) point.x += radius;
                else if (i == 2) point.x -= radius;
                else if (i == 3) point.z += radius;
                else if (i == 4) point.z -= radius;
                float terrain;
                if (ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(point, out terrain)
                                                && terrain <= airborne.y + 0.25f)
                    support = Mathf.Max(support, terrain + HelOathGhostMotion.GroundClearance);
                int hits = Physics.RaycastNonAlloc(
                    new Vector3(point.x, airborne.y + 0.4f, point.z), Vector3.down,
                    groundHits, 256f, GroundMask, QueryTriggerInteraction.Ignore);
                for (int h = 0; h < hits; h++)
                {
                    RaycastHit hit = groundHits[h];
                    if (hit.normal.y < 0.25f || hit.point.y > airborne.y + 0.25f
                                             || hit.collider.GetComponentInParent<Character>() != null) continue;
                    support = Mathf.Max(support, hit.point.y + HelOathGhostMotion.GroundClearance);
                }
            }

            return !float.IsNegativeInfinity(support);
        }

        // Probe for a walkable platform close to this exact Y level. The normal
        // TryGround intentionally prefers the ghost's own current floor; that
        // preference must NOT be used for cross-floor recall in a dungeon.
        private bool TryLevelGround(Vector3 sample, float ownerFootY, out float support)
        {
            float best = float.PositiveInfinity;
            support = float.NegativeInfinity;
            float terrain;
            if (ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(sample, out terrain))
            {
                float difference = Mathf.Abs(terrain - ownerFootY);
                if (difference <= HelOathGhostMotion.LevelGroundTolerance)
                {
                    best = difference;
                    support = terrain + HelOathGhostMotion.GroundClearance;
                }
            }

            Vector3 origin = new Vector3(sample.x, ownerFootY + 1.65f, sample.z);
            int count = Physics.RaycastNonAlloc(origin, Vector3.down, groundHits, 4f,
                GroundMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = groundHits[i];
                if (hit.normal.y < 0.35f || hit.collider.GetComponentInParent<Character>() != null) continue;
                float difference = Mathf.Abs(hit.point.y - ownerFootY);
                if (difference > HelOathGhostMotion.LevelGroundTolerance || difference >= best) continue;
                best = difference;
                support = hit.point.y + HelOathGhostMotion.GroundClearance;
            }

            return best < float.PositiveInfinity;
        }

        private bool TryGround(Vector3 position, float referenceY, out float support)
        {
            support = float.NegativeInfinity;
            // Sample the center and both feet's surrounding footprint, not a single
            // point which can leave one side of the body inside a slope.
            float radius = HelOathGhostMotion.FootprintRadius;
            for (int i = 0; i < 5; i++)
            {
                Vector3 p = position;
                if (i == 1) p.x += radius;
                else if (i == 2) p.x -= radius;
                else if (i == 3) p.z += radius;
                else if (i == 4) p.z -= radius;
                float terrain = float.NegativeInfinity;
                bool hasTerrain = ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(p, out terrain);
                if (!hasTerrain) terrain = float.NegativeInfinity;
                float originY = Mathf.Max(referenceY + 1.5f, terrain + 1.5f);
                int hits = Physics.RaycastNonAlloc(new Vector3(p.x, originY, p.z), Vector3.down,
                    groundHits, 32f, GroundMask, QueryTriggerInteraction.Ignore);
                float closest = float.PositiveInfinity;
                float surface = terrain;
                for (int h = 0; h < hits; h++)
                {
                    RaycastHit hit = groundHits[h];
                    if (hit.normal.y < 0.25f || hit.point.y < terrain - 0.05f) continue;
                    float difference = Mathf.Abs(hit.point.y - referenceY);
                    // Prefer the floor nearest our current level, not an overhead roof.
                    if (difference < closest)
                    {
                        closest = difference;
                        surface = hit.point.y;
                    }
                }

                if (!float.IsNegativeInfinity(surface))
                    support = Mathf.Max(support, surface + HelOathGhostMotion.GroundClearance);
            }

            return !float.IsNegativeInfinity(support);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (root != null)
            {
                root.SetActive(false);
                Object.Destroy(root);
            }

            appearance.Dispose();
        }
    }
}