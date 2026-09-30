using System;

namespace HaldorExpansion.Features.HelOath
{
    // No Unity dependency: state transitions can be verified outside the game.
    internal sealed class HelOathState
    {
        internal const int MaxTouchStacks = 5;

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

        internal void Equip(object current)
        {
            if (ReferenceEquals(weapon, current)) return;
            weapon = current;
            Charge = 0f;
            Casting = Prepared = PreparedWithTouchV = false;
            ClearTouch();
            Epoch = Guid.NewGuid().ToString("N");
            // Keep cooldown when sheathing/re-equipping.
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
                ClearTouch();
                return;
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
            if (TouchStacks > 0) TouchStacks--;

            bool eligible = fullFenrisSet && weapon != null && TouchAwakened && !Casting && !Prepared;
            touchTimerArmed = eligible;
            if (eligible) nextTouchAt = now + interval;
            else if (!fullFenrisSet || weapon == null) ClearTouch();
            else nextTouchAt = 0f;
        }

        internal bool Activate(float now, float duration)
        {
            if (weapon == null || Charge < 100f || Casting || Prepared || now < blockedUntil) return false;
            PreparedWithTouchV = TouchStacks >= MaxTouchStacks;
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

        private static float NormalizeTouchInterval(float interval)
        {
            if (float.IsNaN(interval) || float.IsInfinity(interval) || interval <= 0f) return 3f;
            return interval;
        }

        private void ClearTouch()
        {
            TouchStacks = 0;
            TouchAwakened = false;
            touchTimerArmed = false;
            nextTouchAt = 0f;
        }
    }
}
