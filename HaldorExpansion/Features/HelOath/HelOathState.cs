using System;

namespace HaldorExpansion.Features.HelOath
{
    // No Unity dependency: state transitions can be verified outside the game.
    internal sealed class HelOathState
    {
        internal const int MaxTouchStacks = 5;
        internal const float TouchInactiveGrace = 5f;
        internal const float TouchDecayInterval = 3f;

        internal float Charge { get; private set; }
        internal bool Casting { get; private set; }
        internal bool Prepared { get; private set; }
        internal bool PreparedWithTouchV { get; private set; }
        internal int TouchStacks { get; private set; }
        internal bool TouchAwakened { get; private set; }
        internal string Epoch { get; private set; } = Guid.NewGuid().ToString("N");

        private object weapon;
        private float readyAt;
        private float blockedUntil;
        private float nextTouchAt;
        private bool touchTimerArmed;
        private float nextTouchDecayAt;
        private bool touchDecayArmed;

        internal void Equip(object current, float restoredCharge = 0f)
        {
            if (ReferenceEquals(weapon, current)) return;
            weapon = current;
            Charge = current == null ? 0f : NormalizeCharge(restoredCharge);
            Casting = Prepared = PreparedWithTouchV = false;
            Epoch = Guid.NewGuid().ToString("N");
            // Keep cooldown and Hel's Touch when sheathing/re-equipping. Charge itself is restored from the bow instance.
            // Touch is player state: while the bow/full Fenris are unavailable it becomes inactive and decays in UpdateTouch.
        }

        internal void AddDamage(float damage, float requiredDamage, float now, string epoch)
        {
            if (weapon == null || epoch != Epoch || Casting || Prepared || now < blockedUntil || Charge >= 100f)
                return;
            if (float.IsNaN(damage) || float.IsInfinity(damage) || damage <= 0f || float.IsNaN(requiredDamage) || float.IsInfinity(requiredDamage) || requiredDamage <= 0f)
                return;
            Charge = Math.Min(100f, Charge + damage * 100f / requiredDamage);
        }

        // Hel's Touch is awakened by the first real Hel's Oath damage while the full Fenris set is worn.
        // It is intentionally independent from Embrace charge: charge may still be 1%, 50% or 100%.
        internal void RegisterBowDamage(float now, bool fullFenrisSet, float interval)
        {
            interval = NormalizeTouchInterval(interval);
            if (weapon == null || !fullFenrisSet || Casting || Prepared || TouchAwakened) return;
            TouchAwakened = true;
            touchTimerArmed = true;
            nextTouchAt = now + interval;
        }

        internal void UpdateTouch(float now, bool fullFenrisSet, float interval)
        {
            interval = NormalizeTouchInterval(interval);
            if (weapon == null || !fullFenrisSet)
            {
                UpdateInactiveTouch(now);
                return;
            }

            // Returning to Hel's Oath + full Fenris resumes the saved stage, but never grants
            // catch-up stacks for the time spent in another loadout.
            if (touchDecayArmed)
            {
                touchDecayArmed = false;
                nextTouchDecayAt = 0f;
                touchTimerArmed = false;
                nextTouchAt = 0f;
            }

            if (!TouchAwakened || Casting || Prepared) return;

            if (!touchTimerArmed)
            {
                touchTimerArmed = true;
                nextTouchAt = now + interval;
                return;
            }

            while (TouchStacks < MaxTouchStacks && now >= nextTouchAt)
            {
                TouchStacks++;
                nextTouchAt += interval;
            }
        }

        internal void RegisterCombatHit(float now, bool fullFenrisSet, float interval)
        {
            interval = NormalizeTouchInterval(interval);
            bool eligible = fullFenrisSet && weapon != null && TouchAwakened && !Casting && !Prepared;
            if (!eligible) return; // Frozen/inactive Touch is already governed by loadout decay.

            if (TouchStacks > 0) TouchStacks--;
            touchTimerArmed = true;
            nextTouchAt = now + interval;
        }

        internal bool Activate(float now, float duration, bool touchActive)
        {
            if (weapon == null || Charge < 100f || Casting || Prepared || now < blockedUntil) return false;
            PreparedWithTouchV = touchActive && TouchStacks >= MaxTouchStacks;
            Charge = 0f;
            Casting = true;
            readyAt = now + Math.Max(0f, duration);
            ClearTouch();
            Epoch = Guid.NewGuid().ToString("N");
            return true;
        }

        internal void Tick(float now)
        {
            if (!Casting || now < readyAt) return;
            Casting = false;
            Prepared = true;
        }

        internal bool TryFire(float now, out bool touchV)
        {
            touchV = false;
            if (!Prepared || weapon == null) return false;
            touchV = PreparedWithTouchV;
            Prepared = false;
            PreparedWithTouchV = false;
            blockedUntil = now + 3f;
            Epoch = Guid.NewGuid().ToString("N");
            return true;
        }

        internal bool TryFire(float now)
        {
            bool ignored;
            return TryFire(now, out ignored);
        }


        internal void ResetTouch() => ClearTouch();

        private void UpdateInactiveTouch(float now)
        {
            touchTimerArmed = false;
            nextTouchAt = 0f;
            if (!TouchAwakened)
            {
                touchDecayArmed = false;
                nextTouchDecayAt = 0f;
                return;
            }

            if (!touchDecayArmed)
            {
                touchDecayArmed = true;
                nextTouchDecayAt = now + TouchInactiveGrace;
                return;
            }

            // The first stage is lost when the grace window ends; further stages fade one by one.
            while (TouchStacks > 0 && now >= nextTouchDecayAt)
            {
                TouchStacks--;
                if (TouchStacks <= 0)
                {
                    ClearTouch();
                    return;
                }
                nextTouchDecayAt += TouchDecayInterval;
            }

            // An awakened zero-stack cycle is also forgotten once its grace expires.
            if (TouchStacks <= 0 && now >= nextTouchDecayAt) ClearTouch();
        }

        private static float NormalizeTouchInterval(float interval)
        {
            if (float.IsNaN(interval) || float.IsInfinity(interval) || interval <= 0f) return 3f;
            return interval;
        }

        private static float NormalizeCharge(float charge)
        {
            if (float.IsNaN(charge) || float.IsInfinity(charge)) return 0f;
            return Math.Max(0f, Math.Min(100f, charge));
        }

        private void ClearTouch()
        {
            TouchStacks = 0;
            TouchAwakened = false;
            touchTimerArmed = false;
            nextTouchAt = 0f;
            touchDecayArmed = false;
            nextTouchDecayAt = 0f;
        }
    }
}
