using System;
using System.IO;
using System.Reflection;
using BepInEx.Configuration;
using HaldorExpansion.Configuration;
using HaldorExpansion.Trading;
class AdamantineTradingTests
{
    static void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS: "+name);}
    static void Set(object o,string name,object value)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);
    static ConfigurationRuntime Create(string dir,ConfigFile trade,bool rare){
        var r=new ConfigurationRuntime();var general=new ConfigFile(Path.Combine(dir,"general.cfg"),false){SaveOnConfigSet=false};
        general.Bind("Compatibility","Enable long item tooltips",false,"");var interval=general.Bind("Configuration","Config check interval (seconds)",.5f,"");
        var balance=new ConfigFile(Path.Combine(dir,"balance.cfg"),false){SaveOnConfigSet=false};balance.Bind("Existing","Keep me",42,"");
        if(rare)balance.Bind("Forge of Possibilities","Adamantine Idol chest drop chance",1f,"");
        Set(r,"<General>k__BackingField",general);Set(r,"<Balance>k__BackingField",balance);Set(r,"<TradeEntries>k__BackingField",trade);
        Set(r,"pollInterval",interval);Set(r,"folder",dir);Set(r,"legacyPath",Path.Combine(dir,"legacy.cfg"));Set(r,"tradePath",Path.Combine(dir,"trading.yml"));
        Set(r,"newGeneral",!File.Exists(general.ConfigFilePath));Set(r,"newBalance",!File.Exists(balance.ConfigFilePath));Set(r,"newTrading",!File.Exists(Path.Combine(dir,"trading.yml")));return r;
    }
    static void Main(){
        string dir=Path.Combine(Path.GetTempPath(),"he-adamantine-trade-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
        try{
            var trade=new ConfigFile(Path.Combine(dir,".memory"),false){SaveOnConfigSet=false};
            new ShopConfiguration().RegisterShopProgressionConfig(trade);new SellPriceConfiguration().RegisterHaldorSellPriceConfig(trade);
            string id=HaldorIdolCatalog.AdamantineId;Check(HaldorIdolCatalog.Items.Length==18,"18 idol entries");
            foreach(var idol in HaldorIdolCatalog.Items){bool rare=idol.Id==id;
                Check(TradingCatalog.Defaults.Shop[idol.Id].Price==(rare?4000:1000)&&TradingCatalog.Defaults.Sell[idol.Id].Price==(rare?2500:300),idol.Id+" buy/sell defaults");
                Check(TradingCatalog.Stack(idol.Id,20,20)==1&&ShopCategoryFilter.Classify("Material",idol.Id,"")==ShopCategory.Other,"single purchase in Other: "+idol.Id);
            }
            var buy=TradingCatalog.Defaults.Shop[id];var sell=TradingCatalog.Defaults.Sell[id];TradingCatalog.Defaults.Shop.Remove(id);TradingCatalog.Defaults.Sell.Remove(id);
            var original=Create(dir,trade,false);original.Finish();string file=Path.Combine(dir,"trading.yml");
            var custom=TradingDocument.Parse(File.ReadAllText(file),TradingCatalog.Defaults);custom.Shop["Iron"].Price=12345;custom.Shop["Iron"].Enabled=false;custom.Shop["Iron"].RequiredKey="my_world_key";custom.Sell[HaldorIdolCatalog.CustomId].Price=333;File.WriteAllText(file,custom.Serialize());
            TradingCatalog.Defaults.Shop.Add(id,buy);TradingCatalog.Defaults.Sell.Add(id,sell);var upgraded=Create(dir,trade,true);upgraded.Finish();
            var doc=TradingDocument.Parse(File.ReadAllText(file),TradingCatalog.Defaults);
            Check(doc.Shop[id].Price==4000&&doc.Shop[id].Stack==1&&doc.Sell[id].Price==2500,"new buy/sell rows added to existing schema");
            Check(doc.Shop["Iron"].Price==12345&&!doc.Shop["Iron"].Enabled&&doc.Shop["Iron"].RequiredKey=="my_world_key"&&doc.Sell[HaldorIdolCatalog.CustomId].Price==333,"custom existing values preserved");
            Check((int)upgraded.Balance["Existing","Keep me"].BoxedValue==42&&(float)upgraded.Balance["Forge of Possibilities","Adamantine Idol chest drop chance"].BoxedValue==1f,"new balance entry; old value preserved");
            bool enabled=false,price=false,key=false,sellPrice=false;
            foreach(var entry in upgraded.TradingUiEntries){
                if(!entry.Definition.Key.StartsWith(id+" "))continue;string field=entry.Definition.Key.Substring(id.Length+1);
                if(field=="Enabled"){entry.BoxedValue=false;enabled=true;}
                if(field=="Price"&&entry.Definition.Section.StartsWith("Trading - Buy")){entry.BoxedValue=4321;price=true;}
                if(field=="RequiredGlobalKey"){entry.BoxedValue="defeated_fader";key=true;}
                if(field=="Price"&&entry.Definition.Section.StartsWith("Trading - Sell")){entry.BoxedValue=0;sellPrice=true;}
            }
            Check(enabled&&price&&key&&sellPrice,"four F1 trading controls present");
            Check(!TradingCatalog.Enabled(id)&&ShopConfiguration.GetShopPrice(id,0)==4321&&ShopConfiguration.GetShopRequiredGlobalKey(id)=="defeated_fader"&&SellPriceConfiguration.GetConfiguredHaldorSellPrice(id,-1)==0,"live F1 edits including sell=0");
            var disk=TradingDocument.Parse(File.ReadAllText(file),TradingCatalog.Defaults);Check(!disk.Shop[id].Enabled&&disk.Shop[id].Price==4321&&disk.Sell[id].Price==0,"F1 values persist in YAML");
            disk.Shop[id].Enabled=true;disk.Shop[id].Price=4444;disk.Shop[id].Stack=99;disk.Sell[id].Price=2222;File.WriteAllText(file,disk.Serialize());
            var poll=typeof(ConfigurationRuntime).GetMethod("Poll",BindingFlags.Instance|BindingFlags.NonPublic);poll.Invoke(upgraded,new object[]{file,null,false,true});poll.Invoke(upgraded,new object[]{file,null,false,true});
            Check(TradingCatalog.Enabled(id)&&ShopConfiguration.GetShopPrice(id,0)==4444&&SellPriceConfiguration.GetConfiguredHaldorSellPrice(id,0)==2222&&TradingCatalog.Stack(id,99,99)==1,"live YAML reload; stack forced to one");
            ZNet.instance=new ZNet{Server=false};
            Check(!upgraded.TryEditTrading("shop",id,"price",1),"client cannot overwrite server trading");ZNet.instance=null;
            Check(Directory.GetFiles(Path.Combine(dir,"backups"),"*.bak").Length>=2,"migration writes backups");
        }finally{Directory.Delete(dir,true);}
    }
}
namespace Jotunn.Extensions{public static class ConfigFileExtensions{
 public static ConfigEntry<T> BindConfig<T>(this ConfigFile f,string section,string key,T value,string description,bool synced=true,AcceptableValueBase acceptableValues=null)=>f.Bind(section,key,value,new ConfigDescription(description,acceptableValues));
}}
