using System.Collections.Generic;

namespace HaldorExpansion.Features.Cestus
{
    internal static class CestusAnimationRuntime
    {
        internal sealed class CestusAnimatorSpeedState
        {
            public bool Applied;
            public float LastBaseSpeed = 1f;
            public float LastAppliedSpeed = 1f;
        }

        internal static readonly Dictionary<int, CestusAnimatorSpeedState> CestusAnimatorStates =
            new Dictionary<int, CestusAnimatorSpeedState>();
    }
}
