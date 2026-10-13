using System;
using System.Collections.Generic;
namespace UnityEngine {
 public class GameObject { public string name; public ItemDrop drop; public T GetComponent<T>() where T:class { return drop as T; } }
 public static class Mathf { public static int Max(int a,int b)=>Math.Max(a,b); public static float Max(float a,float b)=>Math.Max(a,b); public static int Clamp(int v,int a,int b)=>Math.Max(a,Math.Min(b,v)); public static float Clamp(float v,float a,float b)=>Math.Max(a,Math.Min(b,v)); }
}
public class ItemDrop {
 public ItemData m_itemData = new ItemData();
 public class ItemData {
  public class SharedData { public string m_name; public int m_maxQuality=1; public float m_maxDurability, m_durabilityPerLevel; public bool m_canBeReparied,m_useDurability; }
  public SharedData m_shared=new SharedData(); public UnityEngine.GameObject m_dropPrefab; public int m_quality=1,m_variant; public float m_durability; public bool m_equipped; public long m_crafterID; public string m_crafterName;
 }
}
public class Inventory { public List<ItemDrop.ItemData> items=new List<ItemDrop.ItemData>(); public List<ItemDrop.ItemData> GetAllItems()=>items; }
public class Player { public Inventory inventory=new Inventory(); public Inventory GetInventory()=>inventory; }
public class Recipe { public string name; public UnityEngine.GameObject m_craftingStation,m_repairStation; }
public class ObjectDB { public static ObjectDB instance; public Dictionary<string,UnityEngine.GameObject> items=new Dictionary<string,UnityEngine.GameObject>(); public UnityEngine.GameObject GetItemPrefab(string s)=>items.ContainsKey(s)?items[s]:null; public Recipe GetRecipe(ItemDrop.ItemData item)=>null; }
namespace HaldorExpansion { static class HaldorExpansionPlugin { public static Logger Log=new Logger(); public static void DebugLog(string s) {} } class Logger { public void LogError(object s)=>Console.WriteLine(s); } }
namespace HaldorExpansion.Features.Berserker { static class BerserkerIds { public const string HelmetPrefab="helmet"; } }
namespace HaldorExpansion.Features.DelayedDoom { static class DelayedDoomIds { public const string ItemKey="$doom"; } static class DelayedDoomItemRegistration { public const string DelayedDoomChestPrefabName="doom"; } }
namespace HaldorExpansion.Compatibility {}
