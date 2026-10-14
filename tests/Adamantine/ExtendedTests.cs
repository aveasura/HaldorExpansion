using System;
using System.Collections.Generic;
using HaldorExpansion.Features.Upgrades;
using HaldorExpansion.Features.Awakened;
partial class RefinementTests
{
    static void AllArtifacts() {
        ObjectDB.instance=new ObjectDB();
        var player=new Player {station=new CraftingStation {m_upgrader=true}};
        IdolRegistration.ItemDrop=Make("aveasura_he_idol");
        var rare=AdamantineIdolRegistration.ItemDrop.m_itemData.Clone();rare.m_stack=30;player.inventory.items.Add(rare);
        var ids=(string[])typeof(UpgradeRecipes).GetField("SupportedPrefabs",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).GetValue(null);
        Check(ids.Length==12,"all 12 existing artifacts exercised");
        foreach(string id in ids) {
            var drop=Make(id);ObjectDB.instance.m_items.Add(drop.gameObject);
            ObjectDB.instance.m_recipes.Add(new Recipe {m_item=drop,m_resources=new Piece.Requirement[0]});
            player.inventory.items.Add(drop.m_itemData.Clone());
        }
        UpgradeRecipes.Register();
        var list=new List<Recipe>();UpgradeRecipes.AppendAvailable(player,list);AdamantineRefinement.AppendAvailable(player,list);
        Check(list.Count==24,"12 originals plus 12 safe alternatives");
        foreach(var recipe in list) {
            if(!AdamantineRefinement.IsAlternative(recipe))continue;
            var item=player.inventory.items.Find(x=>x.m_dropPrefab==recipe.m_item.gameObject);item.m_quality=10;
            Check(AdamantineRefinement.TryRefine(player,recipe,item,1f,out var success) && !success && item.m_quality==9 && !AwakenedEquipment.IsAwakened(item),item.m_dropPrefab.name+" loses Q10 eligibility");
            Check(AdamantineRefinement.TryRefine(player,recipe,item,0f,out success) && success && item.m_quality==10 && AwakenedEquipment.IsAwakened(item),item.m_dropPrefab.name+" regains Q10 eligibility");
        }
        Check(rare.m_stack==6,"24 attempts consume exactly 24 idols");
    }
    static void UiCraft() {
        ObjectDB.instance=new ObjectDB();var player=new Player {station=new CraftingStation {m_upgrader=true}};
        var drop=Make("SwordBronze");var source=new Recipe {m_item=drop,m_resources=new[]{new Piece.Requirement {m_resItem=IdolRegistration.Native.drop,m_upgraderResource=true}}};ObjectDB.instance.m_recipes.Add(source);
        var item=drop.m_itemData.Clone();player.inventory.items.Add(item);player.inventory.items.Add(AdamantineIdolRegistration.ItemDrop.m_itemData.Clone());
        var list=new List<Recipe>{source};AdamantineRefinement.AppendAvailable(player,list);var gui=new InventoryGui();UnityEngine.Random.next=0;
        var warning=typeof(AdamantineForgeWarningPatch).GetMethod("Postfix",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
        Check(warning.GetParameters()[0].ParameterType==typeof(InventoryGui) && warning.GetParameters()[0].Name=="__instance","Harmony receives instance, never unboxed private struct as object");
        var label=new TMPro.TMP_Text {text="original"};
        warning.Invoke(null,new object[]{gui,label});Check(label.text=="original","empty selected struct leaves warning unchanged");
        gui.Select(source);warning.Invoke(null,new object[]{gui,label});Check(label.text=="original","ordinary recipe leaves warning unchanged");
        gui.Select(list[1]);warning.Invoke(null,new object[]{gui,label});Check(label.text==Localization.instance.Localize(AdamantineIdolRegistration.ForgeWarningKey),"private selected struct boxed safely and adamantine warning displayed");
        AdamantineForge.Craft(gui,player,list[1],item);
        Check(item.m_quality==2 && gui.Refreshes==1 && HaldorExpansion.HaldorExpansionPlugin.Log.Errors==0,"Forge commit refreshes optional-bool UI method without exception");
        AdamantineForge.Craft(gui,player,list[1],item);
        Check(item.m_quality==2 && gui.Refreshes==2 && HaldorExpansion.HaldorExpansionPlugin.Log.Errors==0,"rejected repeat refreshes without another upgrade or consumption");
    }
    static void Loot() {
        var table=new DropTable();table.m_drops.Add(new DropTable.DropData {m_item=IdolRegistration.Native});
        var original=new List<ItemDrop.ItemData>{IdolRegistration.Native.drop.m_itemData,IdolRegistration.ItemDrop.m_itemData};
        var result=new List<ItemDrop.ItemData>(original);AdamantineIdolRegistration.ChestChance.Value=100f;
        IdolContainerScopePatch.Current=null;AdamantineLoot.AppendItems(table,result);
        Check(result.Count==2,"no adamantine loot outside chest initialization");
        IdolContainerScopePatch.Current=new Container {m_defaultItems=table};AdamantineLoot.AppendItems(new DropTable(),result);
        Check(result.Count==2,"unrelated tables rejected");AdamantineLoot.AppendItems(table,result);
        Check(result.Count==3 && result[0]==original[0] && result[1]==original[1] && result[2].m_stack==1,"all three idols coexist; original loot unchanged");
        AdamantineIdolRegistration.ChestChance.Value=0f;AdamantineLoot.AppendItems(table,result);Check(result.Count==3,"zero disables drops");
        AdamantineIdolRegistration.ChestChance.Value=1f;UnityEngine.Random.next=.009f;result.Clear();AdamantineLoot.AppendItems(table,result);
        Check(result.Count==1,"independent one-percent roll without other idol drops");
        UnityEngine.Random.next=.01f;result.Clear();AdamantineLoot.AppendItems(table,result);Check(result.Count==0,"exact one-percent boundary");
        var rng=new System.Random(20261014);int hits=0;for(int i=0;i<1000000;i++)if(AdamantineLoot.Roll(1f,(float)rng.NextDouble()))hits++;
        Check(hits>9600 && hits<10400,"million-roll deterministic sample: "+hits+" / 1000000");
        Check(!AdamantineLoot.Roll(0f,0f) && AdamantineLoot.Roll(100f,1f) && !AdamantineLoot.Roll(float.NaN,0f),"chance endpoints and invalid input");
    }
}
