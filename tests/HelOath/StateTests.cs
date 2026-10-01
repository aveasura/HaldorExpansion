using System;
using System.Reflection;
// Behavioral regression tests. The state class is loaded from the actual source build.
class StateTests
{
    static int Main(string[] args)
    {
        try
        {
            var s = new HaldorExpansion.Features.HelOath.HelOathState();
            s.Equip("bow-a");
            string epoch = s.Epoch;
            s.AddDamage(400f, 1000f, 0f, epoch);
            Check(s.Charge == 40f, "Actual damage contributes proportionally");
            s.AddDamage(900f, 1000f, 0f, epoch);
            Check(s.Charge == 100f, "Charge caps at full");
            Check(s.Activate(0f, 1f, true), "Full charge starts activation");
            s.AddDamage(500f, 1000f, 0f, s.Epoch);
            Check(s.Charge == 0f, "No accumulation during activation");
            Check(!s.TryFire(0.9f), "Casting cannot produce a special shot");
            s.Tick(1f);
            s.AddDamage(500f, 1000f, 1f, s.Epoch);
            Check(s.Charge == 0f, "Prepared shot blocks accumulation");
            Check(s.TryFire(2f), "Prepared shot is consumed");
            Check(!s.TryFire(2f), "Cannot fire special shot twice");
            s.AddDamage(1000f, 1000f, 4.99f, s.Epoch);
            Check(s.Charge == 0f, "Three second lockout survives flight time");
            s.AddDamage(500f, 1000f, 5f, epoch);
            Check(s.Charge == 0f, "Delayed credits from old shots are rejected");
            s.AddDamage(500f, 1000f, 5f, s.Epoch);
            Check(s.Charge == 50f, "Accumulation resumes after cooldown");
            float storedBowACharge = s.Charge;
            s.Equip("bow-b", 0f);
            Check(s.Charge == 0f && !s.Prepared, "A different bow starts from its own stored charge");
            s.Equip(null);
            Check(s.Charge == 0f, "Unequipped runtime state is inert");
            s.Equip("bow-a", storedBowACharge);
            Check(s.Charge == 50f, "Re-equipping a bow restores that bow's persisted charge");
            s.Equip("bow-b", 0f);
            s.AddDamage(float.NaN, 1000f, 9f, s.Epoch);
            Check(s.Charge == 0f, "Malformed credits cannot poison charge");
            s.AddDamage(50f, float.NaN, 9f, s.Epoch);
            Check(s.Charge == 0f, "Invalid configured threshold cannot poison charge");
            s.AddDamage(1000f, 1000f, 9f, s.Epoch);
            s.Activate(9f, 1f, true);
            s.Equip(null);
            s.Tick(20f);
            Check(!s.Prepared && s.Charge == 0f, "Unequip cancels casting and prepared shot");
            s.Equip("bow-a");
            s.AddDamage(1000f,1000f,20f,s.Epoch);
            Check(s.Activate(20f,0f,true), "Instant activation accepts full charge");
            s.Tick(20f);
            Check(s.Prepared && !s.Casting, "Instant activation has no animation lock");
            Check(s.TryFire(20f), "Instant prepared shot fires immediately");
            s.Equip(null); s.Equip("bow-a");
            s.AddDamage(1000f,1000f,21f,s.Epoch);
            Check(s.Charge == 0f, "Weapon switch cannot bypass special-shot cooldown");

            // Fresh state for Hel's Touch timing and Touch V snapshot behavior.
            // Touch now awakens from the first real bow damage; it does not wait for full charge.
            s = new HaldorExpansion.Features.HelOath.HelOathState();
            s.Equip("bow-touch");
            s.AddDamage(100f, 1000f, 30f, s.Epoch);
            Check(s.Charge == 10f, "Touch test starts far below full Embrace charge");
            s.UpdateTouch(40f, true, 3f);
            Check(s.TouchStacks == 0, "Touch cannot start before Hel's Oath actually damages an enemy");
            s.RegisterBowDamage(40f, true, 3f);
            Check(s.TouchAwakened && s.TouchStacks == 0, "First bow damage awakens Touch without granting an instant stack");
            s.UpdateTouch(42.99f, true, 3f);
            Check(s.TouchStacks == 0, "Touch does not arrive before the configured interval");
            s.UpdateTouch(43f, true, 3f);
            Check(s.TouchStacks == 1, "Touch I arrives three hitless seconds after bow damage");
            s.UpdateTouch(55f, true, 3f);
            Check(s.TouchStacks == 5, "Touch catches up to V and caps at five stacks independently of Embrace charge");

            float chargeBeforeReset = s.Charge;
            s.RegisterCombatHit(55.5f, true, 3f);
            Check(s.TouchStacks == 4 && s.Charge == chargeBeforeReset, "A combat hit removes one Touch stack and preserves Embrace charge");
            s.UpdateTouch(58.49f, true, 3f);
            Check(s.TouchStacks == 4, "Combat hit restarts the no-hit timer");
            s.UpdateTouch(58.5f, true, 3f);
            Check(s.TouchStacks == 5, "Touch resumes exactly after the restarted timer");
            s.RegisterCombatHit(58.6f, true, 3f);
            s.RegisterCombatHit(58.7f, true, 3f);
            s.RegisterCombatHit(58.8f, true, 3f);
            s.RegisterCombatHit(58.9f, true, 3f);
            s.RegisterCombatHit(59.0f, true, 3f);
            Check(s.TouchStacks == 0 && s.TouchAwakened, "Repeated combat hits can strip stacks while the Touch cycle remains awakened");
            s.UpdateTouch(74f, true, 3f);
            Check(s.TouchStacks == 5, "Touch can rebuild to V after repeated damage without another outgoing hit");

            // Fill Embrace only after Touch has already existed for a while.
            s.AddDamage(900f, 1000f, 74f, s.Epoch);
            Check(s.Charge == 100f, "Touch building does not interfere with Embrace charge");
            Check(s.Activate(74f, 0f, true), "Touch V can activate Embrace");
            Check(s.TouchStacks == 0 && !s.TouchAwakened && s.PreparedWithTouchV, "Activation consumes Touch and its awakened cycle while snapshotting Touch V");
            s.Tick(74f);
            bool touchV;
            Check(s.TryFire(74f, out touchV) && touchV, "Prepared arrow remembers Touch V after stacks disappear");
            s.RegisterBowDamage(74.1f, true, 3f);
            Check(s.TouchAwakened, "Real damage from the fired special shot can awaken a fresh Touch cycle without recharging Embrace");

            s = new HaldorExpansion.Features.HelOath.HelOathState();
            s.Equip("bow-touch-loss");
            s.RegisterBowDamage(80f, true, 3f);
            s.UpdateTouch(86f, true, 3f);
            Check(s.TouchStacks == 2, "Touch builds while full Fenris is present");

            // Loadout swap: Touch becomes inactive externally, but its player-owned stage is retained briefly.
            s.UpdateTouch(86.1f, false, 3f);
            Check(s.TouchStacks == 2 && s.TouchAwakened, "Losing full Fenris starts grace instead of deleting Touch");
            s.RegisterCombatHit(88f, false, 3f);
            Check(s.TouchStacks == 2, "Frozen Touch is not additionally punished by combat hits");
            s.UpdateTouch(91.09f, false, 3f);
            Check(s.TouchStacks == 2, "All Touch stacks survive the five-second grace window");
            s.UpdateTouch(91.1f, false, 3f);
            Check(s.TouchStacks == 1 && s.TouchAwakened, "First Touch stack fades when grace ends");

            // Returning before complete decay restores the saved stage but does not grant offline catch-up.
            s.UpdateTouch(92f, true, 3f);
            Check(s.TouchStacks == 1 && s.TouchAwakened, "Re-equipping the required loadout restores the retained Touch stage");
            s.UpdateTouch(94.99f, true, 3f);
            Check(s.TouchStacks == 1, "Touch growth timer restarts after returning to the loadout");
            s.UpdateTouch(95f, true, 3f);
            Check(s.TouchStacks == 2, "Touch resumes normal growth after the restarted timer");

            // Unequipping the bow uses the same grace/decay path because Touch belongs to the player, not the item.
            s.Equip(null);
            s.UpdateTouch(95.1f, false, 3f);
            s.UpdateTouch(100.1f, false, 3f);
            Check(s.TouchStacks == 1 && s.TouchAwakened, "Unequipped bow loses one retained Touch stack after grace");
            s.UpdateTouch(103.1f, false, 3f);
            Check(s.TouchStacks == 0 && !s.TouchAwakened, "Touch fully expires after the final retained stack decays");
            s.Equip("bow-touch-loss");
            s.UpdateTouch(110f, true, 3f);
            Check(s.TouchStacks == 0 && !s.TouchAwakened, "Fully expired Touch requires new bow damage to awaken again");

            // A retained raw Touch V must never empower Embrace while the Fenris requirement is inactive.
            s.RegisterBowDamage(110f, true, 3f);
            s.UpdateTouch(125f, true, 3f);
            Check(s.TouchStacks == 5, "Touch can rebuild to V after a fresh damage event");
            s.AddDamage(1000f, 1000f, 125f, s.Epoch);
            s.UpdateTouch(125.1f, false, 3f);
            Check(s.Activate(125.1f, 0f, false), "Embrace remains usable after the Fenris requirement is removed");
            Check(!s.PreparedWithTouchV, "Inactive retained Touch V cannot empower Embrace");

            Console.WriteLine("PASS: charge, cast, prepared shot, cooldown, stale credit, per-bow restore, Touch timing/hit-decrement/V snapshot, grace/decay/loadout gating and invalid input scenarios");
            DamageMathTests.Run();
            DotTests.Run();
            return 0;
        }
        catch(Exception e) { Console.Error.WriteLine("FAIL: " + e.Message); return 1; }
    }
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
}
