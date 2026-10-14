using System;
using System.Reflection;
using BepInEx.Logging;
using HaldorExpansion.Features.Upgrades;
class HaldorExpansionPlugin
{
    public const string ModGuid="test.haldor.maintenance";
    public static ManualLogSource Log=new ManualLogSource("HaldorExpansion");
}
class MaintenanceTests
{
    static int passed;
    static void Check(bool ok,string name)
    {
        if(!ok)throw new Exception(name);
        passed++;Console.WriteLine("PASS: "+name);
    }
    static void Main(string[] options)
    {
        var type=Assembly.GetExecutingAssembly().GetType("HaldorExpansion.Compatibility.KnownAssetWarningFilter");
        Check(type!=null,"narrow warning filter exists");
        var method=type.GetMethod("ShouldLog",BindingFlags.Static|BindingFlags.NonPublic);
        Func<string,LogLevel,object,bool> log=(source,level,data)=>(bool)method.Invoke(null,new object[]{source,level,data});
        const string sourceName="Jotunn.Managers.AssetManager";
        string[] oldPaths={"Assets/world/Props/stone/Stone1_huge.prefab","Assets/world/Props/Caverocks/caverock_pillar.prefab","Assets/world/Props/Rocks/highstone.prefab"};
        string[] names={"Stone1_huge","caverock_pillar","highstone"};
        for(int i=0;i<names.Length;i++)
        {
            string newPath="Assets/world/Props/DeepNorth/HotSpring/"+names[i]+".prefab";
            string text="Ambiguous asset name for path. old: "+oldPaths[i]+", new: "+newPath+", using old path";
            Check(!log(sourceName,LogLevel.Warning,text),names[i]+" exact warning suppressed");
            Check(log("OtherMod",LogLevel.Warning,text),names[i]+" same message from another source preserved");
            foreach(var level in new[]{LogLevel.Error,LogLevel.Fatal,LogLevel.Info,LogLevel.Debug,LogLevel.Message})
                Check(log(sourceName,level,text),names[i]+" "+level+" preserved");
            Check(log(sourceName,LogLevel.Warning,text+" extra context"),names[i]+" changed warning preserved");
            Check(log(sourceName,LogLevel.Warning,text.Replace("Assets/world/Props/DeepNorth/HotSpring/","Assets/SomeMod/")),names[i]+" other path preserved");
        }
        Check(log(sourceName,LogLevel.Warning,"Ambiguous asset name for path. old: Another.prefab, new: Another.prefab, using old path"),"unknown asset collision preserved");
        Check(log(sourceName,LogLevel.Warning,"Failed to load asset"),"unrelated AssetManager warning preserved");
        Check(log(sourceName,LogLevel.Warning,null)&&log(sourceName,LogLevel.Warning,new object()),"null and non-string logs preserved");
        string[] groups={"BerserkerHelmet","DelayedDoomChest","PitKingChest","BerserkerAxe","BoneCrushers","HelOath","ShadowCrossbow","BerserkerCloak","HelheimPelt","WoundedBeastCloak","ScorchedEnduranceCloak"};
        float[] bases={1200,1200,1200,1200,1000,1800,400,1200,1800,1800,1800};
        float[] increments={200,200,200,120,110,90,30,120,50,60,90};
        for(int i=0;i<groups.Length;i++)
        {
            var group=typeof(ItemUpgradeBalance).GetNestedType(groups[i],BindingFlags.NonPublic);
            var baseField=group.GetField("BaseDurability",BindingFlags.Static|BindingFlags.NonPublic);
            Check(baseField!=null,groups[i]+" has central BaseDurability");
            float b=(float)baseField.GetRawConstantValue();
            float per=(float)group.GetField("DurabilityPerLevel",BindingFlags.Static|BindingFlags.NonPublic).GetRawConstantValue();
            Check(b==bases[i]&&per==increments[i],groups[i]+" original base and increment preserved");
            foreach(int quality in new[]{1,2,10,25})
                Check(b+per*(quality-1)==bases[i]+increments[i]*(quality-1),groups[i]+" quality "+quality+" unchanged");
        }
        if (options.Length > 0 && options[0] == "--integration")
        {
        // Exercise the installed Harmony hook against the real BepInEx logger, not a log stub.
        HaldorExpansionPlugin.Log.LogEvent+=(sender,args)=>Console.WriteLine("INSTALL: "+args.Data);
        type.GetMethod("Install",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        int received=0;
        var actualSource=new ManualLogSource(sourceName);
        actualSource.LogEvent+=(sender,args)=>received++;
        string known="Ambiguous asset name for path. old: "+oldPaths[0]+", new: Assets/world/Props/DeepNorth/HotSpring/"+names[0]+".prefab, using old path";
        actualSource.Log(LogLevel.Warning,known);
        Check(received==0,"installed hook suppresses the real logger event");
        actualSource.Log(LogLevel.Warning,"Unknown warning");
        actualSource.Log(LogLevel.Error,known);
        Check(received==2,"installed hook forwards unrelated warnings and errors");
        var otherSource=new ManualLogSource("OtherMod");
        otherSource.LogEvent+=(sender,args)=>received++;
        otherSource.Log(LogLevel.Warning,known);
        Check(received==3,"installed hook preserves other log sources");
        type.GetMethod("Shutdown",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        actualSource.Log(LogLevel.Warning,known);
        Check(received==4,"shutdown restores original logger behavior");
        actualSource.Dispose();otherSource.Dispose();
        }
        Console.WriteLine(passed+" checks passed");
    }
}
