using System;
using System.Collections.Generic;
using System.Reflection;
namespace HarmonyLib { public static class AccessTools { public static FieldInfo Field(Type t,string n)=>t.GetField(n); public static MethodInfo Method(Type t,string n)=>t.GetMethod(n); } }
namespace UnityEngine { public static class Time { public static float time; } public static class Mathf { public static float Max(float a,float b)=>Math.Max(a,b); public static float Min(float a,float b)=>Math.Min(a,b); public static float Clamp(float a,float b,float c)=>Math.Max(b,Math.Min(a,c)); } }
public class ZDO { public long m_uid; public long owner; public long GetOwner()=>owner; }
public class ZNetView {
 public ZDO zdo=new ZDO(); public bool owned=true; public static long Sender=1;
 public Dictionary<string,Action<long,ZPackage>> handlers=new Dictionary<string,Action<long,ZPackage>>();
 public bool IsValid()=>true; public bool IsOwner()=>owned; public ZDO GetZDO()=>zdo;
 public void Register<T>(string s,Action<long,T> a) { handlers[s]=(sender,p)=>a(sender,(T)(object)p); }
 public void InvokeRPC(string s, params object[] args) { handlers[s](Sender,(ZPackage)args[0]); }
}
public class ZPackage { readonly Queue<object> q=new Queue<object>(); public void Write(long v)=>q.Enqueue(v); public void Write(float v)=>q.Enqueue(v); public void Write(bool v)=>q.Enqueue(v); public long ReadLong()=>(long)q.Dequeue(); public float ReadSingle()=>(float)q.Dequeue(); public bool ReadBool()=>(bool)q.Dequeue(); }
public class ItemDrop { public class ItemData { public bool m_equipped=true; public int m_quality=10; public HaldorExpansion.Features.Awakened.Artifact kind; } }
public class Inventory { public List<ItemDrop.ItemData> items=new List<ItemDrop.ItemData>(); public List<ItemDrop.ItemData> GetAllItems()=>items; }
public class Character {
 public ZNetView m_nview=new ZNetView(); public float hp=100,max=100; public bool dead;
 public bool IsPlayer()=>this is Player; public bool IsDead()=>dead; public float GetHealth()=>hp; public float GetMaxHealth()=>max;
 public short FindWeakSpotIndex(object o)=>0; public void RPC_Damage(long sender,HitData hit) { var state=HaldorExpansion.Features.Awakened.AwakenedCombat.Observe(this,hit); hp-=hit.damage; HaldorExpansion.Features.Awakened.AwakenedCombat.Complete(this,state); }
}
public class Player:Character { public static Player m_localPlayer; public ItemDrop.ItemData weapon; public Inventory inventory=new Inventory(); public ItemDrop.ItemData GetCurrentWeapon()=>weapon; public Inventory GetInventory()=>inventory; public void Heal(float h,bool t){hp+=h;} }
public class Attack { public Character m_character; public ItemDrop.ItemData m_weapon; }
public class HitData { public bool m_ranged; public object m_hitCollider; public short m_weakSpot; public Player attacker; public float damage=100; public Character GetAttacker()=>attacker; public float GetTotalDamage()=>damage; public void Serialize(ref ZPackage p){} public void Deserialize(ref ZPackage p){} }
public static class BaseAI { public static bool IsEnemy(Player p,Character t)=>true; }
namespace HaldorExpansion { public static class HaldorExpansionPlugin { public static Logger Log; } public class Logger { public void LogWarning(string s){} } }
namespace HaldorExpansion.Features.Berserker { public static class BerserkerRuntime { public static float Debt; public static int Wrath=1; public static int EffectiveWrathStage(Player p)=>Wrath; public static void GrantAwakenedKillDebt(Player p){Debt+=40;} } }
namespace HaldorExpansion.Features.Awakened {
 public enum Artifact {None, BerserkerAxe, BerserkerHelmet, BoneCrushers}
 public static class AwakenedEquipment { public static Artifact Identify(ItemDrop.ItemData i)=>i.kind; public static bool IsAwakened(ItemDrop.ItemData i)=>i.m_quality>=10; public static bool Has(Player p,Artifact a)=>p.inventory.items.Exists(i=>i.kind==a && i.m_equipped && IsAwakened(i)); }
 public static class AwakenedBalance { public static class BoneCrushers {public const float LifeSteal=.03f;} public static class BerserkerHelmet {public const float MissingHealthOnKill=.1f;} }
}
