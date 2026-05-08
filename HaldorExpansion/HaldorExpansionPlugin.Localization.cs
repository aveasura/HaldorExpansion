using System.Collections.Generic;
using Jotunn.Entities;
using Jotunn.Managers;

namespace HaldorExpansion
{
    public partial class HaldorExpansionPlugin
    {
        private void AddLocalizations()
        {
            CustomLocalization loc = LocalizationManager.Instance.GetLocalization();

            loc.AddTranslation("Russian", new Dictionary<string, string>
            {
                // Осквернённый Бризингамен
                ["$item_brisingamen"] = "Осквернённый Бризингамен",
                ["$item_brisingamen_desc"] =
                    "Легендарное золотое ожерелье, чья древняя сила облегчает даже самую тяжёлую ношу.",

                // Кираса безмолвной расплаты
                ["$item_chest_delayeddoom"] = "Кираса безмолвной расплаты",
                ["$item_chest_delayeddoom_desc"] =
                    "Если удар после брони должен снести больше половины максимального здоровья, " +
                    "только часть урона проходит сразу, а остальное возвращается отложенной болью через несколько секунд.\n" +
                    "Она не спасает от расплаты — лишь даёт время нанести ответный удар.",
                ["$se_delayeddoom_regen"] = "Безмолвная живучесть",
                ["$se_delayeddoom_regen_desc"] = "+5 к восстановлению здоровья.",

                // Арбалет теневой охоты
                [ShadowCrossbowItemKey] = "Арбалет теневой охоты",
                [ShadowCrossbowItemDescKey] =
                    "Тяжёлый арбалет для тех, кто предпочитает решать бой первым и единственным выстрелом. " +
                    "Особенно беспощаден к целям, которые ещё не поняли, что на них уже открыли охоту.",

                // Костоломы
                [CestusItemKey] = "Костоломы",
                [CestusItemDescKey] =
                    "Рождённые в грязи и закалённые кровью, эти кастеты признают лишь один закон: " +
                    "кто выстоял под ударом, тот и забирает победу.\n" +
                    "Ни одна стойка врага не выдержит их бешеного ритма ударов.\n" +
                    "Получая урон, владелец накапливает боевой запал.\n" +
                    "Активное умение: при полном запасе боевого запала нажмите колесико мыши, " +
                    "чтобы выпустить круговой выброс силы и получить краткий защитный покров.",
                [CestusEffectNameKey] = "Закон арены",
                [CestusEffectDescKey] =
                    "+30 к максимуму здоровья.\n" +
                    "+90% к скорости атаки.\n" +
                    "-15 брони.",

                // Накидка хозяина ямы
                ["$item_chest_pitking"] = "Накидка хозяина ямы",
                ["$item_chest_pitking_desc"] =
                    "Её не создавали для защиты. " +
                    "Она создана для тех, кто выходит на арену не прятаться за сталью, а ломать кости голыми руками.",
                ["$se_pitking_vigor"] = "Воля хозяина ямы",
                ["$se_pitking_vigor_desc"] = "+70 к максимуму здоровья.",

                ["$itemset_steelheart"] = "Стальное сердце",
                ["$itemset_steelheart_desc"] =
                    "Сет с кастетом: активка кастета получает на 50% больше силы щита и урона ударной волны.",

                // Плащ раненого зверя
                [WoundedBeastCapeItemKey] = "Плащ раненого зверя",
                ["$item_cape_woundedbeast_desc"] =
                    "Пока кровь ещё не остыла, зверь отказывается падать.\n" +
                    "Чем ближе смерть, тем яростнее он цепляется за жизнь — но каждая рана вгрызается глубже.",
                ["$se_cape_woundedbeast"] = "Раненый зверь",
                ["$se_cape_woundedbeast_desc"] =
                    "Получаемый урон +15%.\n" +
                    "Раз в секунду восстанавливает здоровье: чем ниже текущее здоровье, тем сильнее восстановление.\n" +
                    "Огонь, яд и дух подавляют регенерацию на 2 секунды.\n\n" +
                    "При использовании вместе с Кирасой безмолвной расплаты эффект лечения сокращается в 2 раза.",
                
                // Плащ выжженной стойкости
                [StaminaCapeFeature.ItemKey] = "Плащ выжженной стойкости",
                [StaminaCapeFeature.ItemDescKey] =
                    "Часть полученного после брони урона принимает на себя выносливость. " +
                    "Но когда силы иссякают, расплата становится тяжелее.",
                [StaminaCapeFeature.EffectNameKey] = "Выжженная стойкость",
                [StaminaCapeFeature.EffectDescKey] =
                    "15% полученного после брони урона уходит в выносливость, " +
                    "но плата за эту отсрочку — усиленное истощение. " +
                    "Если выносливости не хватает, недостающий урон проходит по здоровью на 30% сильнее.",
            });

            loc.AddTranslation("English", new Dictionary<string, string>
            {
                // Corrupted Brisingamen
                ["$item_brisingamen"] = "Corrupted Brisingamen",
                ["$item_brisingamen_desc"] =
                    "A legendary golden necklace whose ancient power makes even the heaviest burden easier to bear.",

                // Cuirass of Silent Reckoning
                ["$item_chest_delayeddoom"] = "Cuirass of Silent Reckoning",
                ["$item_chest_delayeddoom_desc"] =
                    "If a post-armor hit would take more than half of your maximum health, " +
                    "only part of the damage is taken immediately, while the rest returns as delayed pain over the next few seconds.\n" +
                    "It does not save you from the reckoning — it only gives you time to strike back.",
                ["$se_delayeddoom_regen"] = "Silent Vitality",
                ["$se_delayeddoom_regen_desc"] = "+5 health regeneration.",

                // Crossbow of the Shadow Hunt
                [ShadowCrossbowItemKey] = "Crossbow of the Shadow Hunt",
                [ShadowCrossbowItemDescKey] =
                    "A heavy crossbow for those who prefer to decide the battle with the first and only shot. " +
                    "Especially ruthless against targets that do not yet realize the hunt has already begun.",

                // Bone Crushers
                [CestusItemKey] = "Bone Crushers",
                [CestusItemDescKey] =
                    "Born in the mud and tempered in blood, these cestus obey only one law: " +
                    "the one who endures the blow claims the victory.\n" +
                    "No enemy stance can withstand their savage rhythm of strikes.\n" +
                    "Taking damage builds battle fervor.\n" +
                    "Active ability: when battle fervor is full, press the middle mouse button " +
                    "to release a circular burst of force and gain a brief protective barrier.",
                [CestusEffectNameKey] = "Law of the Arena",
                [CestusEffectDescKey] =
                    "+30 to maximum health.\n" +
                    "+90% attack speed.\n" +
                    "-15 armor.",

                // Pit King's Cuirass
                ["$item_chest_pitking"] = "Pit King's Cuirass",
                ["$item_chest_pitking_desc"] =
                    "It was not forged for protection. " +
                    "It was made for those who step into the pit not to hide behind steel, but to break bones with bare hands.",
                ["$se_pitking_vigor"] = "Pit King's Vigor",
                ["$se_pitking_vigor_desc"] = "+70 to maximum health.",

                ["$itemset_steelheart"] = "Steel Heart",
                ["$itemset_steelheart_desc"] =
                    "Set with the cestus: the cestus active ability gains 50% more barrier strength and shockwave damage.",

                // Cloak of the Wounded Beast
                [WoundedBeastCapeItemKey] = "Cloak of the Wounded Beast",
                ["$item_cape_woundedbeast_desc"] =
                    "While the blood still burns, the beast refuses to fall.\n" +
                    "The closer death comes, the more fiercely it clings to life — but every wound bites deeper.",
                ["$se_cape_woundedbeast"] = "Wounded Beast",
                ["$se_cape_woundedbeast_desc"] =
                    "Incoming damage +15%.\n" +
                    "Restores health once per second: the lower your current health is, the stronger the regeneration becomes.\n" +
                    "Fire, poison, and spirit suppress regeneration for 2 seconds.\n\n" +
                    "When used together with the Cuirass of Silent Reckoning, its healing effect is halved.",
                
                // Cloak of Burned Resolve
                [StaminaCapeFeature.ItemKey] = "Cloak of Burned Resolve",
                [StaminaCapeFeature.ItemDescKey] =
                    "Part of the damage taken after armor is absorbed by stamina. " +
                    "But when strength runs dry, the reckoning grows harsher.",
                [StaminaCapeFeature.EffectNameKey] = "Burned Resolve",
                [StaminaCapeFeature.EffectDescKey] =
                    "15% of damage taken after armor is redirected into stamina, " +
                    "but this delayed pain drains stamina 50% harder. " +
                    "If you do not have enough stamina, the missing damage hits health 30% harder.",
            });
        }
    }
}