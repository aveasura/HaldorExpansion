using System;

namespace HaldorExpansion.Features.HelOath
{
    // Movement and positioning tuning only; never changes souls, damage or firing rules.
    internal static class HelOathGhostMotion
    {
        // ===== FORMATION DISTANCES: EDIT THESE TO TUNE GHOST SPACING =====
        // First apparition: Guardian materializes NEAR the player, then RUNS to his orbit.
        // Keep this beyond the ghost-to-ghost minimum spacing (HelOathGhostSpacing.Minimum).
        internal const float GuardianSpawnDistance = 4f;

        // Fallback when the preferred apparition spot is obstructed.
        internal const float SpawnFallbackMinRadius = 3.2f;
        internal const float SpawnFallbackMaxRadius = 4.8f;

        // Normal ghosts (slots 1 and 2) FOLLOW the owner's moving formation.
        // These are local X/Z offsets; the Guardian NEVER uses them.
        internal const float EscortSide = 4.5f;

        internal const float EscortRear = 2.1f;

        // Slow roaming when the owner is stationary (measured from owner).
        internal const float PatrolMinRadius = 3.8f;

        internal const float PatrolMaxRadius = 8.0f;

        // Within this band an escort may stop correcting its exact slot.
        internal const float EscortHoldMinRadius = 3.6f;

        internal const float EscortHoldMaxRadius = 5.9f;

        // Follow immediately on owner movement, or when the escort drifts out.
        internal const float EscortMoveThreshold = 0.55f;
        internal const float EscortRejoinDistance = 6.7f;

        internal const float EscortFollowReleaseSeconds = 0.65f;

        // Small velocity lead avoids forever chasing the owner's OLD position.
        internal const float EscortLeadSeconds = 0.16f;

        internal const float EscortMaxLeadSpeed = 12f;

        // Only escorts use the shorter catch-up and recall distances.
        internal const float EscortCatchUpDistance = 8f;
        internal const float EscortCatchUpMaxSpeed = 16f;
        internal const float EscortRecallDistance = 14f;
        internal const float EscortBlockedRecallSeconds = 1.5f;
        internal const float EscortBlockedRecallDistance = 7.5f;

        // Guardian (always slot 0): 360-degree orbit, not necessarily behind owner.
        // This is the DISTANT destination after materialization, not the spawn spot.
        internal const int GuardianIndex = 0;
        internal const float GuardianTravelNear = 12f;
        internal const float GuardianTravelFar = 16f;
        internal const float GuardianCombatNear = 18f;

        internal const float GuardianCombatFar = 25f;

        // Emergency catch-up fade/recall; keep above GuardianCombatFar + tolerance.
        internal const float RecallDistance = 33f;

        // Multi-floor interiors: horizontal distance alone misses a soul standing
        // directly above or below its owner. Require a stable owner-side floor
        // before recalling, so jumping/falling cannot cause unwanted recalls.
        internal const float VerticalFloorGap = 2.8f;
        internal const float VerticalRecallDelay = 0.8f;
        internal const float LevelGroundTolerance = 1.35f;

        internal const float GuardianLevelTolerance = 2.1f;

        // ===== END OF FORMATION DISTANCES =====
        internal const float MinPatrolLeg = 3.5f;
        internal const float WalkSpeed = 1.8f;
        internal const float FollowSpeed = 14f;
        internal const float FollowAcceleration = 8f;
        internal const float FollowDeceleration = 8f;
        internal const float CombatHoldSeconds = 2.2f;
        internal const float Acceleration = 2.8f;
        internal const float Deceleration = 3.5f;
        internal const float StopSeconds = 0.8f;
        internal const float IdleMinSeconds = 1.5f;
        internal const float IdleMaxSeconds = 3.5f;
        internal const float GroundClearance = 0.10f;

        internal const float FootprintRadius = 0.28f;

        // While moving, the rendered body faces its path, not the bow target.
        // Turning is fast enough for sprint follow, but acceleration is reduced
        // until the feet are actually pointing along the chosen path.
        internal const float PatrolTurnDegrees = 240f;
        internal const float FollowTurnDegrees = 420f;

        internal const float AimTurnDegrees = 180f;

        // A chasing ghost only briefly brakes for the last part of its draw
        // and the arrow's release. Keep the rest of its FIFO cycle mobile.
        internal const float ShotTurnDegrees = 1080f;
        internal const float ShotFacingDrawFraction = 0.30f;
        internal const float ShotFacingAfterFire = 0.18f;
        internal const float TurnBrakeFollow = 34f;
        internal const float TurnBrakeWalk = 8f;
        internal const float StopBrakeFollow = 40f;
        internal const float StopBrakeWalk = 10f;
        internal const float StopBeforeAimingSpeed = 0.2f;

        // Alignment is the horizontal dot product of facing and route (-1..1).
        // In a sharp turn a follower must slow instead of running sideways;
        // once aligned it regains the original full chase speed.
        internal static float FacingSpeedFactor(float alignment)
        {
            return Math.Max(0f, Math.Min(1f, (alignment - 0.15f) / 0.85f));
        }

        internal static float FacingSpeed(float speed, float wantedSpeed, float alignment,
            float dt, bool following, bool stop)
        {
            dt = Math.Max(0f, dt);
            float target = stop ? 0f : Math.Max(0f, wantedSpeed) * FacingSpeedFactor(alignment);
            float acceleration = following ? FollowAcceleration : Acceleration;
            float deceleration = stop ? (following ? StopBrakeFollow : StopBrakeWalk)
                : alignment < 0.8f ? (following ? TurnBrakeFollow : TurnBrakeWalk)
                : following ? FollowDeceleration : Deceleration;
            return speed < target
                ? Math.Min(target, speed + acceleration * dt)
                : Math.Max(target, speed - deceleration * dt);
        }

