// Network/Unity stand-ins; ConfigFile and ConfigEntry are the real BepInEx DLL.
using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
namespace UnityEngine { public static class Time { public static float unscaledTime; } }
namespace HarmonyLib { public static class AccessTools {
 public static System.Reflection.FieldInfo Field(Type t,string n)=>t.GetField(n,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public);
 public static System.Reflection.MethodInfo Method(Type t,string n)=>t.GetMethod(n,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public);
} }
namespace Jotunn.Entities { public class CustomRPC { public void SendPackage(List<ZNetPeer> p,ZPackage z){} } }
namespace Jotunn.Managers {
 public class SynchronizationManager { public static SynchronizationManager Instance=new SynchronizationManager(); public void RegisterCustomConfig(ConfigFile c){} public void AddInitialSynchronization(Jotunn.Entities.CustomRPC r,Func<ZPackage> f){} }
 public class NetworkManager { public static NetworkManager Instance=new NetworkManager(); public Jotunn.Entities.CustomRPC AddRPC(string n,Func<long,ZPackage,IEnumerator> a,Func<long,ZPackage,IEnumerator> b)=>new Jotunn.Entities.CustomRPC(); }
}
public class ZNet { public static ZNet instance; public bool Server; public bool IsServer()=>Server; public List<ZNetPeer> GetPeers()=>new List<ZNetPeer>(); public ZNetPeer GetServerPeer()=>new ZNetPeer(); }
public class ZNetPeer { public long m_uid; }
public class ZPackage { string text; public void Write(string t){text=t;} public string ReadString()=>text; }
public class ObjectDB { public static ObjectDB instance; }
public class Trader {}
public class StoreGui { public static StoreGui instance; public static bool IsVisible()=>false; }
namespace HaldorExpansion {
 public class FakeLog { public void LogInfo(object s){} public void LogWarning(object s){} public void LogError(object s){} }
 public class HaldorExpansionPlugin { public ConfigFile Config; public PluginInfo Info; public static FakeLog Log=new FakeLog(); }
}
namespace HaldorExpansion.Trading {
 public static class TrophyValueUpdater { public static void ApplyTrophyValues(ObjectDB d){} }
 public static class HaldorShopUpdater { public static bool IsHaldor(Trader t)=>false; }
}
namespace HaldorExpansion.Patches { public static class HaldorShopExpansionPatch { public static void Refresh(Trader t){} } }
namespace HaldorExpansion.Presentation { public static class ModLocalization { public static void AddLocalizations(){} } }
