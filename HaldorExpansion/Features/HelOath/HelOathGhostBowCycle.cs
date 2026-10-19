using System;

namespace HaldorExpansion.Features.HelOath
{
    // One independent draw/release timeline per render body. Owner Animator snapshots
    // are visual hints, never a prerequisite for releasing a queued arrow.
    internal sealed class HelOathGhostBowCycle
    {
        internal const float MinimumDraw = 0.30f;
        internal const float MinimumRelease = 0.12f;
        internal float RequiredDraw { get; set; } = MinimumDraw;
        internal int ReleasedCycle { get; private set; }
        private int drawingCycle, drawnFrames;
        private float drawnSeconds, releaseUntil;

        internal bool Releasing(float now) => now < releaseUntil;
        internal float DrawProgress => Math.Min(1f, drawnSeconds / Math.Max(MinimumDraw, RequiredDraw));

        internal bool WantsDraw(int cycle, float now) => cycle > ReleasedCycle && !Releasing(now);

        // Called once after this body's Animator has actually advanced a draw frame.
        // The draw timer is independent of which state name/clip Unity reports.
        internal bool Draw(int cycle, float now, float dt)
        {
            if (!WantsDraw(cycle, now)) return false;
            if (drawingCycle != cycle)
            {
                drawingCycle = cycle;
                drawnFrames = 0;
                drawnSeconds = 0f;
            }

            if (dt > 0f)
            {
                drawnFrames++;
                drawnSeconds += Math.Min(dt, 0.1f);
            }

            return true;
        }

        internal bool Ready(int cycle, float now) => drawingCycle == cycle && WantsDraw(cycle, now)
                                                                           && drawnFrames >= 2 &&
                                                                           drawnSeconds + 0.00001f >=
                                                                           Math.Max(MinimumDraw, RequiredDraw);

        internal bool Fire(int cycle, float now, float duration)
        {
            if (!Ready(cycle, now)) return false;
            ReleasedCycle = cycle;
            releaseUntil = now + Math.Max(MinimumRelease, duration);
            drawingCycle = drawnFrames = 0;
            drawnSeconds = 0f;
            return true;
        }

        internal void CancelDraw(int cycle)
        {
            if (drawingCycle != cycle) return;
            drawingCycle = drawnFrames = 0;
            drawnSeconds = 0f;
        }

        // Only removal/ultimate can interrupt the current action. Normal owner
        // draws never call this, even if the next shot is already queued.
        internal void Cancel()
        {
            releaseUntil = 0f;
            drawingCycle = drawnFrames = 0;
            drawnSeconds = 0f;
        }
    }
}