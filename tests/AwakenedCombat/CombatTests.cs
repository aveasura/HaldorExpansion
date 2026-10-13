using System;
using HaldorExpansion.Features.Awakened;
using HaldorExpansion.Features.Berserker;
class CombatTests {
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);Console.WriteLine("PASS "+label);}
 static Player Player(Artifact weapon){var p=new Player {hp=50,weapon=new ItemDrop.ItemData {kind=weapon}};p.m_nview.zdo.owner=1;p.m_nview.zdo.m_uid=1;global::Player.m_localPlayer=p;AwakenedCombat.Register(p,p.m_nview);return p;}
 static long Begin(Player p,Character target,HitData hit,bool secondary=false){var a=new Attack {m_character=p,m_weapon=p.weapon};AwakenedCombat.MarkAttack(p,a,secondary);AwakenedCombat.Enter(a);return AwakenedCombat.CreateToken(target,hit);}
 static Character Enemy(float hp=100){var c=new Character {hp=hp};c.m_nview.zdo.owner=2;return c;}
 static void Resolve(Character c,Player p,HitData hit,long token){ZNetView.Sender=2;AwakenedCombat.Resolve(c,p,hit,token,()=>c.RPC_Damage(1,hit));AwakenedCombat.Exit();AwakenedCombat.Update(p);}
 static void Main(){
  var p=Player(Artifact.BoneCrushers);var c=Enemy(10);var hit=new HitData {attacker=p,damage=1000};
  Check(AwakenedCombat.CreateToken(c,hit)==0,"Nova/non-attack cannot mint receipt");
  long token=Begin(p,c,hit);Check(token>0,"exact normal strike eligible");Resolve(c,p,hit,token);Check(Math.Abs(p.hp-50.3f)<.001,"Bone Crushers caps overkill to actual HP loss");
  AwakenedCombat.Resolve(c,p,hit,token,()=>throw new Exception("replay executed"));AwakenedCombat.Update(p);Check(Math.Abs(p.hp-50.3f)<.001,"duplicate token cannot apply damage/heal twice");
  c=Enemy();token=Begin(p,c,hit,true);Check(token==0,"Bone secondary excluded");AwakenedCombat.Exit();
  c=Enemy();token=Begin(p,c,hit);p.weapon=new ItemDrop.ItemData {kind=Artifact.BoneCrushers};Resolve(c,p,hit,token);Check(Math.Abs(p.hp-50.3f)<.001,"source weapon instance unequipped rejects reward");
  p=Player(Artifact.BerserkerAxe);p.inventory.items.Add(new ItemDrop.ItemData {kind=Artifact.BerserkerHelmet});c=Enemy();hit=new HitData {attacker=p,damage=200};token=Begin(p,c,hit,true);Resolve(c,p,hit,token);Check(BerserkerRuntime.Debt==40,"axe alt kill adds exactly forty debt");Check(p.hp==55,"Feast independently heals ten percent missing HP");
  c=Enemy();token=Begin(p,c,hit);ZNetView.Sender=99;AwakenedCombat.Resolve(c,p,hit,token,()=>c.RPC_Damage(1,hit));AwakenedCombat.Exit();AwakenedCombat.Update(p);Check(BerserkerRuntime.Debt==40,"wrong owner cannot award kill");
  c=Enemy();hit.m_ranged=true;token=Begin(p,c,hit);Check(token==0,"ranged meteor excluded inside scope");AwakenedCombat.Exit();hit.m_ranged=false;
  c=Enemy();token=Begin(p,c,hit);ZNetView.Sender=2;
  AwakenedCombat.Resolve(c,p,hit,token,()=>{var outer=AwakenedCombat.Observe(c,hit);var nested=new HitData {attacker=p};c.RPC_Damage(1,nested);AwakenedCombat.Complete(c,outer);});
  AwakenedCombat.Exit();AwakenedCombat.Update(p);Check(BerserkerRuntime.Debt==40,"nested effect kill cannot credit outer strike");
  p=Player(Artifact.BerserkerAxe); p.inventory.items.Add(new ItemDrop.ItemData {kind=Artifact.BerserkerHelmet});
  float debtBefore=BerserkerRuntime.Debt;
  for(int kill=0;kill<2;kill++){c=Enemy();hit=new HitData {attacker=p,damage=200};token=Begin(p,c,hit);Resolve(c,p,hit,token);}
  Check(Math.Abs(p.hp-59.5f)<.001 && BerserkerRuntime.Debt==debtBefore+80,"consecutive kills have no cooldown and heal missing HP");
  p=Player(Artifact.BerserkerAxe);p.weapon.m_quality=9;p.inventory.items.Add(new ItemDrop.ItemData {kind=Artifact.BerserkerHelmet});
  c=Enemy();hit=new HitData {attacker=p,damage=200};debtBefore=BerserkerRuntime.Debt;token=Begin(p,c,hit);Resolve(c,p,hit,token);
  Check(p.hp==55 && BerserkerRuntime.Debt==debtBefore,"helmet works with ordinary axe independently");
  p=Player(Artifact.BerserkerAxe);p.weapon.m_quality=11;BerserkerRuntime.Wrath=0;
  p.inventory.items.Add(new ItemDrop.ItemData {kind=Artifact.BerserkerHelmet});c=Enemy();hit=new HitData {attacker=p,damage=200};
  token=Begin(p,c,hit);Resolve(c,p,hit,token);Check(p.hp==50 && BerserkerRuntime.Debt==debtBefore+40,"Q11 harvest works without Wrath or Feast");BerserkerRuntime.Wrath=1;
  p.m_nview.handlers["HaldorAwakenedDirectReceipt"](2,new ZPackage());Check(true,"malformed receipt contained");
  c=Enemy();AwakenedCombat.Register(c,c.m_nview);c.m_nview.handlers["HaldorAwakenedDirectDamage"](1,new ZPackage());Check(true,"malformed damage packet contained");
  var attack=new Attack {m_character=p,m_weapon=p.weapon};AwakenedCombat.MarkAttack(p,attack,false);AwakenedCombat.Enter(attack);
  c.m_nview.handlers["HaldorAwakenedDirectDamage"]=(sender,packet)=>{c.hp=0;throw new Exception("after damage");};
  Check(AwakenedCombat.TryRoute(c,hit),"dispatch exception after damage must suppress vanilla fallback");AwakenedCombat.Exit();
  c=Enemy();p.weapon.m_quality=9;p.inventory.items.Clear();token=Begin(p,c,hit);Check(token==0,"quality nine has no awakened reward");AwakenedCombat.Exit();
 }
}
