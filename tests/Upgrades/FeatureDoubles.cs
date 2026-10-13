using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
namespace UnityEngine {
 public static class Random {public static int state;}
 public class Object { public string name; }
 public class Component:Object { public GameObject gameObject; public Transform transform=new Transform(); public T GetComponent<T>() where T:class=>gameObject.GetComponent<T>(); }
 public class Transform { public Vector3 position; }
 public struct Vector3 { public static Vector3 up=>new Vector3(); public static Vector3 operator +(Vector3 a,Vector3 b)=>a; }
 public struct Quaternion { public static Quaternion identity=>new Quaternion(); }
 public class GameObject:Object { public ItemDrop drop; public Container container; public T GetComponent<T>() where T:class=>(drop as T)??(container as T); }
 public class ScriptableObject:Object {public static T CreateInstance<T>() where T:ScriptableObject,new()=>new T();}
}
public class ItemDrop:UnityEngine.Component {
 public static List<ItemData> Dropped=new List<ItemData>(); public ItemData m_itemData=new ItemData();public static ItemDrop DropItem(ItemData i,int n,UnityEngine.Vector3 v,UnityEngine.Quaternion q){Dropped.Add(i);return new ItemDrop{m_itemData=i};}
 public class ItemData {
  public class SharedData {public string m_name,m_description;public int m_maxQuality=1,m_maxStackSize=20,m_value;public float m_maxDurability=100f,m_durabilityPerLevel;public float m_upgradeChance,m_breakChance,m_breakReturnIngreientsAmount; public bool m_questItem;public SharedData Clone()=>(SharedData)MemberwiseClone();}
  public SharedData m_shared=new SharedData();public UnityEngine.GameObject m_dropPrefab;public float m_durability;public int m_quality=1,m_stack=1;public bool m_equipped,m_cheated;public byte m_worldLevel;
  public float GetMaxDurability(int quality)=>m_shared.m_maxDurability+(quality-1)*m_shared.m_durabilityPerLevel;
  public ItemData Clone()=>(ItemData)MemberwiseClone();
 }
}
public class CraftingStation:UnityEngine.Object {public bool m_upgrader;}
public class Recipe:UnityEngine.ScriptableObject {public ItemDrop m_item;public bool m_enabled=true,m_noCraftOnlyUpgrade;public int m_amount=1,m_minStationLevel=1;public CraftingStation m_craftingStation,m_repairStation;public Piece.Requirement[] m_resources=new Piece.Requirement[0];}
public class Piece {public class Requirement {public ItemDrop m_resItem;public int m_amount=1,m_amountPerLevel=1;public bool m_upgraderResource,m_recover=true;public int GetAmount(int q)=>q<=1?m_amount:(int)Math.Floor((q>=4?4f+(q-4)/2f:q-1)*m_amountPerLevel+(m_upgraderResource?m_amount:0));}}
public class ObjectDB {public static ObjectDB instance;public bool UseHashLookup;public Dictionary<string,UnityEngine.GameObject> hashes=new Dictionary<string,UnityEngine.GameObject>();public void UpdateRegisters(){hashes.Clear();foreach(var p in m_items)hashes.Add(p.name,p);}public List<UnityEngine.GameObject> m_items=new List<UnityEngine.GameObject>();public List<Recipe> m_recipes=new List<Recipe>();public UnityEngine.GameObject GetItemPrefab(string name)=>UseHashLookup?(hashes.ContainsKey(name)?hashes[name]:null):m_items.Find(x=>x.name==name);public Recipe GetRecipe(ItemDrop.ItemData item)=>m_recipes.Find(x=>x.m_item.m_itemData.m_shared.m_name==item.m_shared.m_name);}
public class Inventory {public bool AddItem(ItemDrop.ItemData i){items.Add(i);return true;}public List<ItemDrop.ItemData> items=new List<ItemDrop.ItemData>();public List<ItemDrop.ItemData> GetAllItems()=>items;public bool ContainsItem(ItemDrop.ItemData i)=>items.Contains(i);}
public class Character {}
public class Humanoid:Character {public Inventory inventory=new Inventory();public Inventory GetInventory()=>inventory;}
public class Player:Humanoid {public void GetAvailableRecipes(ref List<Recipe> list){}public static List<string> s_FilterCraft=new List<string>();public CraftingStation station;public CraftingStation GetCurrentCraftingStation()=>station;}
public class InventoryGui {}
public class StatusEffect:UnityEngine.ScriptableObject {protected Character m_character;public string m_tooltip,m_name;public virtual void Setup(Character c){m_character=c;}public virtual void SetLevel(int i,float f){}public virtual string GetTooltipString()=>m_tooltip;public virtual void ModifyMaxCarryWeight(float b,ref float l){} }
public class SE_Stats:StatusEffect {public float m_addMaxCarryWeight;}
public class Localization {public static Localization instance=new Localization();public string ringName="Corrupted Brisingamen";public string Localize(string s,params string[] args)=>s=="$se_brisingamen_carry_desc"?"+{0:0.##} carry weight":s=="$item_brisingamen"?ringName:s;}
public class Game {public static int m_worldLevel;}
public class Container:UnityEngine.Component {public DropTable m_defaultItems=new DropTable();public Inventory inventory=new Inventory();public Inventory GetInventory()=>inventory;}
public class DropTable {
 public struct DropData {public UnityEngine.GameObject m_item;}
 public List<DropData> m_drops=new List<DropData>();public int calls;
 public Queue<List<ItemDrop.ItemData>> itemRolls=new Queue<List<ItemDrop.ItemData>>();public Queue<List<UnityEngine.GameObject>> objectRolls=new Queue<List<UnityEngine.GameObject>>();
 static List<ItemDrop.ItemData> buffer=new List<ItemDrop.ItemData>();
 public List<ItemDrop.ItemData> GetDropListItems(){calls++;UnityEngine.Random.state++;buffer.Clear();buffer.AddRange(itemRolls.Dequeue());return (List<ItemDrop.ItemData>)Invoke("AppendItems",buffer);}
 public List<UnityEngine.GameObject> GetDropList(){calls++;return (List<UnityEngine.GameObject>)Invoke("AppendObjects",objectRolls.Dequeue());}
 object Invoke(string n,object value){var t=typeof(DropTable).Assembly.GetType("HaldorExpansion.Features.Upgrades.IdolLoot");return t==null?value:t.GetMethod(n,BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new[]{(object)this,value});}
}
public class CharacterDrop {public class Drop {public UnityEngine.GameObject m_prefab;} public List<Drop> m_drops=new List<Drop>();public static Dictionary<string,Tuple<float,int>> s_pseudoCounter=new Dictionary<string,Tuple<float,int>>();public Queue<List<KeyValuePair<UnityEngine.GameObject,int>>> rolls=new Queue<List<KeyValuePair<UnityEngine.GameObject,int>>>();public int calls,throwOnCall;
 public List<KeyValuePair<UnityEngine.GameObject,int>> GenerateDropList(){calls++;if(calls==throwOnCall)throw new InvalidOperationException("deliberate sample failure");Tuple<float,int> old;s_pseudoCounter.TryGetValue("probe",out old);s_pseudoCounter["probe"]=Tuple.Create(.2f,(old==null?0:old.Item2)+1);var result=rolls.Dequeue();var t=typeof(CharacterDrop).Assembly.GetType("HaldorExpansion.Features.Upgrades.IdolLoot");return (List<KeyValuePair<UnityEngine.GameObject,int>>)t.GetMethod("AppendCharacter",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{this,result});}}
