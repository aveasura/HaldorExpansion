using System;
using UnityEngine;

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

        private readonly HelOathGhostAppearance appearance = new HelOathGhostAppearance();
        private readonly HelOathGhostModel model = new HelOathGhostModel();
        private readonly HelOathGhostIntent intent = new HelOathGhostIntent();
        private readonly HelOathGhostDeparture departure = new HelOathGhostDeparture();
        private readonly RaycastHit[] groundHits = new RaycastHit[32];
        private GameObject root;
        private HelOathGhostCompanions companions;
        private float nextPeerTarget;
        private int index;
        private float opacity;
        private float speed;
        private Vector3 travelDirection;
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
        internal float ReactionDelay => model.ReactionDelay;
        internal Vector3 ShotOrigin => root.transform.position + Vector3.up * 1.35f + root.transform.forward * 0.35f;

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

        internal int QueueShot(float due) => model.QueueShot(due);
        internal void CancelShot(int cycle) => model.CancelShot(cycle);

        internal bool Fired(Vector3 direction, int cycle)
        {
            if (!CanFire || !model.Fired(direction, cycle)) return false;
            focusDirection = direction;
            intent.Focus(Time.time);
            finalShotStarted = departure.Retiring;
            departure.HoldRelease(Time.time + model.ReleaseDuration);
            return true;
        }

        internal void PrepareShot(Vector3 aim)
        {
            if (!CanFire) return;
            combatAim = aim;
            focusDirection = aim - ShotOrigin;
            nextAimCheck = Time.time + ReactionDelay + 0.75f;
            nextLaneCheck = 0f;
            intent.Focus(Time.time);
        }

        internal void FinishRecall(Player owner, Quaternion formation, bool finalVolley = false)
        {
            if (!recalling || !Exists) return;
            Vector3 target = PickDestination(owner, formation, true);
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

            float ground;
            if (!TryGround(target, owner.transform.position.y, out ground))
                ground = owner.transform.position.y + HelOathGhostMotion.GroundClearance;
            root.transform.position = new Vector3(target.x, ground, target.z);
            destination = root.transform.position;
            // A final volley during recall emerges from mist at the new position,
            // rather than silently consuming the soul or firing from the old location.
            opacity = 0f;
            speed = 0f;
            blockedSeconds = 0f;
            nextSteer = 0f;
            travelDirection = Vector3.zero;
            recalling = false;
        }

        internal bool ClearShot(Vector3 aim) => CanFire && ClearLane(ShotOrigin, aim);

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
                ghost.nextRoam = Time.time + UnityEngine.Random.Range(HelOathGhostMotion.IdleMinSeconds,
                    HelOathGhostMotion.IdleMaxSeconds);
                ghost.model.Build(owner, ghost.root.transform, ghost.appearance);
                ghost.appearance.AddAura(ghost.root.transform, owner.transform);
                ghost.root.SetActive(true);
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
            for (int attempt = 0; attempt < 16; attempt++)
            {
                Vector3 direction = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f) * forward;
                // Full circle is available, but the forward firing lane is chosen less often.
                if (Vector3.Dot(direction, forward) > 0.45f && UnityEngine.Random.value < 0.75f) continue;
                float radius = UnityEngine.Random.Range(HelOathGhostMotion.PatrolMinRadius,
                    initial ? 4.5f : HelOathGhostMotion.PatrolMaxRadius);
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

        private Vector3 Target(Player owner, Quaternion formation)
        {
            Vector3 offset = root.transform.position - owner.transform.position;
            offset.y = 0f;
            bool wasFollowing = intent.Following;
            intent.Update(Time.time, ownerVelocity.magnitude, offset.magnitude, model.IsAiming);
            if (model.IsAiming) focusDirection = model.AimDirection;
            if (intent.Following)
            {
                if (ownerVelocity.sqrMagnitude < 0.36f && offset.sqrMagnitude < 42.25f)
                {
                    // Brake in place rather than reversing toward a slot behind our feet.
                    resting = true;
                    repositioning = false;
                    return destination = root.transform.position;
                }

                if (!wasFollowing)
                {
                    Vector3 local = Quaternion.Inverse(formation) * offset;
                    float side = Mathf.Abs(local.x) < 0.5f ? (index == 1 ? -1f : 1f) : Mathf.Sign(local.x);
                    followOffset = new Vector3(side * Mathf.Clamp(Mathf.Abs(local.x), 1.4f, 3.3f), 0f,
                        -Mathf.Clamp(Mathf.Abs(local.z), 1.8f, 3.8f));
                }

                resting = false;
                repositioning = false;
                Vector3 goal = owner.transform.position + formation * followOffset + ownerVelocity * 0.3f;
                if (!Room(goal, true) && Time.time >= nextPeerTarget)
                {
                    nextPeerTarget = Time.time + 0.4f;
                    Vector3 alternate;
                    if (FindRoomNear(owner, goal, out alternate))
                    {
                        followOffset = Quaternion.Inverse(formation) *
                                       (alternate - owner.transform.position - ownerVelocity * 0.3f);
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
                nextRoam = Time.time + UnityEngine.Random.Range(HelOathGhostMotion.IdleMinSeconds,
                    HelOathGhostMotion.IdleMaxSeconds);
            }

            if (intent.Focused(Time.time))
            {
                if (Time.time >= nextAimCheck)
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
                nextRoam = Time.time + UnityEngine.Random.Range(HelOathGhostMotion.IdleMinSeconds,
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
            if (!departure.Retiring && (separation.sqrMagnitude >
                                        HelOathGhostMotion.RecallDistance * HelOathGhostMotion.RecallDistance
                                        || (blockedSeconds > 3f && separation.sqrMagnitude > 100f))) recalling = true;
            if (recalling)
            {
                opacity = Mathf.MoveTowards(opacity, 0f, dt / 0.25f);
                if (opacity <= 0f) FinishRecall(owner, formation);
                appearance.SetFade(BodyFade(opacity), Time.time, index, false, opacity);
                return true;
            }

            opacity = Mathf.MoveTowards(opacity, 1f, dt / FadeInSeconds);
            bool focused = intent.Focused(Time.time) || model.IsAiming;
            bool stop = departure.Retiring || resting || (focused && !intent.Following && !repositioning);
            Vector3 movement = Move(owner, target, stop, focused, dt);
            model.Animate(root.transform.InverseTransformDirection(movement), !departure.Retiring || !finalShotStarted,
                dt);
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
                if (fromOwner.sqrMagnitude > 64f || settledOffset.sqrMagnitude > 36f) continue;
                float ground;
                if (!TryGround(candidate, Position.y, out ground) || Mathf.Abs(ground - Position.y) > 2.5f) continue;
                candidate.y = ground;
                if (!Room(candidate, true) || !BodyClear(candidate) || !RouteClear(Position, candidate)) continue;
                result = candidate;
                return true;
            }

            return false;
        }

        private Vector3 Move(Player owner, Vector3 target, bool stop, bool aiming, float dt)
        {
            Vector3 old = root.transform.position;
            // Existing overlaps (e.g. a returning body) separate with real walking
            // animation. Departing bodies finish their release without being shoved.
            if (!departure.Retiring && !departure.Fading && companions != null)
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
            // Face the actual detour, not the blocked direct route. Otherwise a
            // detour beyond 90 degrees is permanently rejected by the speed gate.
            // At rest, preserve heading rather than returning to the old patrol leg.
            Vector3 face = aiming ? focusDirection : desiredDirection;
            face.y = 0f;
            if (face.sqrMagnitude > 0.001f)
                root.transform.rotation = Quaternion.RotateTowards(root.transform.rotation,
                    Quaternion.LookRotation(face), (aiming ? 180f : 120f) * dt);
            bool follow = intent.Following && !departure.Retiring;
            float wantedSpeed = stop
                ? 0f
                : follow && ownerVelocity.sqrMagnitude > 0.36f
                    ? HelOathGhostMotion.PursuitSpeed(distance,
                        distance > 0.01f ? Vector3.Dot(ownerVelocity, delta / distance) : 0f)
                    : HelOathGhostMotion.DesiredSpeed(distance,
                        follow ? HelOathGhostMotion.FollowSpeed : HelOathGhostMotion.WalkSpeed);
            if (!stop && distance > 0.001f)
            {
                if (desiredDirection.sqrMagnitude < 0.001f) wantedSpeed = 0f;
                else
                {
                    travelDirection = travelDirection.sqrMagnitude < 0.001f
                        ? desiredDirection
                        : Vector3.RotateTowards(travelDirection, desiredDirection, (follow ? 5f : 3f) * dt, 0f)
                            .normalized;
                    if (!aiming) wantedSpeed *= Mathf.Max(0f, Vector3.Dot(root.transform.forward, desiredDirection));
                }
            }

            speed = HelOathGhostMotion.NextSpeed(speed, wantedSpeed, dt, follow);
            Vector3 next = old + travelDirection * (speed * dt);
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
                UnityEngine.Object.Destroy(root);
            }

            appearance.Dispose();
        }
    }
}