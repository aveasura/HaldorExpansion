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
  Check(method!=null,"Compatibility ordering callback exists");
  var a=new Category("Trading UI");var b=new Category("Compatibility");var c=new Category("Configuration");
  var other=new ArrayList{new Category("Z"),new Category("Compatibility")};
  var own=new ArrayList{a,b,c};
  var manager=new Manager();manager._filteredSetings.Add(new PluginGroup{Info=new BepInPlugin("another.mod","Other","1.0.0"),Categories=other});
  manager._filteredSetings.Add(new PluginGroup{Info=new BepInPlugin(HaldorExpansionPlugin.ModGuid,"Haldor","1.3.0"),Categories=own});
  method.Invoke(null,new object[]{manager});
  Check(ReferenceEquals(own[0],b)&&ReferenceEquals(own[1],a)&&ReferenceEquals(own[2],c),"Compatibility first, other groups retain order and identity");
  Check(((Category)other[0]).Name=="Z","other plugins unchanged");
  method.Invoke(null,new object[]{manager});Check(ReferenceEquals(own[0],b)&&own.Count==3,"repeated rebuild is idempotent");
  own.Remove(b);method.Invoke(null,new object[]{manager});Check(ReferenceEquals(own[0],a)&&own.Count==2,"filtered-out Compatibility is not recreated");
  var prop=new PropertyGroup{Info=new BepInPlugin(HaldorExpansionPlugin.ModGuid,"Haldor","1.3.0"),Categories=new ArrayList{a,b,c}};
  manager._filteredSetings=new ArrayList{prop};method.Invoke(null,new object[]{manager});Check(ReferenceEquals(prop.Categories[0],b),"property-based manager data supported");
 }
}
