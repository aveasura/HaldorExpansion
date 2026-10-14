using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HaldorExpansion.Configuration;
namespace HarmonyLib {
 static class AccessTools {
  public static FieldInfo Field(Type t,string n)=>t.GetField(n,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static);
  public static PropertyInfo Property(Type t,string n)=>t.GetProperty(n,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static);
  public static MethodInfo Method(Type t,string n)=>t.GetMethod(n,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static);
 }
 class Harmony {public Harmony(string s){} public void Patch(MethodInfo m,HarmonyMethod postfix){} }
 class HarmonyMethod { public HarmonyMethod(MethodInfo m){} }
}
class ZNet {public static ZNet instance=null; public bool IsServer()=>true;}
class Localization {public static Localization instance=null;public string GetSelectedLanguage()=>"English";}
class HaldorExpansionPlugin {public const string ModGuid="test.haldor";public static LogSink Log=new LogSink();}
class LogSink {public void LogInfo(string s){} public void LogWarning(string s){throw new Exception(s);}}
namespace HaldorExpansion.Configuration {
 class ConfigurationRuntime {
  public ConfigFile Balance {get;set;}
  public IEnumerable<ConfigEntryBase> TradingUiEntries=>new ConfigEntryBase[0];
  public string TradingDisplayName(ConfigEntryBase e,bool r)=>null;
 }
}
class Manager {public IList _filteredSetings=new ArrayList();}
class PluginGroup {public BepInPlugin Info;public IList Categories;}
class Category {public string Name; public Category(string n){Name=n;}}
class PropertyGroup {public BepInPlugin Info{get;set;} public IList Categories{get;set;}}
class CompatibilityOrderTests {
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS: "+name);}
 static void Main(){
  var method=typeof(ConfigurationManagerIntegration).GetMethod("PrioritizeCompatibility",BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public);
  Check(method!=null,"F1 category ordering callback exists");
  var trading=new Category("Trading UI");var compatibility=new Category("Compatibility");
  var configuration=new Category("Configuration");var balance=new Category("Balance");var shop=new Category("Shop Progression");
  var other=new ArrayList{new Category("Z"),new Category("Compatibility"),new Category("Trading UI")};
  var own=new ArrayList{configuration,balance,trading,compatibility,shop};
  var manager=new Manager();manager._filteredSetings.Add(new PluginGroup{Info=new BepInPlugin("another.mod","Other","1.0.0"),Categories=other});
  manager._filteredSetings.Add(new PluginGroup{Info=new BepInPlugin(HaldorExpansionPlugin.ModGuid,"Haldor","1.3.0"),Categories=own});
  method.Invoke(null,new object[]{manager});
  Check(ReferenceEquals(own[0],compatibility)&&ReferenceEquals(own[1],trading),"Compatibility and Trading UI are first and second");
  Check(ReferenceEquals(own[2],configuration)&&ReferenceEquals(own[3],balance)&&ReferenceEquals(own[4],shop),"remaining categories retain relative order and identity");
  Check(((Category)other[0]).Name=="Z"&&((Category)other[1]).Name=="Compatibility"&&((Category)other[2]).Name=="Trading UI","other plugins unchanged");
  method.Invoke(null,new object[]{manager});
  Check(ReferenceEquals(own[0],compatibility)&&ReferenceEquals(own[1],trading)&&own.Count==5,"repeated rebuild is idempotent");
  own.Remove(compatibility);method.Invoke(null,new object[]{manager});
  Check(ReferenceEquals(own[0],trading)&&ReferenceEquals(own[1],configuration)&&own.Count==4,"filtered-out Compatibility is not recreated or reordered");
  own.Remove(trading);own.Add(compatibility);method.Invoke(null,new object[]{manager});
  Check(ReferenceEquals(own[0],compatibility)&&ReferenceEquals(own[1],configuration)&&own.Count==4,"filtered-out Trading UI is not recreated");
  var prop=new PropertyGroup{Info=new BepInPlugin(HaldorExpansionPlugin.ModGuid,"Haldor","1.3.0"),Categories=new ArrayList{configuration,trading,balance,compatibility}};
  manager._filteredSetings=new ArrayList{prop};method.Invoke(null,new object[]{manager});
  Check(ReferenceEquals(prop.Categories[0],compatibility)&&ReferenceEquals(prop.Categories[1],trading)&&ReferenceEquals(prop.Categories[2],configuration),"property-based manager data supported");
 }
}
