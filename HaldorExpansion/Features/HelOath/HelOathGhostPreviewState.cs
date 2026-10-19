namespace HaldorExpansion.Features.HelOath
{
    // Temporary visual-test rules. Keep separate from the eventual +10 awakening.
    internal sealed class HelOathGhostPreviewState
    {
        internal const int Limit = 3;
        internal const float InactivitySeconds = 180f;
        internal int Count { get; private set; }
        private float lastHit;

        internal void Hit(float now, bool eligible, float damage)
        {
            if (!eligible || !Finite(now) || !Finite(damage) || damage <= 0f) return;
            Update(now, true);
            if (Count < Limit) Count++;
            lastHit = now;
        }

        internal void Update(float now, bool eligible)
        {
            if (!eligible || !Finite(now) || now < lastHit || now - lastHit >= InactivitySeconds) Reset();
        }

        internal void Reset()
        {
            Count = 0;
            lastHit = 0f;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}