using System;
using System.Collections.Generic;
using HaldorExpansion.Presentation;
using UnityEngine;
class ShopTooltipTests {
 static int assertions;
 static void Check(bool ok,string message){assertions++;if(!ok)throw new Exception(message);}
 static Trader.TradeItem Item(string name,int stack=1){return new Trader.TradeItem{m_stack=stack,m_prefab=new ItemDrop{m_itemData=new ItemDrop.ItemData{m_shared=new ItemDrop.Shared{m_name=name}}}};}
 static void Main(){
  Trader.TradeItem hidden=Item("hidden"),first=Item("filtered-first",7),second=Item("filtered-second",13);
  var t1=new UITooltip();var t2=new UITooltip();var rows=new List<GameObject>{t1.gameObject,t2.gameObject};var trader=new Trader();var gui=new StoreGui(trader,rows);
  t1.transform.parent=gui.m_listRoot;t2.transform.parent=gui.m_listRoot;
  HaldorTabsController.Visible=new List<Trader.TradeItem>{first,second};
  ShopItemTooltip.Prepare(t2);
  Check(t2.m_topic=="filtered-second"&&t2.m_text=="v1:13","wrong filtered row");
  Check(second.m_prefab.m_itemData.LastStack==13,"stack not forwarded");
  Check(hidden.m_prefab.m_itemData.Calls==0&&first.m_prefab.m_itemData.Calls==0&&second.m_prefab.m_itemData.Calls==1,"nonhovered item calculated");
  Check(t2.Anchor==gui.m_tooltipAnchor,"native anchor not preserved");
  second.m_prefab.m_itemData.Config="updated-config";ShopItemTooltip.Prepare(t2);
  Check(t2.m_text=="updated-config:13"&&second.m_prefab.m_itemData.Calls==2,"repeat hover failed refresh");
  var observedData=second.m_prefab.m_itemData;
  Action<string,Action,Action> ignored=(label,change,restore)=>{int calls=observedData.Calls,sets=t2.Sets;change();ShopItemTooltip.Prepare(t2);Check(calls==observedData.Calls&&sets==t2.Sets,label);restore();};
  ignored("unowned changed",()=>second.Owned=false,()=>second.Owned=true);
  ignored("other trader changed",()=>trader.Haldor=false,()=>trader.Haldor=true);
  ignored("closed UI changed",()=>gui.m_rootPanel.activeSelf=false,()=>gui.m_rootPanel.activeSelf=true);
  ignored("outside list changed",()=>t2.transform.parent=null,()=>t2.transform.parent=gui.m_listRoot);
  ignored("unknown row changed",()=>rows.Remove(t2.gameObject),()=>rows.Add(t2.gameObject));
  ignored("out of range changed",()=>HaldorTabsController.Visible.Remove(second),()=>HaldorTabsController.Visible.Add(second));
  var prefab=second.m_prefab;ignored("missing prefab changed",()=>second.m_prefab=null,()=>second.m_prefab=prefab);
  var data=prefab.m_itemData;ignored("missing data changed",()=>prefab.m_itemData=null,()=>prefab.m_itemData=data);
  var shared=data.m_shared;ignored("missing shared changed",()=>data.m_shared=null,()=>data.m_shared=shared);
  ignored("missing gui changed",()=>StoreGui.instance=null,()=>StoreGui.instance=gui);
  ShopItemTooltip.Prepare(null);
  data.Throw=true;int before=t2.Sets;ShopItemTooltip.Prepare(t2);Check(t2.Sets==before&&HaldorExpansion.HaldorExpansionPlugin.Log.Warnings==1,"native failure did not preserve fallback");data.Throw=false;
  // Native UITooltip.Set may synchronously reenter the hover prefix.
  t2.PointerOver=true;t2.Current=false;t2.HoverPrefix=()=>ShopItemTooltip.Prepare(t2);data.Config="new-hover";
  int initial=data.Calls;ShopItemTooltip.Prepare(t2);Check(data.Calls==initial+1&&t2.m_text=="new-hover:13","reentrant native Set recalculated tooltip");
  data.m_shared.m_name="lore-item";int callsBeforeLore=data.Calls;
  ShopItemTooltip.Prepare(t2);
  Check(t2.m_text=="A legend without stats."&&data.Calls==callsBeforeLore,"shop must use lore without full tooltip");
  Check(t2.m_topic=="lore-item","lore preview must preserve item name");
  Console.WriteLine("PASS "+assertions+" assertions; native Set hover callback tooltip calculations="+(data.Calls-initial));
 }
}
