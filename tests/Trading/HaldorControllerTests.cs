using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Configuration;
using HaldorExpansion.Configuration;
using HaldorExpansion.Presentation;
using HaldorExpansion.Trading;
using System.Reflection;
namespace HarmonyLib { static class AccessTools {
 public static FieldInfo Field(Type t,string n)=>t.GetField(n,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
 public static MethodInfo Method(Type t,string n)=>t.GetMethod(n,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
} }
class Root { public bool activeSelf=true; }
namespace UnityEngine { static class Time { public static int frameCount; } }
namespace UnityEngine.UI {
 public class Scrollbar { public float value; }
 public class ScrollRect {
  public Scrollbar verticalScrollbar; public bool vertical=true;
  public float verticalNormalizedPosition; public int stopCalls;
  public void StopMovement(){stopCalls++;}
 }
}
class ItemDrop {
 public string name; public Data m_itemData=new Data();
 public class Data { public Shared m_shared=new Shared(); }
 public class Shared { public string m_name=""; public string m_itemType="Material"; }
}
class Trader {
 public bool haldor=true;
 public class TradeItem { public ItemDrop m_prefab; }
 public List<TradeItem> stock=new List<TradeItem>();
 public List<TradeItem> GetAvailableItems()=>new List<TradeItem>(stock);
}
class StoreGui {
 public static StoreGui instance;
 public Root m_rootPanel=new Root(); public UnityEngine.UI.Scrollbar m_listScroll=new UnityEngine.UI.Scrollbar();
 public UnityEngine.UI.ScrollRect listRect=new UnityEngine.UI.ScrollRect(); public int scrollSearches;
 public StoreGui(){listRect.verticalScrollbar=m_listScroll;}
 public T[] GetComponentsInChildren<T>(bool includeInactive) where T:class {
  scrollSearches++; return new T[]{listRect as T};
 }
 public Trader m_trader; public object m_selectedItem;
 public List<Trader.TradeItem> visible; public int fills;
 public void FillList() { fills++;visible=HaldorTabsController.GetVisibleItems(m_trader); m_selectedItem=visible.Count>0?visible[0]:null; }
}
static class ZInput { public static bool GetButtonDown(string s)=>false; }
static class HaldorExpansionPlugin { public static Logger Log=new Logger(); }
class Logger { public void LogWarning(string s) { throw new Exception(s); } }
namespace HaldorExpansion.Trading { static class HaldorShopUpdater { public static bool IsHaldor(Trader t)=>t!=null&&t.haldor; } }
namespace HaldorExpansion.Presentation {
 sealed class HaldorTabsView:IDisposable {
  public StoreGui Gui; public static HaldorTabsView Create(StoreGui g)=>new HaldorTabsView{Gui=g};
  public void Select(ShopCategory c){} public void Cycle(ShopCategory c,int d){} public void UpdateLayout(){} public void Dispose(){}
 }
}
class HaldorControllerTests {
 static void Check(bool b,string n){if(!b)throw new Exception(n);Console.WriteLine("PASS: "+n);}
 static string Names(List<Trader.TradeItem> l)=>string.Join(",",l.ConvertAll(x=>x.m_prefab.name));
 static void Main(){
  string path=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".cfg");
  var config=new ConfigFile(path,false);TradingUiConfiguration.Register(config);
  var t=new Trader();foreach(var id in new[]{"Copper","Wood","Sap","Iron","Eitr"})t.stock.Add(new Trader.TradeItem{m_prefab=new ItemDrop{name=id}});
  var gui=new StoreGui{m_trader=t};StoreGui.instance=gui;HaldorTabsController.Sync(gui);
  Check(Names(gui.visible)=="Copper,Wood,Sap,Iron,Eitr","initial shop preserves order");
  config["Trading UI","Show Metals tab"].BoxedValue=false;HaldorTabsController.Tick(gui);
  Check(Names(gui.visible)=="Copper,Wood,Sap,Iron,Eitr","hidden tab alone does not hide items");
  config["Trading UI","Hide items from disabled tabs"].BoxedValue=true;HaldorTabsController.Tick(gui);
  Check(Names(gui.visible)=="Wood,Sap,Eitr","live sales ban refreshes ALL");
  config["Trading UI","Enable Haldor tabs"].BoxedValue=false;HaldorTabsController.Tick(gui);
  Check(Names(gui.visible)=="Wood,Sap,Eitr","sales ban remains with UI off");
  config["Trading UI","Show Metals tab"].BoxedValue=true;HaldorTabsController.Tick(gui);
  Check(Names(gui.visible)=="Copper,Wood,Sap,Iron,Eitr","restoring category preserves item order with UI off");
  config["Trading UI","Enable Haldor tabs"].BoxedValue=true;HaldorTabsController.Tick(gui);
  HaldorTabsController.Select(ShopCategory.Metals);
  int searches=gui.scrollSearches;
  Check(Names(gui.visible)=="Copper,Iron","metal tab shows matching products");
  Check(gui.m_listScroll.value==1f&&gui.listRect.verticalNormalizedPosition==1f,
   "switching categories immediately resets linked shop list and scrollbar to top");
  Check(gui.scrollSearches==searches,"linked ScrollRect is resolved only once per GUI");
  // Simulate Unity changing content bounds later in the same frame. Only the
  // next frame should correct the scroll; subsequent frames must allow user scrolling.
  gui.listRect.verticalNormalizedPosition=0.5f;gui.m_listScroll.value=0.5f;
  HaldorTabsController.Tick(gui);
  Check(gui.listRect.verticalNormalizedPosition==0.5f,
   "layout correction is deferred until a subsequent frame");
  UnityEngine.Time.frameCount++;HaldorTabsController.Tick(gui);
  Check(gui.listRect.verticalNormalizedPosition==1f&&gui.m_listScroll.value==1f,
   "next frame corrects list bounds without calling FillList again");
  int refreshed=gui.fills;
  gui.listRect.verticalNormalizedPosition=0.3f;UnityEngine.Time.frameCount++;
  HaldorTabsController.Tick(gui);
  Check(gui.listRect.verticalNormalizedPosition==0.3f&&gui.fills==refreshed,
   "deferred reset runs once and never blocks subsequent user scrolling");
  // Rapid changes replace the pending correction instead of accumulating work.
  HaldorTabsController.Select(ShopCategory.Wood);
  HaldorTabsController.Select(ShopCategory.All);
  gui.listRect.verticalNormalizedPosition=0.4f;
  UnityEngine.Time.frameCount++;HaldorTabsController.Tick(gui);
  Check(gui.listRect.verticalNormalizedPosition==1f&&gui.scrollSearches==searches,
   "rapid category changes leave a single pending correction");
  HaldorTabsController.Select(ShopCategory.Metals);
  gui.m_selectedItem=t.stock[0];config["Trading UI","Show Metals tab"].BoxedValue=false;HaldorTabsController.Tick(gui);
  Check(HaldorTabsController.Category==ShopCategory.All&&ReferenceEquals(gui.m_selectedItem,t.stock[1]),"hiding selected tab resets category and selects first allowed product");
  HaldorTabsController.Select(ShopCategory.Metals);
  Check(HaldorTabsController.Category==ShopCategory.All,"hidden category cannot be selected");
  foreach(var category in new[]{"Equipment","Wood","Metals","Resources","Other"})
   config["Trading UI","Show "+category+" tab"].BoxedValue=false;
  HaldorTabsController.Tick(gui);
  Check(gui.visible.Count==0 && gui.m_selectedItem==null,"all categories banned produces safe empty shop");
  int index=0;HaldorTabsController.ClampSelection(gui,ref index);
  Check(index==-1,"empty list cannot select an out-of-range purchase");
  config["Trading UI","Hide items from disabled tabs"].BoxedValue=false;HaldorTabsController.Tick(gui);
  Check(Names(gui.visible)=="Copper,Wood,Sap,Iron,Eitr","turning policy off restores ALL even with every category tab hidden");
  config["Trading UI","Hide items from disabled tabs"].BoxedValue=true;HaldorTabsController.Tick(gui);
  t.haldor=false;HaldorTabsController.Tick(gui);gui.FillList();
  Check(Names(gui.visible)=="Copper,Wood,Sap,Iron,Eitr","other traders unaffected");
  Check(Names(t.stock)=="Copper,Wood,Sap,Iron,Eitr","original stock never mutated");
  HaldorTabsController.Reset();
  int stops=gui.listRect.stopCalls;
  UnityEngine.Time.frameCount++;HaldorTabsController.Tick(gui);
  Check(gui.listRect.stopCalls==stops,"closing or changing trader cancels pending scroll work");
  if(File.Exists(path))File.Delete(path);
 }
}
