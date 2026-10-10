using System;
using System.Reflection;
using HaldorExpansion.Features.HelOath;
using HaldorExpansion.Features.PeltOfHelheim;
using HaldorExpansion.Features.DelayedDoom;
using UnityEngine;

internal static class StatusLifecycleTests
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static Player Reset()
    {
        foreach (Type type in new[] { typeof(HelOathTouchStatus), typeof(PeltOfHelheimStatus), typeof(DelayedDoomDebtStatus) })
            foreach (FieldInfo field in type.GetFields(BindingFlags.Static | BindingFlags.NonPublic))
                if (!field.IsLiteral && !field.IsInitOnly)
                    field.SetValue(null, field.FieldType.IsValueType ? Activator.CreateInstance(field.FieldType) : null);
        ObjectDB.instance = new ObjectDB();
        HelOathItemRegistration.HelOathItemDrop = null;
        PeltOfHelheimItemRegistration.CapeItemDrop = null;
        DelayedDoomItemRegistration.DelayedDoomChestItemDrop = null;
        return Player.m_localPlayer = new Player();
    }

    private static ItemDrop WithIcon(Sprite icon)
    {
        var drop = new ItemDrop();
        drop.m_itemData.m_shared.m_icons = new[] { icon };
        return drop;
    }

    private static void HelLateIcon()
    {
        Player p = Reset();
        HelOathTouchStatus.Sync(p, 5);
        var active = p.m_seman.Effects[0];
        var icon = new Sprite();
        HelOathItemRegistration.HelOathItemDrop = WithIcon(icon);
        HelOathTouchStatus.Sync(p, 5);
        Check(ReferenceEquals(active.m_icon, icon), "active icon not backfilled");
        Check(ReferenceEquals(ObjectDB.instance.m_StatusEffects[0].m_icon, icon), "prototype icon not backfilled");
        Check(p.m_seman.Adds == 1, "same-stage reconciliation reapplied the effect");
    }

    private static void HelExternalRemoval()
    {
        Player p = Reset();
        HelOathTouchStatus.Sync(p, 4);
        var stale = p.m_seman.Effects[0];
        p.m_seman.RemoveStatusEffect(stale);
        HelOathTouchStatus.Sync(p, 4);
        Check(p.m_seman.Effects.Count == 1 && !ReferenceEquals(stale, p.m_seman.Effects[0]), "stale active reference blocked recovery");
    }

    private static void HelOwnerSwap()
    {
        Player old = Reset();
        HelOathTouchStatus.Sync(old, 3);
        Player next = Player.m_localPlayer = new Player();
        var own = new SE_Stats { name = HelOathIds.TouchStatus };
        next.m_seman.Effects.Add(own);
        HelOathTouchStatus.Sync(next, 0);
        Check(next.m_seman.Removals == 0 && next.m_seman.Effects.Contains(own), "old owner cache removed new owner's effect");
        HelOathTouchStatus.Sync(next, 3);
        Check(next.m_seman.Effects.Count == 1 && next.m_seman.Effects[0].m_name == HelOathIds.TouchNameKey(3), "new owner not synchronized");
        Check(old.m_seman.Effects.Count == 1, "new owner sync changed old owner's manager");
    }

    private static void HelFailedAddRetry()
    {
        Player p = Reset();
        p.m_seman.FailAdd = true;
        HelOathTouchStatus.Sync(p, 5);
        Check(p.m_seman.Effects.Count == 0, "failure setup did not fail");
        p.m_seman.FailAdd = false;
        HelOathTouchStatus.Sync(p, 5);
        Check(p.m_seman.Effects.Count == 1, "failed addition not retried");
    }

    private static void HelRegistrationRetry()
    {
        Player p = Reset();
        ObjectDB.instance = null;
        HelOathTouchStatus.Sync(p, 5);
        Check(p.m_seman.Effects.Count == 0, "unregistered effect applied");
        ObjectDB.instance = new ObjectDB();
        HelOathTouchStatus.Sync(p, 5);
        Check(p.m_seman.Effects.Count == 1, "registration not retried");
    }

    private static void HelStagesAndEnd()
    {
        Player p = Reset();
        for (int stage = 1; stage <= 5; stage++)
        {
            HelOathTouchStatus.Sync(p, stage);
            Check(p.m_seman.Effects.Count == 1, "stage transition duplicated effect");
            var effect = (SE_Stats)p.m_seman.Effects[0];
            Check(effect.m_name == HelOathIds.TouchNameKey(stage) && effect.m_tooltip == HelOathIds.TouchDescriptionKey(stage), "incorrect stage keys");
            Check(effect.m_speedModifier == stage * HelOathState.TouchSpeedPerStack, "incorrect stage speed");
        }
        HelOathTouchStatus.Sync(p, 0);
        Check(p.m_seman.Effects.Count == 0, "inactive status survived");
    }

    private static void PeltLateIconAndFastPath()
    {
        Player p = Reset();
        PeltOfHelheimStatus.Sync(p, true);
        var prototype = (SE_Stats)ObjectDB.instance.m_StatusEffects[0];
        var mods = prototype.m_mods;
        int enumerations = ObjectDB.instance.m_StatusEffects.Enumerations;
        var icon = new Sprite();
        PeltOfHelheimItemRegistration.CapeItemDrop = WithIcon(icon);
        PeltOfHelheimStatus.EnsureRegistered();
        Check(ReferenceEquals(prototype.m_icon, icon) && ReferenceEquals(p.m_seman.Effects[0].m_icon, icon), "Pelt late icon not backfilled");
        Check(ObjectDB.instance.m_StatusEffects.Enumerations == enumerations, "registered fast path rescanned ObjectDB");
        Check(ReferenceEquals(mods, prototype.m_mods), "registered fast path rebuilt modifiers");
        Check(mods.Count == 1 && mods[0].m_type == HitData.DamageType.Pierce && mods[0].m_modifier == HitData.DamageModifier.VeryResistant, "ward modifier changed");
    }

    private static void PeltRegistrationReset()
    {
        Player p = Reset();
        PeltOfHelheimStatus.Sync(p, true);
        var registered = ObjectDB.instance.m_StatusEffects[0];
        PeltOfHelheimStatus.ResetRegistration();
        PeltOfHelheimStatus.EnsureRegistered();
        Check(ObjectDB.instance.m_StatusEffects.Count == 1 && ReferenceEquals(registered, ObjectDB.instance.m_StatusEffects[0]), "reset duplicated registered status");
        ObjectDB.instance = new ObjectDB();
        PeltOfHelheimStatus.EnsureRegistered();
        Check(ObjectDB.instance.m_StatusEffects.Count == 1 && !ReferenceEquals(registered, ObjectDB.instance.m_StatusEffects[0]), "new database reused old prototype");
    }

    private static void PeltExternalRemoval()
    {
        Player p = Reset();
        PeltOfHelheimStatus.Sync(p, true);
        p.m_seman.RemoveStatusEffect(p.m_seman.Effects[0]);
        PeltOfHelheimStatus.Sync(p, true);
        Check(p.m_seman.Effects.Count == 1, "Pelt stale active reference blocked recovery");
    }

    private static void PeltOwnerSwap()
    {
        Player old = Reset();
        PeltOfHelheimStatus.Sync(old, true);
        Player next = Player.m_localPlayer = new Player();
        PeltOfHelheimStatus.Sync(next, true);
        Check(next.m_seman.Effects.Count == 1, "Pelt old-owner cache blocked new owner");
        Check(old.m_seman.Effects.Count == 1, "Pelt changed old owner during swap");
    }

    private static void PeltOwnerDisableIsolation()
    {
        Player old = Reset();
        PeltOfHelheimStatus.Sync(old, true);
        Player next = Player.m_localPlayer = new Player();
        var own = new SE_Stats { name = old.m_seman.Effects[0].name };
        next.m_seman.Effects.Add(own);
        PeltOfHelheimStatus.Sync(next, false);
        Check(next.m_seman.Removals == 0 && next.m_seman.Effects.Contains(own), "Pelt old-owner cache removed new owner's effect");
    }

    private static void PeltFailedAddRetry()
    {
        Player p = Reset();
        p.m_seman.FailAdd = true;
        PeltOfHelheimStatus.Sync(p, true);
        p.m_seman.FailAdd = false;
        PeltOfHelheimStatus.Sync(p, true);
        Check(p.m_seman.Effects.Count == 1, "Pelt failed addition not retried");
        PeltOfHelheimStatus.Sync(p, false);
        Check(p.m_seman.Effects.Count == 0, "Pelt disabled status survived");
    }

    private static void DebtStartRepeatEnd()
    {
        Player p = Reset();
        DelayedDoomDebtStatus.SetActive(p, true);
        var active = p.m_seman.Effects[0];
        Check(active.GetType() == typeof(StatusEffect), "debt indicator carries gameplay stat modifiers");
        Check(active.m_name == DelayedDoomIds.DebtNameKey && active.m_tooltip == DelayedDoomIds.DebtDescriptionKey && active.m_ttl == 0f, "debt label or lifetime wrong");
        DelayedDoomDebtStatus.SetActive(p, true);
        Check(p.m_seman.Effects.Count == 1 && p.m_seman.Adds == 1 && ReferenceEquals(active, p.m_seman.Effects[0]), "repeat debt created another status");
        DelayedDoomDebtStatus.SetActive(p, false);
        Check(p.m_seman.Effects.Count == 0, "ended debt retained indicator");
        Check(p.Health == 123f && p.Stamina == 87f, "indicator changed player combat values");
    }

    private static void DebtLateIconAndRecovery()
    {
        Player p = Reset();
        DelayedDoomDebtStatus.SetActive(p, true);
        var icon = new Sprite();
        DelayedDoomItemRegistration.DelayedDoomChestItemDrop = WithIcon(icon);
        DelayedDoomDebtStatus.SetActive(p, true);
        Check(ReferenceEquals(p.m_seman.Effects[0].m_icon, icon), "debt late icon not backfilled");
        p.m_seman.Effects.Clear();
        DelayedDoomDebtStatus.SetActive(p, true);
        Check(p.m_seman.Effects.Count == 1, "removed debt indicator not recovered");
    }

    private static void DebtFailureAndOwnerIsolation()
    {
        Player p = Reset();
        p.m_seman.ThrowOnAdd = true;
        DelayedDoomDebtStatus.SetActive(p, true);
        Check(p.Health == 123f && p.Stamina == 87f && p.m_seman.Effects.Count == 0, "presentation failure changed combat state");
        p.m_seman.ThrowOnAdd = false;
        DelayedDoomDebtStatus.SetActive(p, true);
        Check(p.m_seman.Effects.Count == 1, "debt failed addition not retried");
        Player next = Player.m_localPlayer = new Player();
        DelayedDoomDebtStatus.SetActive(p, false);
        Check(p.m_seman.Effects.Count == 1, "nonlocal owner changed");
        DelayedDoomDebtStatus.SetActive(next, true);
        Check(next.m_seman.Effects.Count == 1, "debt indicator not applied to new owner");
    }

    public static int Main()
    {
        int failures = 0;
        Action[] tests = { HelLateIcon, HelExternalRemoval, HelOwnerSwap, HelFailedAddRetry, HelRegistrationRetry,
            HelStagesAndEnd, PeltLateIconAndFastPath, PeltRegistrationReset, PeltExternalRemoval, PeltOwnerSwap,
            PeltOwnerDisableIsolation, PeltFailedAddRetry, DebtStartRepeatEnd, DebtLateIconAndRecovery, DebtFailureAndOwnerIsolation };
        foreach (Action test in tests)
        {
            try { test(); Console.WriteLine("PASS " + test.Method.Name); }
            catch (Exception error) { failures++; Console.WriteLine("FAIL " + test.Method.Name + ": " + error.Message); }
        }
        Console.WriteLine($"{tests.Length - failures}/{tests.Length} passed (boundary doubles; not a real-game test)");
        return failures == 0 ? 0 : 1;
    }
}
