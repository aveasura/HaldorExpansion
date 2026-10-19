using System;

namespace HaldorExpansion.Features.HelOath
{
    // Each render body owns its draw/release gate. Source snapshots alone cannot
    // authorize an arrow: they can skip a short draw or arrive during recovery.
    internal sealed class HelOathGhostBowCycle
    {
        internal const float MinimumDraw = 0.12f;
        internal const float MinimumRelease = 0.12f;
        internal int ReleasedCycle { get; private set; }
        private int shownCycle, shownFrames;
        private float shownSeconds, releaseUntil;
        private float releaseStarted = float.NegativeInfinity;

        internal bool Releasing(float now) => now < releaseUntil;

        internal bool Draw(int cycle, float now, float dt)
        {
            if (cycle <= ReleasedCycle || now < releaseStarted + MinimumRelease) return false;
            if (shownCycle != cycle)
            {
                shownCycle = cycle;
                shownSeconds = 0f;
                shownFrames = 0;
            }

            if (dt > 0f)
            {
                shownSeconds += Math.Min(dt, 0.1f);
                shownFrames++;
            }

            releaseUntil = Math.Min(releaseUntil, now);
            return true;
        }

        internal bool Ready(int cycle, float now) => cycle > ReleasedCycle && cycle == shownCycle
                                                                           && shownFrames >= 2 &&
                                                                           shownSeconds + 0.00001f >= MinimumDraw
                                                                           && now >= releaseStarted + MinimumRelease;

        internal bool Fire(int cycle, float now, float duration)
        {
            if (!Ready(cycle, now)) return false;
            ReleasedCycle = cycle;
            releaseStarted = now;
            releaseUntil = now + Math.Max(MinimumRelease, duration);
            shownCycle = shownFrames = 0;
            shownSeconds = 0f;
            return true;
        }

        internal void Cancel()
        {
            releaseUntil = 0f;
            releaseStarted = float.NegativeInfinity;
            shownCycle = shownFrames = 0;
            shownSeconds = 0f;
        }
    }
}