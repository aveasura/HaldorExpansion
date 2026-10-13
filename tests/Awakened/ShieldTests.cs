using System;
using System.Collections.Generic;
using HaldorExpansion.Features.Cestus;
class ShieldTests
{
    static int checks;
    static void Near(float expected, float actual, string label) { checks++; if (Math.Abs(expected-actual)>.001f) throw new Exception(label); }
    static void Main()
    {
        var p = new Player(); var s = CestusStateStore.State; s.Player = p;
        s.AwakenedShieldBonusActive=true; s.ShieldRemaining=160; s.ShieldMax=200; s.ShieldDecayPerTick=5;
        CestusShieldRuntime.RefreshAwakenedShield(p,s); Near(160,s.ShieldRemaining,"equipped unchanged");
        p.Awakened=false; CestusShieldRuntime.RefreshAwakenedShield(p,s);
        Near(120,s.ShieldRemaining,"remaining loses only extra share"); Near(150,s.ShieldMax,"base set remains"); Near(3.75f,s.ShieldDecayPerTick,"decay scales with shield");
        CestusShieldRuntime.RefreshAwakenedShield(p,s); Near(120,s.ShieldRemaining,"no repeated scaling");
        p.Awakened=true; CestusShieldRuntime.RefreshAwakenedShield(p,s); Near(120,s.ShieldRemaining,"re-equip cannot refill");
        p.Awakened=false; s.AwakenedShieldBonusActive=true; s.ShieldRemaining=200; s.ShieldMax=200;
        Near(50,CestusShieldRuntime.AbsorbCestusShield(p,50),"absorption after removal"); Near(100,s.ShieldRemaining,"removal precedes damage absorption");
        p.Authority=false; s.AwakenedShieldBonusActive=true; s.ShieldRemaining=200;
        CestusShieldRuntime.RefreshAwakenedShield(p,s); Near(200,s.ShieldRemaining,"remote copy never changes authority state");
        Console.WriteLine("PASS awakened shield: " + checks + " checks");
    }
}
public class Player { public bool Awakened=true, Authority=true; public bool IsDead()=>false; public float GetHealth()=>50; }
public class HitData { public enum DamageModifier { SlightlyResistant } }
namespace UnityEngine {
 public class Coroutine {} public class WaitForSeconds {public WaitForSeconds(float value){}}
 public static class Mathf {public static float Max(float a,float b)=>Math.Max(a,b);public static float Min(float a,float b)=>Math.Min(a,b);public static float Clamp01(float v)=>Math.Max(0,Math.Min(1,v));}
}
namespace HaldorExpansion {
 public class HaldorExpansionPlugin {public static HaldorExpansionPlugin Instance;public void StopCoroutine(UnityEngine.Coroutine c){}public static void DebugLog(string s){}}
}
namespace HaldorExpansion.Diagnostics { public static class PlayerDiagnostics {public static string GetCestusDebugPlayerTag(Player p)=>"test";} }
namespace HaldorExpansion.Compatibility { public static class CharacterHealthAccess {public static float GetCharacterMaxHealth(Player p)=>100;public static void SetCharacterHealth(Player p,float v){}} }
namespace HaldorExpansion.Features.Awakened { internal enum Artifact {PitKingChest} internal static class AwakenedEquipment {internal static bool Has(Player p,Artifact a)=>p.Awakened;} }
namespace HaldorExpansion.Features.Cestus {
 internal static class CestusAbility {internal const float CestusSetBonusMultiplier=1.5f;}
 internal static class CestusEquipment {internal static bool HasCestusSetBonus(Player p)=>true;}
 internal static class CestusNetworkState {
  internal static float GetSyncedCestusShieldRemaining(Player p)=>0;
  internal static bool IsCestusShieldAuthorityRuntime(Player p)=>p.Authority;
  internal static void SyncCestusShieldState(Player p,CestusStateStore.CestusState s,bool force=false){}
 }
 internal static class CestusStateStore {
  internal class CestusState {internal Player Player;internal bool AwakenedShieldBonusActive;internal float ShieldRemaining,ShieldMax,ShieldDecayPerTick;internal int ShieldTicksLeft;internal UnityEngine.Coroutine ShieldRoutine;internal float LastSyncedShieldRemaining,LastSyncedShieldMax;internal bool LastSyncedShieldActive;}
  internal static CestusState State=new CestusState();internal static Dictionary<long,CestusState> ActiveCestusStates=new Dictionary<long,CestusState>();
  internal static CestusState GetOrCreateCestusState(Player p)=>State;internal static long GetCestusStateKey(Player p)=>1;
 }
}