namespace HarmonyLib {public class HarmonyPatch:Attribute {public HarmonyPatch(Type t,string n){}public HarmonyPatch(Type t,string n,Type[] types){}}}
namespace Jotunn.Entities {public class CustomItem {public UnityEngine.GameObject ItemPrefab;public CustomItem(UnityEngine.GameObject p,bool fixReference){ItemPrefab=p;}}}
namespace Jotunn.Managers {
 public class ItemManager {public static ItemManager Instance=new ItemManager();public Dictionary<string,Jotunn.Entities.CustomItem> custom=new Dictionary<string,Jotunn.Entities.CustomItem>();public Jotunn.Entities.CustomItem GetItem(string name)=>custom.ContainsKey(name)?custom[name]:null;public bool AddItem(Jotunn.Entities.CustomItem item){custom.Add(item.ItemPrefab.name,item);RegisterItemInObjectDB(item.ItemPrefab);return true;}public void RegisterItemInObjectDB(UnityEngine.GameObject p){var db=ObjectDB.instance;if(db.GetItemPrefab(p.name)==null){db.m_items.Add(p);db.hashes.Add(p.name,p);}}}
 public class PrefabManager {public static PrefabManager Instance=new PrefabManager();public UnityEngine.GameObject CreateClonedPrefab(string n,string source){var src=ObjectDB.instance.GetItemPrefab(source);var p=new UnityEngine.GameObject{name=n};p.drop=new ItemDrop{gameObject=p};p.drop.m_itemData=src.drop.m_itemData.Clone();p.drop.m_itemData.m_shared=src.drop.m_itemData.m_shared.Clone();return p;}}
}
namespace HaldorExpansion {static class HaldorExpansionPlugin {public static Logger Log=new Logger();}class Logger {public void LogInfo(object s)=>Console.WriteLine(s);public void LogWarning(object s)=>Console.WriteLine(s);public void LogError(object s)=>Console.WriteLine(s);}}
namespace HaldorExpansion.Integration {static class ItemPrefabLookup {public static UnityEngine.GameObject Registered(string s)=>ObjectDB.instance.GetItemPrefab(s)??Jotunn.Managers.ItemManager.Instance.GetItem(s)?.ItemPrefab;}}
namespace HaldorExpansion.Features.Berserker {static class BerserkerIds {public const string HelmetPrefab="aveasura_he_berserker_helmet",AxePrefab="aveasura_he_berserker_axe",CloakPrefab="aveasura_he_berserker_cloak";}}
namespace HaldorExpansion.Features.DelayedDoom {static class DelayedDoomItemRegistration {public const string DelayedDoomChestPrefabName="ArmorDelayedDoomChest";}}
namespace HaldorExpansion.Features.PitKing {static class PitKingItemRegistration {public const string PitKingChestPrefabName="ArmorPitKingChest";}}
namespace HaldorExpansion.Features.Brisingamen {static class BrisingamenItemRegistration {public const string RingPrefabName="BrisingamenRing";}static class BrisingamenIds {public const string ItemKey="$item_brisingamen",EffectDescriptionKey="$se_brisingamen_carry_desc";}}

