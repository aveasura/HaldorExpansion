using System;
using HaldorExpansion.Features.Awakened;
class StatsRefreshTests
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static void Main()
    {
        var player = new Player(); Player.m_localPlayer = player;
        AwakenedStatRefresh.Refresh(player); Check(player.Reads == 0, "unrelated loadout does not alter cadence");
        player.CapeEquipped = true; player.Quality = 9;
        AwakenedStatRefresh.Refresh(player); Check(player.Reads == 0, "Q9 unchanged");
        player.Quality = 10; AwakenedStatRefresh.Refresh(player);
        Check(player.MaxHealth == 130 && player.Reads == 1 && player.Health == 70, "equipped Q10 transition refreshes without healing");
        AwakenedStatRefresh.Refresh(player); Check(player.Reads == 1, "unchanged signature does not stack or recompute");
        player.CapeEquipped = false; AwakenedStatRefresh.Refresh(player);
        Check(player.MaxHealth == 100 && player.Reads == 2, "unequip removes maximum immediately");
        player.PeltEquipped = true; AwakenedStatRefresh.Refresh(player);
        Check(player.MaxStamina == 130 && player.Stamina == 20, "pelt increases cap without refilling");
        player.PeltEquipped = false; player.Stamina = 120; AwakenedStatRefresh.Refresh(player);
        Check(player.MaxStamina == 50 && player.Stamina == 50, "pelt removal clamps existing stamina");
        var remote = new Player { CapeEquipped = true, Quality = 10 };
        AwakenedStatRefresh.Refresh(remote); Check(remote.Reads == 0, "remote player ignored");
        Player.m_localPlayer = remote; AwakenedStatRefresh.Refresh(remote);
        Check(remote.Reads == 1 && remote.MaxHealth == 130, "new player instance has independent state");
        remote.FoodHealth = 120; AwakenedStatRefresh.Refresh(remote);
        Check(remote.Reads == 1, "ordinary food decay retains native cadence");
        remote.Quality = 9; AwakenedStatRefresh.Refresh(remote);
        Check(remote.MaxHealth == 120, "downgrade recomputes current food rather than subtracting stale bonus");
        Console.WriteLine("Awakened stat refresh behavioral tests passed (game doubles).");
    }
}
public class Humanoid {}
public class Player : Humanoid
{
    public static Player m_localPlayer;
    public bool CapeEquipped, PeltEquipped;
    public int Quality, Reads;
    public float FoodHealth = 100, MaxHealth = 100, Health = 70, MaxStamina = 50, Stamina = 20;
    private void GetTotalFoodValue(out float hp, out float stamina, out float eitr)
    { Reads++; hp = FoodHealth + (AwakenedEquipment.Has(this, Artifact.WoundedBeastCloak) ? 30 : 0); stamina = 50 + (AwakenedEquipment.Has(this, Artifact.HelheimPelt) ? 80 : 0); eitr = 0; }
    public void SetMaxHealth(float value, bool flash) { MaxHealth = value; Health = Math.Min(Health,value); }
    public void SetMaxStamina(float value, bool flash) { MaxStamina = value; Stamina = Math.Min(Stamina,value); }
}
namespace HaldorExpansion.Features.Awakened
{
    internal enum Artifact { WoundedBeastCloak, HelheimPelt }
    internal static class AwakenedEquipment { internal static bool Has(Player p, Artifact artifact) => p.Quality >= 10 && (artifact == Artifact.WoundedBeastCloak ? p.CapeEquipped : p.PeltEquipped); }
}
namespace HarmonyLib
{
    public class HarmonyPatch : Attribute { public HarmonyPatch(Type type, string method) {} }
    public static class AccessTools { public static System.Reflection.MethodInfo Method(Type type, string name) => type.GetMethod(name,System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance); }
}
