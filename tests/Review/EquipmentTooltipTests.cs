using System;
using System.Collections.Generic;
using HaldorExpansion.Features.Berserker;
using HaldorExpansion.Presentation;

internal static class EquipmentTooltipTests
{
    internal static void Run(Action<bool, string> check)
    {
        string lore = "<color=#C9A56B><b>Легенда</b></color>\nИстория.";
        string mechanics = "Расплата: наносит урон.";
        string stats = "$item_armor: <color=orange>25</color>\nПассивка";
        string tooltip = "$description\n\n" + stats;
        string description = lore + "\n\n" + mechanics;
        string result = ItemLoreTooltip.MoveToEnd(tooltip, "$description", description, lore);
        check(result == stats + "\n\n" + mechanics + "\n\n" + lore,
            "Stats and mechanics must precede lore, preserving tags and native tokens");
        check(ItemLoreTooltip.MoveToEnd(result, "$description", description, lore) == result,
            "Repeated processing must not duplicate lore");
        check(ItemLoreTooltip.MoveToEnd(tooltip, "$description", lore, lore) == stats + "\n\n" + lore,
            "Lore-only description still follows the complete stat/passive block");
        check(ItemLoreTooltip.MoveToEnd(description + "\n\n" + stats, "$description", description, lore) == result,
            "Already localized descriptions remain supported");
        check(ItemLoreTooltip.MoveToEnd("other mod tooltip", "$description", description, lore) == "other mod tooltip",
            "Unknown tooltip layout must stay unchanged");
        check(ItemLoreTooltip.MoveToEnd(tooltip, "$description", "unrelated description", lore) == tooltip,
            "Never remove unrecognized prose");
        check(ItemLoreTooltip.MoveToEnd(tooltip, "$description", description, "") == tooltip,
            "Missing lore must preserve tooltip");
        check(ItemLoreTooltip.MoveToEnd(null, "$description", description, lore) == null,
            "Missing tooltip is safe");

        var player = new Player();
        Player.m_localPlayer = player;
        player.Items.Add(new ItemDrop.ItemData
        {
            m_equipped = true,
            m_shared = new ItemDrop.ItemData.SharedData { m_name = BerserkerIds.HelmetItemKey }
        });
        int[] armor = { 0, 10, 15, 19, 22, 25 };
        float[] reduction = { 0f, .02f, .03f, .038f, .045f, .05f };
        float[] stagger = { 0f, .30f, .45f, .57f, .675f, .75f };
        for (int stage = 0; stage <= 5; stage++)
        {
            BerserkerRuntime.Stage = stage;
            check(BerserkerHelmetRuntime.ArmorBonus(player) == armor[stage], "Integer armor stage " + stage);
            check(Math.Abs(BerserkerHelmetRuntime.IncomingDamageMultiplier(player) - (1f - reduction[stage])) < .00001f,
                "Damage reduction stage " + stage);
            check(Math.Abs(BerserkerHelmetRuntime.IncomingStaggerMultiplier(player) - (1f - stagger[stage])) < .00001f,
                "Stagger reduction stage " + stage);
            player.Health = 50f;
            float healed = BerserkerHelmetRuntime.ApplyLifeSteal(player, 100f);
            check(Math.Abs(healed - reduction[stage] * 100f) < .0001f, "Lifesteal stage " + stage);
        }
        player.Health = 99f;
        check(BerserkerHelmetRuntime.ApplyLifeSteal(player, 100f) == 1f, "Heal cannot exceed missing health");
        check(BerserkerHelmetRuntime.ApplyLifeSteal(player, float.NaN) == 0f, "Reject invalid damage");
        player.Items[0].m_equipped = false;
        check(BerserkerHelmetRuntime.ArmorBonus(player) == 0f, "No bonus with unequipped helmet");
        check(BerserkerHelmetRuntime.ApplyLifeSteal(player, 100f) == 0f, "No healing with unequipped helmet");
    }
}

// Player/Unity boundaries only; the helmet's actual calculations run above.
public class ItemDrop
{
    public class ItemData
    {
        public SharedData m_shared;
        public bool m_equipped;
        public UnityEngine.GameObject m_dropPrefab;
        public class SharedData { public string m_name; }
    }
}
public class Inventory
{
    public readonly List<ItemDrop.ItemData> Items = new List<ItemDrop.ItemData>();
    public List<ItemDrop.ItemData> GetAllItems() => Items;
}
public class Player
{
    public static Player m_localPlayer;
    private readonly Inventory inventory = new Inventory();
    public List<ItemDrop.ItemData> Items => inventory.Items;
    public float Health = 100f;
    public bool IsDead() => false;
    public Inventory GetInventory() => inventory;
    public float GetMaxHealth() => 100f;
    public float GetHealth() => Health;
    public void Heal(float amount, bool showText) { Health += amount; }
}
namespace UnityEngine { public class GameObject { public string name; } }
namespace HaldorExpansion.Features.Berserker
{
    internal static class BerserkerRuntime
    {
        internal static int Stage;
        internal static int EffectiveWrathStage(Player player) => Stage;
    }
}
