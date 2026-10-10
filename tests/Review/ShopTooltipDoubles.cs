using System;
using System.Collections.Generic;
using System.Reflection;
namespace UnityEngine {
 public struct Vector2 { public static Vector2 zero => new Vector2(); }
 public class Transform { public Transform parent; public bool IsChildOf(Transform root) { for(var p=this;p!=null;p=p.parent) if(p==root)return true; return false; } }
 public class RectTransform:Transform {}
 public class GameObject { public bool activeSelf=true; public Transform transform=new RectTransform(); }
}
namespace HarmonyLib { public static class AccessTools { public static FieldInfo Field(Type type,string name) => type.GetField(name, BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance); } }
public class StoreGui {
 public static StoreGui instance;
 public UnityEngine.GameObject m_rootPanel=new UnityEngine.GameObject();
 public UnityEngine.RectTransform m_listRoot=new UnityEngine.RectTransform(), m_tooltipAnchor=new UnityEngine.RectTransform();
 private Trader m_trader;
 private List<UnityEngine.GameObject> m_itemList;
 public StoreGui(Trader trader,List<UnityEngine.GameObject> rows){m_trader=trader;m_itemList=rows;instance=this;}
}
public class Trader { public bool Haldor=true; public class TradeItem { public bool Owned=true;public ItemDrop m_prefab;public int m_stack; } }
public class ItemDrop {
 public ItemData m_itemData=new ItemData();
 public class ItemData {
  public Shared m_shared=new Shared(); public int Calls,LastStack; public string Config="v1"; public bool Throw;
  public string GetTooltip(int stack){Calls++;LastStack=stack;if(Throw)throw new Exception("native failure");return Config+":"+stack;}
 }
 public class Shared { public string m_name="item"; }
}
public class UITooltip {
 public UnityEngine.GameObject gameObject=new UnityEngine.GameObject();public UnityEngine.Transform transform=>gameObject.transform;
 public string m_topic="placeholder",m_text="short";public int Sets;public UnityEngine.RectTransform Anchor;
 // Models the native Set callback when the pointer is over this selectable and it is not current.
 public bool PointerOver, Current;public Action HoverPrefix;
 public void Set(string topic,string text,UnityEngine.RectTransform anchor,UnityEngine.Vector2 position){Sets++;Anchor=anchor;if(topic==m_topic&&text==m_text)return;m_topic=topic;m_text=text;if(!Current&&PointerOver&&HoverPrefix!=null){HoverPrefix();Current=true;}}
}
namespace HaldorExpansion.Trading { public static class HaldorShopUpdater {public static bool IsHaldor(Trader t)=>t!=null&&t.Haldor; public static bool IsOwned(Trader.TradeItem t)=>t.Owned;} }
namespace HaldorExpansion.Presentation { public static class HaldorTabsController { public static List<Trader.TradeItem> Visible=new List<Trader.TradeItem>(); public static int Reads; public static List<Trader.TradeItem> GetVisibleItems(Trader t){Reads++;return Visible;} } }
namespace HaldorExpansion {public static class HaldorExpansionPlugin {public static Logger Log=new Logger();public class Logger {public int Warnings;public void LogWarning(string text){Warnings++;}}} }