public class ZDO {}

public class ZNetScene {public static ZNetScene instance;public List<UnityEngine.GameObject> m_prefabs=new List<UnityEngine.GameObject>();}
public class Terminal {public List<string> lines=new List<string>();public void AddString(string text){lines.Add(text);}}
namespace Jotunn.Entities {public abstract class ConsoleCommand {public abstract string Name{get;}public abstract string Help{get;}public virtual void Run(string[] args,Terminal terminal){}}}
namespace Jotunn.Managers {public class CommandManager {public static CommandManager Instance=new CommandManager();public List<Jotunn.Entities.ConsoleCommand> commands=new List<Jotunn.Entities.ConsoleCommand>();public void AddConsoleCommand(Jotunn.Entities.ConsoleCommand c){commands.Add(c);}}}

namespace HaldorExpansion.Features.Cestus { static class CestusItemRegistration { public const string GritCestusPrefabName="Bone_crushers"; }}
namespace HaldorExpansion.Features.HelOath { static class HelOathItemRegistration { public const string HelOathPrefabName="BowHelOath"; }}
namespace HaldorExpansion.Features.ShadowCrossbow { static class ShadowCrossbowItemRegistration { public const string ShadowCrossbowPrefabName="CrossbowSilentHunt"; }}
namespace HaldorExpansion.Features.PeltOfHelheim { static class PeltOfHelheimItemRegistration { public const string PrefabName="CapePeltOfHelheim"; }}
namespace HaldorExpansion.Features.WoundedBeast { static class WoundedBeastItemRegistration { public const string WoundedBeastCapePrefabName="CapeWoundedBeast"; }}
namespace HaldorExpansion.Features.StaminaCape { static class StaminaCapeItemRegistration { public const string PrefabName="CapeFracturedBreath"; }}
