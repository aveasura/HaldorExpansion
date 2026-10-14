using System;
using System.Collections.Generic;
namespace UnityEngine {
 public class Object { public string name; public static void Destroy(Object o){} public static T Instantiate<T>(T o) where T:Object => (T)(object)((Recipe)(object)o).Clone(); }
 public class GameObject:Object { public ItemDrop drop; public Container container; public T GetComponent<T>() where T:class => (drop as T) ?? (container as T); }
 public class ScriptableObject:Object { public static T CreateInstance<T>() where T:ScriptableObject,new()=>new T(); }
 public static class Random {public static int state; public static float next; public static float value {get {state++; return next;}} public static float Range(float min,float max)=>next;}
}
public class ItemDrop {
 public UnityEngine.GameObject gameObject;
 public ItemData m_itemData=new ItemData();
 public class ItemData {
  public class SharedData { public string m_name; public int m_maxQuality; public float m_upgradeChance; }
  public SharedData m_shared=new SharedData(); public UnityEngine.GameObject m_dropPrefab;
  public int m_quality=1,m_stack=1; public byte m_worldLevel; public float m_durability; public bool m_equipped;
  public Dictionary<string,string> m_customData=new Dictionary<string,string>();
  public ItemData Clone() => (ItemData)MemberwiseClone();
 }
}
public class Recipe:UnityEngine.ScriptableObject {
 public CraftingStation m_craftingStation,m_repairStation; public int m_minStationLevel; public ItemDrop m_item; public bool m_enabled=true,m_noCraftOnlyUpgrade,m_requireOnlyOneIngredient;
 public int m_amount; public Piece.Requirement[] m_resources;
 public Recipe Clone() => (Recipe)MemberwiseClone();
}
public class Piece {public class Requirement { public ItemDrop m_resItem; public int m_amount,m_amountPerLevel; public bool m_upgraderResource,m_recover; }}
public class ObjectDB { public static ObjectDB instance; public List<Recipe> m_recipes=new List<Recipe>();
 public List<UnityEngine.GameObject> m_items=new List<UnityEngine.GameObject>();
 public UnityEngine.GameObject GetItemPrefab(string name)=>m_items.Find(x=>x.name==name);
 public Recipe GetRecipe(ItemDrop.ItemData item)=>m_recipes.Find(x=>x.m_item.m_itemData.m_shared.m_name==item.m_shared.m_name);
}
public class CraftingStation {public bool m_upgrader;public EffectList m_craftItemDoneEffects=new EffectList(),m_craftItemDoneFailEffects=new EffectList();}
public class Inventory {
 public List<ItemDrop.ItemData> items=new List<ItemDrop.ItemData>();
 public List<ItemDrop.ItemData> GetAllItems()=>items;
 public bool ContainsItem(ItemDrop.ItemData item)=>items.Contains(item);
 public bool RemoveItem(ItemDrop.ItemData item,int n) { if(!items.Contains(item)||item.m_stack<n)return false; item.m_stack-=n; if(item.m_stack==0)items.Remove(item); return true; }
}
public class Player {
 public UnityEngine.Transform transform=new UnityEngine.Transform(); public void Message(MessageHud.MessageType t,string text,int n=0,object icon=null,bool log=false){}
 public static List<string> s_FilterCraft=new List<string>();
 public ItemDrop.ItemData GetCurrentWeapon()=>inventory.items.Find(x=>x.m_equipped);
 public CraftingStation station; public Inventory inventory=new Inventory();
 public Inventory GetInventory()=>inventory; public CraftingStation GetCurrentCraftingStation()=>station;
 public bool RequirementsMet=true; public bool HaveRequirements(Recipe recipe,bool discover,int quality) => RequirementsMet;
}
public class Game {public static int m_worldLevel;}
namespace HaldorExpansion.Features.Upgrades {
 internal static class IdolRegistration {
  internal static ItemDrop ItemDrop; internal static UnityEngine.GameObject Native;
  internal static bool IsNativeIdol(UnityEngine.GameObject p)=>p==Native;
 }
 internal static class AdamantineIdolRegistration {internal const string ForgeLabelKey="$label",ForgeWarningKey="$warning";internal static ItemDrop ItemDrop; internal static Chance ChestChance=new Chance(); internal static UnityEngine.GameObject Prefab=>ItemDrop?.gameObject;}
 internal class Chance {public float Value=1f;}

}
public class HitData {public enum DamageModifier {SlightlyResistant}}

public class Localization {public static Localization instance=new Localization(); public string Localize(string text,params string[] args)=>text;}
public class DropTable {public struct DropData {public UnityEngine.GameObject m_item;} public List<DropData> m_drops=new List<DropData>();}
public class Container {public DropTable m_defaultItems;}
public class Terminal {public void AddString(string text){}}
public class ZNetScene {public static ZNetScene instance;public List<UnityEngine.GameObject> m_prefabs=new List<UnityEngine.GameObject>();}
namespace Jotunn.Entities { public abstract class ConsoleCommand {public abstract string Name{get;}public abstract string Help{get;}public abstract void Run(string[] args,Terminal terminal);} }
namespace HaldorExpansion.Features.Upgrades {internal static class IdolContainerScopePatch {internal static Container Current;}}
namespace UnityEngine {
 public struct Vector3 {} public struct Quaternion {public static Quaternion identity=>new Quaternion();}
 public class Transform {public Vector3 position;public int childCount;public Transform GetChild(int n)=>new Transform();public Transform Find(string n)=>null;public T GetComponent<T>() where T:class=>null;}
 public class RectTransform:Transform {}
}
namespace TMPro {public class TMP_Text {public string text;}}
namespace HarmonyLib {
 public class HarmonyPatch:Attribute {public HarmonyPatch(Type t,string name){}}
 public static class AccessTools {public static System.Reflection.MethodInfo Method(Type t,string name)=>t.GetMethod(name,System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Static);}
}
public class InventoryGui {
 private struct RecipeDataPair {public Recipe Recipe {get; private set;} public RecipeDataPair(Recipe recipe){Recipe=recipe;}}
 private RecipeDataPair m_selectedRecipe;
 public void Select(Recipe recipe){m_selectedRecipe=new RecipeDataPair(recipe);}
 public int Refreshes;private void UpdateCraftingPanel(bool focusView=false){Refreshes++;}}
public class EffectList {public int Calls;public void Create(UnityEngine.Vector3 position,UnityEngine.Quaternion rotation){Calls++;}}
public class MessageHud {public enum MessageType {Center}}
namespace HaldorExpansion {internal static class HaldorExpansionPlugin {public static TestLog Log=new TestLog();}internal class TestLog {public int Errors;public void LogError(object message){Errors++;}}}