        internal static float DesiredSpeed(float distance, float maximum)
        {
            distance = Math.Max(0f, distance);
            // At follow speed, distance / StopSeconds alone starts braking too
            // late for our bounded deceleration. Respect stopping distance too.
            float margin = Deceleration * StopSeconds;
            float braking = (float)Math.Sqrt(margin * margin + 2f * Deceleration * distance) - margin;
            return Math.Min(maximum, braking);
        }

        internal static float PursuitSpeed(float distance, float ownerAlongPath)
        {
            return PursuitSpeed(distance, ownerAlongPath, FollowSpeed);
        }

        internal static float PursuitSpeed(float distance, float ownerAlongPath, float maximum)
        {
            float feed = Math.Max(0f, ownerAlongPath) * Math.Min(1f, Math.Max(0f, distance) / 0.6f);
            return Math.Min(maximum, feed + DesiredSpeed(distance, maximum));
        }

        internal static float NextSpeed(float speed, float target, float dt, bool following = false)
        {
            float step =
                (target < speed ? (following ? FollowDeceleration : Deceleration) :
                    following ? FollowAcceleration : Acceleration) * Math.Max(0f, dt);
            return speed < target ? Math.Min(target, speed + step) : Math.Max(target, speed - step);
        }

        internal static float GroundY(float current, float support, float dt)
        {
            // Rising support is a hard lower bound. Only descending height is eased.
            return Math.Max(support, current + (support - current) * (1f - (float)Math.Exp(-10f * Math.Max(0f, dt))));
        }
    }

    internal static class HelOathGhostSpacing
    {
        internal const float Minimum = 2.5f;
        internal const float Preferred = 3f;
        internal const float HeightTolerance = 1.5f;

        // Swept discs, not endpoint-only checks: a fast step cannot pass through
        // a peer and finish on the far side. Existing penetration may only improve.
        internal static bool CanMove(float x, float z, float nextX, float nextZ, float peerX, float peerZ)
        {
            float rx = x - peerX, rz = z - peerZ;
            float dx = nextX - x, dz = nextZ - z;
            float start = rx * rx + rz * rz;
            float length = dx * dx + dz * dz;
            if (length < 0.00000001f) return true;
            float dot = rx * dx + rz * dz;
            if (start < Minimum * Minimum - 0.00001f)
                return dot >= 0f && (rx + dx) * (rx + dx) + (rz + dz) * (rz + dz) > start;
            float t = Math.Max(0f, Math.Min(1f, -dot / length));
            float cx = rx + dx * t, cz = rz + dz * t;
            return cx * cx + cz * cz >= Minimum * Minimum - 0.00001f;
        }
    }

    internal sealed class HelOathGhostIntent
    {
        private float focusUntil, followUntil;
        internal bool Following { get; private set; }
        internal bool Focused(float now) => now < focusUntil;
        internal void Focus(float now) => focusUntil = Math.Max(focusUntil, now + HelOathGhostMotion.CombatHoldSeconds);

        internal void Update(float now, float ownerSpeed, float ownerDistance, bool aiming)
        {
            // Preserve the Guardian's existing movement/aiming intent.
            if (aiming) Focus(now);
            if (ownerSpeed > 0.6f || ownerDistance > 8f || ownerDistance < 3.2f)
            {
                Following = true;
                followUntil = now + 0.8f;
            }
            else if (now >= followUntil && ownerDistance < 7.5f) Following = false;
        }

        internal void UpdateEscort(float now, float ownerSpeed, float ownerDistance, bool aiming)
        {
            if (aiming) Focus(now);
            if (ownerSpeed > HelOathGhostMotion.EscortMoveThreshold
                || ownerDistance > HelOathGhostMotion.EscortRejoinDistance)
            {
                Following = true;
                followUntil = now + HelOathGhostMotion.EscortFollowReleaseSeconds;
            }
            else if (now >= followUntil
                     && ownerDistance <= HelOathGhostMotion.EscortHoldMaxRadius)
                Following = false;
        }
    }

    internal sealed class HelOathGhostDeparture
    {
        internal bool Retiring { get; private set; }
        internal bool Fading { get; private set; }
        internal bool CanFire => !Fading;
        private float holdUntil;

        internal void Retire(float now = 0f, float grace = 0f)
        {
            if (!Retiring && !Fading) holdUntil = Math.Max(holdUntil, now + grace);
            Retiring = true;
        }

        internal void HoldRelease(float until)
        {
            if (!Fading) holdUntil = Math.Max(holdUntil, until);
        }

        internal bool Waiting(float now) => !Fading && now < holdUntil;

        internal void BeginFade(bool spent)
        {
            Retiring |= spent;
            Fading = true;
        }

        internal void Reappear()
        {
            if (!Retiring) Fading = false;
        }
    }

    // Slot numbers are reused. A delayed RPC must identify the actual soul.
    internal sealed class HelOathGhostRoster
    {
        private readonly string[] tokens;

        internal HelOathGhostRoster(int limit)
        {
            tokens = new string[limit];
        }

        internal string Token(int index) => tokens[index] ?? "";

        internal void Update(int count)
        {
            for (int i = 0; i < tokens.Length; i++)
                if (i >= count) tokens[i] = null;
                else if (tokens[i] == null) tokens[i] = Guid.NewGuid().ToString("N");
        }
    }
}