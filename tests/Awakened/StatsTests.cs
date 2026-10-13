using System;
using System.Collections.Generic;
using HaldorExpansion.Features.Awakened;
class StatsTests
{
    static int checks;
    static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    static void Near(float actual, float expected, string message) { Check(Math.Abs(actual - expected) < .001f, message); }
    static ItemDrop.ItemData Item(string key, int quality, bool equipped = true) => new ItemDrop.ItemData { m_shared = new ItemDrop.SharedData { m_name = key }, m_quality = quality, m_equipped = equipped };
    static void Main()
    {
        var p = new Player();
        var cape = Item("$item_cape_woundedbeast", 9); p.Inventory.Items.Add(cape);
        float hp = 100, stamina = 50;
        AwakenedStats.Food(p, ref hp, ref stamina); Near(hp, 100, "Q9 no health");
        cape.m_quality = 10;
        AwakenedStats.Food(p, ref hp, ref stamina); Near(hp, 130, "Q10 health");
        float level = 85; AwakenedStats.Skill(p, Skills.SkillType.Unarmed, ref level); Near(level, 100, "skill capped");
        cape.m_equipped = false; hp = 100; AwakenedStats.Food(p, ref hp, ref stamina); Near(hp, 100, "unequip removes immediately");
        var weak = Item("$item_cape_woundedbeast", 1); weak.m_shared = cape.m_shared; p.Inventory.Items.Add(weak);
        Check(!AwakenedEquipment.Has(p, Artifact.WoundedBeastCloak), "shared data cannot transfer awakening");
        var bow = Item("$item_crossbow_silenthunt", 10); p.Inventory.Items.Add(bow);
        Check(!AwakenedEquipment.Has(p, Artifact.ShadowCrossbow), "inventory weapon is insufficient");
        p.Weapon = bow; level = 20; AwakenedStats.Skill(p, Skills.SkillType.Sneak, ref level); Near(level, 80, "held crossbow sneak");
        level = 20; AwakenedStats.Skill(p, Skills.SkillType.Crossbows, ref level); Near(level, 50, "held crossbow skill");
        p.Weapon = null; level = 20; AwakenedStats.Skill(p, Skills.SkillType.Crossbows, ref level); Near(level, 20, "holstering removes bonus");
        p.Inventory.Items.Add(Item("$item_cape_peltofhelheim", 10));
        hp = 100; stamina = 50; AwakenedStats.Food(p, ref hp, ref stamina); Near(stamina, 130, "pelt stamina");
        float damage = 75; AwakenedStats.Fall(p, ref damage); Near(damage, 15, "pelt fall damage");
        p.Inventory.Items.Add(Item("$item_brisingamen", 10)); float regen = 1.5f;
        AwakenedStats.StaminaRegen(p, ref regen); Near(regen, 1.8f, "regen multiplies native modifiers");
        regen = 0; AwakenedStats.StaminaRegen(p, ref regen); Near(regen, 0, "zero native regen stays zero");
        Check(AwakenedStats.Owner(new SEMan(p)) == p, "SEMan runtime owner");
        Check(AwakenedEquipment.Identify(null) == Artifact.None, "null item");
        Check(!AwakenedEquipment.Has(p, Artifact.None), "None never active");
        Check(!AwakenedEquipment.IsAwakened(Item("ordinary", 10)), "ordinary Q10 item");
        p.Inventory.Items.Add(Item("$item_cape_fracturedbreath", 10));
        var mods = new HitData.DamageModifiers(); AwakenedStats.Armor(p, ref mods);
        Check(mods.LastApplied[0].m_type == HitData.DamageType.Slash && mods.LastApplied[0].m_modifier == HitData.DamageModifier.SlightlyResistant, "slash delegates native combination");
        string[] keys = { "$item_chest_delayeddoom", "$item_aveasura_he_berserker_helmet",
            "$item_aveasura_he_berserker_cloak", "$item_cape_fracturedbreath", "$item_cape_woundedbeast",
            "$item_cape_peltofhelheim", "$item_chest_pitking", "$item_aveasura_he_berserker_axe",
            "$item_weapon_gritcestus", "$item_bow_heloath", "$item_crossbow_silenthunt", "$item_brisingamen" };
        foreach (string key in keys)
        {
            var owner = new Player(); var instance = Item(key, 9); owner.Inventory.Items.Add(instance);
            owner.Weapon = instance;
            Artifact kind = AwakenedEquipment.Identify(instance);
            Check(kind != Artifact.None, "all twelve identities: " + key);
            Check(!AwakenedEquipment.Has(owner, kind), "Q9: " + key);
            foreach (int quality in new[] { 10, 11, 20 })
            {
                instance.m_quality = quality;
                Check(AwakenedEquipment.Has(owner, kind), "Q10+: " + key);
                instance.m_equipped = false; owner.Weapon = null;
                Check(!AwakenedEquipment.Has(owner, kind), "removed: " + key);
                instance.m_equipped = true; owner.Weapon = instance;
            }
            var low = Item(key, 1); low.m_shared = instance.m_shared;
            instance.m_equipped = false; owner.Inventory.Items.Add(low); owner.Weapon = low;
            Check(!AwakenedEquipment.Has(owner, kind), "different shared-data instance: " + key);
        }
        p = new Player(); cape = Item("$item_cape_woundedbeast", 11); p.Inventory.Items.Add(cape);
        for (int repeat = 0; repeat < 50; repeat++)
        {
            cape.m_equipped = true; hp = 100; stamina = 50;
            AwakenedStats.Food(p, ref hp, ref stamina); Near(hp, 130, "re-equip cannot accumulate health");
            cape.m_equipped = false; hp = 100;
            AwakenedStats.Food(p, ref hp, ref stamina); Near(hp, 100, "repeated removal");
        }
        Console.WriteLine("PASS awakened stats: " + checks + " checks (game doubles).");
    }
}
public class ItemDrop { public class SharedData { public string m_name; } public class ItemData { public SharedData m_shared; public UnityEngine.GameObject m_dropPrefab; public int m_quality; public bool m_equipped; } }
public class Inventory { public List<ItemDrop.ItemData> Items = new List<ItemDrop.ItemData>(); public List<ItemDrop.ItemData> GetAllItems() => Items; }
public class Player { public Inventory Inventory = new Inventory(); public ItemDrop.ItemData Weapon; public Inventory GetInventory() => Inventory; public ItemDrop.ItemData GetCurrentWeapon() => Weapon; }
public class SEMan { private Player m_character; public SEMan(Player player) { m_character = player; } }
public class Skills { public enum SkillType { Unarmed, Sneak, Crossbows } }
public class HitData { public enum DamageType { Slash } public enum DamageModifier { Normal, SlightlyResistant } public struct DamageModPair { public DamageType m_type; public DamageModifier m_modifier; } public struct DamageModifiers { public List<DamageModPair> LastApplied; public void Apply(List<DamageModPair> values) { LastApplied = values; } } }
namespace UnityEngine { public class GameObject { public string name; } public static class Mathf { public static float Min(float a, float b) => Math.Min(a,b); } }
namespace HarmonyLib { public class HarmonyPatch : Attribute { public HarmonyPatch(Type type, string method) {} } public class HarmonyPriority : Attribute { public HarmonyPriority(int value) {} } public static class Priority { public const int Last = 0; } public static class AccessTools { public static System.Reflection.FieldInfo Field(Type type, string name) => type.GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance); } }
