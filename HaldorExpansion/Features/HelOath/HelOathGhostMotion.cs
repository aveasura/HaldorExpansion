using System;

namespace HaldorExpansion.Features.HelOath
{
    // Presentation tuning only; no soul, damage or Tribute rules.
    internal static class HelOathGhostMotion
    {
        internal const float PatrolMinRadius = 3f;
        internal const float PatrolMaxRadius = 7f;
        internal const float MinPatrolLeg = 3.5f;
        internal const float WalkSpeed = 1.8f;
        internal const float FollowSpeed = 14f;
        internal const float FollowAcceleration = 8f;
        internal const float FollowDeceleration = 8f;
        internal const float CombatHoldSeconds = 2.2f;
        internal const float RecallDistance = 24f;
        internal const float Acceleration = 2.8f;
        internal const float Deceleration = 3.5f;
        internal const float StopSeconds = 0.8f;
        internal const float IdleMinSeconds = 1.5f;
        internal const float IdleMaxSeconds = 3.5f;
        internal const float GroundClearance = 0.10f;
        internal const float FootprintRadius = 0.28f;

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
            float feed = Math.Max(0f, ownerAlongPath) * Math.Min(1f, Math.Max(0f, distance) / 0.6f);
            return Math.Min(FollowSpeed, feed + DesiredSpeed(distance, FollowSpeed));
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
        internal const float Minimum = 1.5f;
        internal const float Preferred = 1.85f;
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
            if (aiming) Focus(now);
            if (ownerSpeed > 0.6f || ownerDistance > 8f)
            {
                Following = true;
                followUntil = now + 0.8f;
            }
            else if (now >= followUntil && ownerDistance < 6.5f) Following = false;
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

    // Slot numbers are reused; a delayed RPC must identify the actual soul.
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