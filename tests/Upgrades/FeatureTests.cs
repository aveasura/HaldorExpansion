using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
class FeatureTests
{
    static int failures;
    static Type Type(string n) => typeof(FeatureTests).Assembly.GetType("HaldorExpansion.Features."+n);
    static object Call(string t,string n,params object[] args) => Type(t).GetMethod(n,BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic).Invoke(null,args);
    static void Check(bool ok,string n) { Console.WriteLine((ok?"PASS ":"FAIL ")+n);if(!ok)failures++; }
    static GameObject Item(string name,string key=null) {var go=new GameObject{name=name};go.drop=new ItemDrop{gameObject=go};go.drop.m_itemData.m_dropPrefab=go;go.drop.m_itemData.m_shared.m_name=key??name;return go;}
    static void Main()
    {
        if(Type("Upgrades.IdolRegistration")==null || Type("Upgrades.UpgradeRecipes")==null || Type("Upgrades.IdolLoot")==null || Type("Brisingamen.BrisingamenCarryStatus")==null)
        { Check(false,"idol, recipes, independent loot and level-aware ring are implemented");Environment.Exit(1); }
        ObjectDB.instance=new ObjectDB();var db=ObjectDB.instance;
        var native=Item("NativeIdol");native.drop.m_itemData.m_shared.m_upgradeChance=.61f;native.drop.m_itemData.m_shared.m_breakChance=.9f;
        var nativeGear=Item("NativeArmor");db.m_items.Add(native);db.m_recipes.Add(new Recipe{m_item=nativeGear.drop,m_resources=new[]{null,new Piece.Requirement{m_resItem=native.drop,m_upgraderResource=true}}});
        Check((bool)Call("Upgrades.IdolRegistration","EnsureReady"),"idol registration succeeds from native resource");
        var idol=(GameObject)Type("Upgrades.IdolRegistration").GetField("Prefab",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
        Check(idol.drop.m_itemData.m_shared.m_upgradeChance==.61f && idol.drop.m_itemData.m_shared.m_breakChance==.9f,"native chance fields inherited, not hardcoded");
        var station=new CraftingStation{name="forge"};
        var helmet=Item("aveasura_he_berserker_helmet");var doom=Item("ArmorDelayedDoomChest");var pit=Item("ArmorPitKingChest");var ring=Item("BrisingamenRing","$item_brisingamen");
        foreach(var go in new[]{helmet,doom,pit,ring})db.m_items.Add(go);
        var newNames=new[]{"aveasura_he_berserker_axe","Bone_crushers","BowHelOath","CrossbowSilentHunt",
            "aveasura_he_berserker_cloak","CapePeltOfHelheim","CapeWoundedBeast","CapeFracturedBreath"};
        var newGear=new List<GameObject>();
        foreach(string name in newNames)
        {
            var gear=Item(name,"$test_"+name);newGear.Add(gear);db.m_items.Add(gear);
            var repairOnly=new Recipe{m_item=gear.drop,m_enabled=false,m_craftingStation=new CraftingStation{name="forge"},m_repairStation=new CraftingStation{name="forge"}};
            db.m_recipes.Add(repairOnly);
        }
        var repair=new Recipe{m_item=helmet.drop,m_enabled=false,m_craftingStation=station,m_repairStation=station};db.m_recipes.Add(repair);
        Call("Upgrades.UpgradeRecipes","Register");int count=db.m_recipes.Count;Call("Upgrades.UpgradeRecipes","Register");
        Check(db.m_recipes.Count==count,"recipe registration idempotent");
        Check(repair.m_repairStation==station && !repair.m_enabled && repair.m_noCraftOnlyUpgrade,"repair station retained and ordinary crafting disabled");
        foreach(var gear in newGear)
        {
            var refine=db.GetRecipe(gear.drop.m_itemData);
            Check((bool)Call("Upgrades.UpgradeRecipes","IsSupported",gear.drop.m_itemData),"new gear supported: "+gear.name);
            Check(refine!=null && refine.m_noCraftOnlyUpgrade && !refine.m_enabled &&
                refine.m_repairStation!=null && refine.m_resources.Length==1 &&
                refine.m_resources[0].m_resItem==idol.drop,"upgrade-only recipe preserves repair and requires same idol: "+gear.name);
        }
        Check(repair.m_resources.Length==1 && repair.m_resources[0].m_resItem==idol.drop && !repair.m_resources[0].m_recover,"one nonrecoverable custom idol requirement");
        foreach(int q in new[]{1,2,4,12,100})Check(repair.m_resources[0].GetAmount(q)==1,"one idol at target level "+q);
        var player=new Player();player.inventory.items.Add(helmet.drop.m_itemData.Clone());var available=new List<Recipe>();
        Call("Upgrades.UpgradeRecipes","AppendAvailable",player,available);Check(available.Count==0,"no recipes away from forge");
        player.station=station;Call("Upgrades.UpgradeRecipes","AppendAvailable",player,available);Check(available.Count==0,"ordinary forge cannot refine");
        player.station=new CraftingStation{m_upgrader=true};Call("Upgrades.UpgradeRecipes","AppendAvailable",player,available);Check(available.Contains(repair),"native upgrader lists supported owned gear");
        Check(!(bool)Call("Upgrades.UpgradeRecipes","CanRefine",player,repair,null),"direct craft without existing gear blocked");
        Check((bool)Call("Upgrades.UpgradeRecipes","CanRefine",player,repair,player.inventory.items[0]),"owned upgrade allowed");
        foreach(var gear in newGear)
        {
            var item=gear.drop.m_itemData.Clone();item.m_quality=10;
            player.inventory.items.Add(item);
            var itemRecipe=db.GetRecipe(item);
            Check((bool)Call("Upgrades.UpgradeRecipes","CanRefine",player,itemRecipe,item),"level-10 new equipment can refine: "+gear.name);
        }
        var allAvailable=new List<Recipe>();
        Call("Upgrades.UpgradeRecipes","AppendAvailable",player,allAvailable);
        Check(allAvailable.Count==9,"all four weapons, four cloaks, and helmet listed by upgrader");
        player.station=station;Check(!(bool)Call("Upgrades.UpgradeRecipes","CanRefine",player,repair,player.inventory.items[0]),"direct ordinary upgrade blocked");
        Check(!(bool)Call("Upgrades.UpgradeRecipes","IsSupported",Item("vanilla").drop.m_itemData),"vanilla item excluded");
        var messagePatch=Type("Upgrades.UpgradeOutcomeItemNamePatch").GetMethod("Prefix",BindingFlags.Static|BindingFlags.NonPublic);
        foreach(string languageName in new[]{"Осквернённый Бризингамен","Corrupted Brisingamen"})
        {
            Localization.instance.ringName=languageName;
            foreach(string message in new[]{"$msg_upgrader_success","$msg_upgrader_broke","$msg_upgrader_failed"})
            {
                string[] original={"$item_brisingamen","7"};
                object[] parameters={message,original};
                messagePatch.Invoke(null,parameters);
                string[] localized=(string[])parameters[1];
                Check(localized[0]==languageName&&localized[1]=="7"&&original[0]=="$item_brisingamen",
                    "refinement result name localized with no input mutation: "+message+" "+languageName);
            }
        }
        object[] vanillaMessage={"$msg_upgrader_success",new[]{"$item_vanilla","7"}};
        messagePatch.Invoke(null,vanillaMessage);
        Check(((string[])vanillaMessage[1])[0]=="$item_vanilla","vanilla forge result left unchanged");
        object[] otherMessage={"$msg_craft",new[]{"$item_brisingamen","7"}};
        messagePatch.Invoke(null,otherMessage);
        Check(((string[])otherMessage[1])[0]=="$item_brisingamen","non-forge messages left unchanged");
        foreach(bool real in new[]{false,true})foreach(bool shadow in new[]{false,true})
        {
            var table=new DropTable();table.m_drops.Add(new DropTable.DropData{m_item=native});
            table.itemRolls.Enqueue(real?new List<ItemDrop.ItemData>{native.drop.m_itemData}:new List<ItemDrop.ItemData>());
            table.itemRolls.Enqueue(shadow?new List<ItemDrop.ItemData>{native.drop.m_itemData}:new List<ItemDrop.ItemData>());
            var output=table.GetDropListItems();
            Check(output.Exists(x=>x.m_dropPrefab==native)==real && output.Exists(x=>x.m_dropPrefab==idol)==shadow,"independent outcomes "+real+"/"+shadow);
            Check(table.calls==2,"one shadow sample, no recursion");
        }
        var untouched=new DropTable();untouched.m_drops.Add(new DropTable.DropData{m_item=helmet});untouched.itemRolls.Enqueue(new List<ItemDrop.ItemData>{helmet.drop.m_itemData});untouched.GetDropListItems();Check(untouched.calls==1,"no extra draw for unrelated table");
        var pots=new DropTable();pots.m_drops.Add(new DropTable.DropData{m_item=native});pots.objectRolls.Enqueue(new List<GameObject>{helmet});pots.objectRolls.Enqueue(new List<GameObject>{native,native});var potResult=pots.GetDropList();Check(potResult.Count==2&&potResult[0]==helmet&&potResult[1]==idol,"destructible loot preserved and at most one custom idol");
        // A failed shadow draw may clear native static buffers, but must retain real contents.
        var broken=new DropTable();broken.m_drops.Add(new DropTable.DropData{m_item=native});broken.itemRolls.Enqueue(new List<ItemDrop.ItemData>{helmet.drop.m_itemData});
        var preserved=broken.GetDropListItems();Check(preserved.Count==1&&preserved[0]==helmet.drop.m_itemData,"sample failure retains original item buffer");
        var next=new DropTable();next.m_drops.Add(new DropTable.DropData{m_item=native});next.itemRolls.Enqueue(new List<ItemDrop.ItemData>());next.itemRolls.Enqueue(new List<ItemDrop.ItemData>{native.drop.m_itemData});Check(next.GetDropListItems().Count==1&&next.calls==2,"sampling guard resets after exception");
        var creature=new CharacterDrop();creature.m_drops.Add(new CharacterDrop.Drop{m_prefab=native});creature.m_drops.Add(new CharacterDrop.Drop{m_prefab=helmet});
        var originalDrops=creature.m_drops;var originalCounters=CharacterDrop.s_pseudoCounter;originalCounters.Clear();
        creature.rolls.Enqueue(new List<KeyValuePair<GameObject,int>>{new KeyValuePair<GameObject,int>(native,2)});creature.rolls.Enqueue(new List<KeyValuePair<GameObject,int>>{new KeyValuePair<GameObject,int>(native,3)});
        var creatureResult=creature.GenerateDropList();Check(creatureResult.Count==2&&creatureResult[0].Value==2&&creatureResult[1].Key==idol&&creatureResult[1].Value==1,"creature preserves native stack and awards one independent idol");
        Check(ReferenceEquals(originalCounters,CharacterDrop.s_pseudoCounter)&&originalCounters["probe"].Item2==1&&ReferenceEquals(originalDrops,creature.m_drops),"creature shadow does not consume native counters or replace source list");
        creature.calls=0;creature.throwOnCall=2;creature.rolls.Enqueue(new List<KeyValuePair<GameObject,int>>{new KeyValuePair<GameObject,int>(helmet,1)});creatureResult=creature.GenerateDropList();Check(creatureResult.Count==1&&ReferenceEquals(originalCounters,CharacterDrop.s_pseudoCounter)&&ReferenceEquals(originalDrops,creature.m_drops),"creature exception restores shared state");
        var overflow=Type("Upgrades.IdolContainerOverflowPatch").GetMethod("Postfix",BindingFlags.Static|BindingFlags.NonPublic);
        var scope=Type("Upgrades.IdolContainerScopePatch").GetField("Current",BindingFlags.Static|BindingFlags.NonPublic);
        var container=new Container();scope.SetValue(null,container);ItemDrop.Dropped.Clear();
        Action<Inventory,bool> attempt=(inventory,success)=>overflow.Invoke(null,overflow.GetParameters().Length==2?new object[]{idol.drop.m_itemData,success}:new object[]{inventory,idol.drop.m_itemData,success});
        attempt(new Inventory(),false);Check(ItemDrop.Dropped.Count==0,"failed transfer to unrelated inventory during generation cannot duplicate idol");
        ItemDrop.Dropped.Clear();attempt(container.inventory,false);Check(ItemDrop.Dropped.Count==1,"full generated chest drops awarded idol nearby exactly once");
        ItemDrop.Dropped.Clear();attempt(container.inventory,true);Check(ItemDrop.Dropped.Count==0,"successfully inserted idol is not also dropped");
        scope.SetValue(null,null);attempt(container.inventory,false);Check(ItemDrop.Dropped.Count==0,"failed transfer outside initial generation does not spawn idol");
        var status=(SE_Stats)Activator.CreateInstance(Type("Brisingamen.BrisingamenCarryStatus"),true);
        status.SetLevel(7,0);Check(status.GetTooltipString().Contains("210"),"ring level-7 tooltip");
        var active=(SE_Stats)Activator.CreateInstance(Type("Brisingamen.BrisingamenCarryStatus"),true);var owner=new Player();var worn=ring.drop.m_itemData.Clone();worn.m_quality=2;worn.m_equipped=true;owner.inventory.items.Add(worn);active.Setup(owner);active.SetLevel(0,0);
        float weight=300;active.ModifyMaxCarryWeight(300,ref weight);Check(weight==460,"equip with native level zero reads actual ring level");
        status.SetLevel(20,0);weight=300;active.ModifyMaxCarryWeight(300,ref weight);Check(weight==460,"hovering high-level ring does not contaminate equipped ring");
        worn.m_quality=7;weight=300;active.ModifyMaxCarryWeight(300,ref weight);Check(weight==510&&active.GetTooltipString().Contains("210"),"active ring changes with its own quality");
        var other=(SE_Stats)Activator.CreateInstance(Type("Brisingamen.BrisingamenCarryStatus"),true);var otherOwner=new Player();var otherRing=ring.drop.m_itemData.Clone();otherRing.m_quality=1;otherRing.m_equipped=true;otherOwner.inventory.items.Add(otherRing);other.Setup(otherOwner);
        float otherWeight=300;other.ModifyMaxCarryWeight(300,ref otherWeight);weight=300;active.ModifyMaxCarryWeight(300,ref weight);Check(otherWeight==450&&weight==510,"two owners keep independent ring bonuses");
        worn.m_equipped=false;weight=300;active.ModifyMaxCarryWeight(300,ref weight);Check(weight==300,"removed ring contributes nothing");
        if(Type("Upgrades.UpgradeItemState")==null) Check(false,"loaded armor durability normalization exists");
        else
        {
            pit.drop.m_itemData.m_shared.m_maxDurability=1200;pit.drop.m_itemData.m_shared.m_durabilityPerLevel=200;
            foreach(var sample in new[]{new[]{1f,2100f,1200f},new[]{7f,2000f,2000f},new[]{7f,2500f,2400f},new[]{100f,0f,0f}})
            {
                var saved=pit.drop.m_itemData.Clone();saved.m_shared=new ItemDrop.ItemData.SharedData();saved.m_quality=(int)sample[0];saved.m_durability=sample[1];
                Call("Upgrades.UpgradeItemState","Normalize",saved);
                Check(saved.m_quality==(int)sample[0]&&saved.m_durability==sample[2],"canonical durability migration preserves quality "+sample[0]+" and wear "+sample[1]);
                Call("Upgrades.UpgradeItemState","Normalize",saved);Check(saved.m_durability==sample[2],"durability normalization idempotent");
            }
            foreach(var excluded in new[]{ring,Item("vanilla")}){var saved=excluded.drop.m_itemData.Clone();saved.m_durability=2100;Call("Upgrades.UpgradeItemState","Normalize",saved);Check(saved.m_durability==2100,"durability migration excludes "+excluded.name);}
            foreach(var weapon in newGear.GetRange(0,4))
            {
                weapon.drop.m_itemData.m_shared.m_maxDurability=1200;
                weapon.drop.m_itemData.m_shared.m_durabilityPerLevel=90;
                var stale=weapon.drop.m_itemData.Clone();stale.m_shared=new ItemDrop.ItemData.SharedData{m_name=weapon.drop.m_itemData.m_shared.m_name};
                stale.m_quality=10;stale.m_durability=2000;stale.m_equipped=true;
                var ownerOfWeapon=new Player();ownerOfWeapon.inventory.items.Add(stale);
                Call("Upgrades.UpgradeItemState","RebindWeapons",ownerOfWeapon);
                Check(stale.m_quality==10 && stale.m_durability==2000 && stale.m_equipped &&
                    ReferenceEquals(stale.m_shared,weapon.drop.m_itemData.m_shared),"weapon rebind keeps quality/wear/equipment and canonical shared: "+weapon.name);
                stale.m_durability=3000;
                Call("Upgrades.UpgradeItemState","RebindWeapons",ownerOfWeapon);
                Check(stale.m_durability==2010,"weapon rebind clamps excess durability without repairing: "+weapon.name);
                stale.m_quality=0;stale.m_durability=0;
                Call("Upgrades.UpgradeItemState","RebindWeapons",ownerOfWeapon);
                Check(stale.m_quality==1 && stale.m_durability==0,"weapon rebind normalizes only quality lower bound: "+weapon.name);
            }
        }
        var menu=new ObjectDB{UseHashLookup=true,m_items=new List<GameObject>(db.m_items),m_recipes=db.m_recipes};menu.UpdateRegisters();
        var world=new ObjectDB{UseHashLookup=true,m_items=menu.m_items,m_recipes=menu.m_recipes};
        ObjectDB.instance=world;int beforeRegistration=world.m_items.Count;
        Check(world.GetItemPrefab(idol.name)==null&&world.m_items.Contains(idol),"reproduce shared item list with stale per-DB index");
        Check((bool)Call("Upgrades.IdolRegistration","EnsureReady"),"registration handles shared menu/world lists");
        Check(world.m_items.Count==beforeRegistration&&world.GetItemPrefab(idol.name)==idol,"stale index repaired without duplicate idol insertion");
        bool menuReload=true;try{menu.UpdateRegisters();}catch(ArgumentException){menuReload=false;}
        Check(menuReload,"returning to menu rebuilds native index without duplicate key exception");
        ObjectDB.instance=menu;Call("Upgrades.IdolRegistration","EnsureReady");Check(menu.m_items.Count==beforeRegistration,"repeated menu registration stays idempotent");
        var command=Jotunn.Managers.CommandManager.Instance.commands.Find(x=>x.Name=="he_idoltest");
        Check(command!=null,"in-game chest loot diagnostic registered");
        if(command!=null)
        {
            Check(Jotunn.Managers.CommandManager.Instance.commands.Count==1,"menu transitions do not duplicate diagnostic registration");
            var chest=new GameObject{name="TestIdolChest",container=new Container()};var table=chest.container.m_defaultItems;table.m_drops.Add(new DropTable.DropData{m_item=native});
            foreach(bool real in new[]{false,true})foreach(bool shadow in new[]{false,true}){table.itemRolls.Enqueue(real?new List<ItemDrop.ItemData>{native.drop.m_itemData}:new List<ItemDrop.ItemData>());table.itemRolls.Enqueue(shadow?new List<ItemDrop.ItemData>{native.drop.m_itemData}:new List<ItemDrop.ItemData>());}
            ZNetScene.instance=new ZNetScene();ZNetScene.instance.m_prefabs.Add(chest);var terminal=new Terminal();UnityEngine.Random.state=42;
            command.Run(new[]{"4"},terminal);
            Check(terminal.lines.Exists(x=>x.Contains("none=1 vanilla-only=1 custom-only=1 both=1")),"diagnostic measures all four real patched outcomes");
            Check(UnityEngine.Random.state==42&&ItemDrop.Dropped.Count==0,"diagnostic restores RNG and creates no world drops");
            command.Run(new[]{"-1"},terminal);Check(table.calls==8,"invalid diagnostic count does not sample tables");
        }
        Environment.Exit(failures==0?0:1);
    }
}
