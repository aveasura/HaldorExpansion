using System.Runtime.CompilerServices;
using UnityEngine;

namespace HaldorExpansion.Combat
{
    // One owner for Animator.speed: Cestus, Wrath and Ragnarök Exhaustion must not
    // independently treat each other's output as the new animation base speed.
    internal static class AttackAnimationSpeedRuntime
    {
        private const float SpeedEpsilon = 0.0001f;

        private sealed class AnimatorState
        {
            internal float BaseSpeed;
            internal float AppliedSpeed;
        }

        // Keys are weak: an animator destroyed during an attack cannot leak a state
        // entry (unlike a static Dictionary<int, State> keyed by GetInstanceID()).
        private static readonly ConditionalWeakTable<Animator, AnimatorState> States =
            new ConditionalWeakTable<Animator, AnimatorState>();

        internal static void Apply(Animator animator, float multiplier)
        {
            AnimatorState state;
            bool wasApplied = States.TryGetValue(animator, out state);
            float observedSpeed = animator.speed;

            if (Mathf.Abs(multiplier - 1f) <= SpeedEpsilon)
            {
                if (wasApplied)
                {
                    // Restore only if no other system (Valheim or another mod) has
                    // overwritten our previous value since the last fixed update.
                    if (Mathf.Abs(observedSpeed - state.AppliedSpeed) <= SpeedEpsilon)
                        animator.speed = state.BaseSpeed;
                    States.Remove(animator);
                }
                return;
            }

            // The game may rewrite Animator.speed every tick. If it has not,
            // reuse the true base to avoid multiplying our own result repeatedly.
            float baseSpeed = wasApplied &&
                              Mathf.Abs(observedSpeed - state.AppliedSpeed) <= SpeedEpsilon
                ? state.BaseSpeed
                : observedSpeed;

            float targetSpeed = baseSpeed * multiplier;
            if (Mathf.Abs(observedSpeed - targetSpeed) > SpeedEpsilon)
                animator.speed = targetSpeed;

            if (!wasApplied)
            {
                state = new AnimatorState();
                States.Add(animator, state);
            }

            state.BaseSpeed = baseSpeed;
            state.AppliedSpeed = targetSpeed;
        }
    }
}
