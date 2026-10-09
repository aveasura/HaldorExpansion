using System;
using System.Collections.Generic;
using System.Globalization;
using HaldorExpansion.Configuration;
using HaldorExpansion.Features.Berserker;
using HaldorExpansion.Features.Cestus;
using HaldorExpansion.Features.HelOath;
using HaldorExpansion.Features.NornThread;
using HaldorExpansion.Features.PeltOfHelheim;
using HaldorExpansion.Features.ShadowCrossbow;
using HaldorExpansion.Features.StaminaCape;
using HaldorExpansion.Features.WoundedBeast;
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;

namespace HaldorExpansion.Presentation
{
    internal static class ModLocalization
    {
        internal static void AddLocalizations()
        {
            CustomLocalization loc = LocalizationManager.Instance.GetLocalization();

            loc.AddTranslation("Russian", WithConfiguredValues(new Dictionary<string, string>
            {
                ["$he_tooltip_scroll"] = "Прокрутка: колесо мыши / PgUp, PgDn",
                [HelOathItemRegistration.HelOathItemKey] = ItemNames.HelOathRu,
                [HelOathItemRegistration.HelOathItemDescKey] =
                    "Холод Хельхейма дремлет в его тетиве. Каждая пролитая им кровь приближает исполнение Клятвы — и когда она будет исполнена, откроются Объятия Хель.\n\n" +
                    "<color=#9EDFF2><b>Фенрис — Касание Хель:</b></color> полный сет пробуждает Касание Хель от урона луком. Каждый стак улучшает подвижность персонажа.\n\n" +
                    "<color=#D5F5FF><b>Объятия Хель:</b></color> урон луком заряжает активную способность. " +
                    "При применении следующая стрела взрывается. " +
                    "<b>чем меньше максимум здоровья и выше максимум выносливости, тем сильнее Объятия Хель.</b> Касание V значительно усиливает их.",
                [HelOathItemRegistration.HelOathPassiveNameKey] = "Жертва Хель",
                [HelOathItemRegistration.HelOathPassiveDescKey] =
                    "50% максимального здоровья превращается в максимальную выносливость.",
                ["$he_hel_ability"] = "Объятия Хель",
                ["$he_hel_prepared"] = "Клятва пробуждена",
                ["$se_hel_touch_1"] = "Касание Хель I",
                ["$se_hel_touch_1_desc"] = "+5% к скорости передвижения.\n-4% затрат выносливости на все действия.",
                ["$se_hel_touch_2"] = "Касание Хель II",
                ["$se_hel_touch_2_desc"] = "+10% к скорости передвижения.\n-8% затрат выносливости на все действия.",
                ["$se_hel_touch_3"] = "Касание Хель III",
                ["$se_hel_touch_3_desc"] = "+15% к скорости передвижения.\n-12% затрат выносливости на все действия.",
                ["$se_hel_touch_4"] = "Касание Хель IV",
                ["$se_hel_touch_4_desc"] = "+20% к скорости передвижения.\n-16% затрат выносливости на все действия.",
                ["$se_hel_touch_5"] = "Касание Хель V",
                ["$se_hel_touch_5_desc"] =
                    "+25% к скорости передвижения.\n-20% затрат выносливости на все действия.\nОбъятия Хель значительно усилены.",
                // Берсерк
                [BerserkerIds.BloodDebtKey] = "Кровавый долг",
                [BerserkerIds.RagnarokAbilityKey] = "Рагнарёк",
                [BerserkerIds.RagnarokNoTargetKey] = "Рагнарёк: нет цели",
                [BerserkerIds.RagnarokCastKey] = "Рагнарёк",
                [BerserkerIds.ExhaustionKey] = "Истощение",
                [BerserkerIds.ExhaustionDescriptionKey] =
                    "После Рагнарёка скорость передвижения снижена на 25% на 3 секунды.",

                [BerserkerItemRegistration.ItemKey] = ItemNames.BerserkerGreataxeRu,
                [BerserkerItemRegistration.ItemDescKey] =
                    "Тяжёлая двуручная секира, жаждущая крови. " +
                    "Чем сильнее ранен владелец, тем смертоноснее удары.\n\n" +
                    "<color=#9EDFF2><b>Ярость (I–V)</b></color>: попадания дают +10% скорости атаки " +
                    "и -5% затрат выносливости за стадию. " +
                    "Вне боя Ярость угасает.\n\n" +
                    "<color=#9EDFF2><b>Кровавый долг</b></color>: растёт от полученных ран и ударов секирой. " +
                    "Усиливает <color=#9EDFF2><b>Расплату</b></color> и <color=#e72517><b>Рагнарёк</b></color>. " +
                    "Постепенно угасает вне боя.\n\n" +
                    "<color=#9EDFF2><b>Расплата (колёсико)</b></color>: для активации требует - Ярость V, долг x1+. " +
                    "Поглощает половину долга, усиливает удар " +
                    "и исцеляет на 30% от нанесенного урона.\n\n" +
                    "<color=#e72517><b>Рагнарёк (E)</b></color>: для активации требует - Ярость V, долг x5+, Плащ берсерка. " +
                    "Обращает всю <color=#9EDFF2><b>Ярость</b></color> и <color=#9EDFF2><b>Кровавый долг</b></color> " +
                    "в предвестие конца, перед которым содрогнутся даже боги.",

                [BerserkerIds.WrathKeyPrefix + "1"] = "Ярость I",
                [BerserkerIds.WrathKeyPrefix + "1_desc"] =
                    "+10% к скорости атаки. -5% затрат выносливости на все действия. Продолжайте атаковать, чтобы усиливать Ярость.",
                [BerserkerIds.WrathKeyPrefix + "2"] = "Ярость II",
                [BerserkerIds.WrathKeyPrefix + "2_desc"] =
                    "+20% к скорости атаки. -10% затрат выносливости на все действия. Без новых попаданий Ярость начнёт угасать.",
                [BerserkerIds.WrathKeyPrefix + "3"] = "Ярость III",
                [BerserkerIds.WrathKeyPrefix + "3_desc"] = "+30% к скорости атаки. -15% затрат выносливости на все действия.",
                [BerserkerIds.WrathKeyPrefix + "4"] = "Ярость IV",
                [BerserkerIds.WrathKeyPrefix + "4_desc"] =
                    "+40% к скорости атаки. -20% затрат выносливости на все действия. Ещё немного — и Ярость сорвётся в Бешенство.",
                [BerserkerIds.WrathKeyPrefix + "5"] = "Ярость V",
                [BerserkerIds.WrathKeyPrefix + "5_desc"] =
                    "Бешенство. +50% к скорости атаки. -25% затрат выносливости на все действия. Открывает Расплату; с Плащом берсерка и достаточным Кровавым долгом становится доступен Рагнарёк.",

                // Плащ берсерка
                [BerserkerCloakItemRegistration.ItemKey] = ItemNames.BerserkerCloakRu,
                [BerserkerCloakItemRegistration.ItemDescKey] =
                    "Плащ для тех, кто принимает удар вместо того, чтобы отступить. Чем кровавее становится бой, тем быстрее приближается Рагнарёк.",
                [BerserkerCloakItemRegistration.EffectNameKey] = "Предвестник Рагнарёка",
                [BerserkerCloakItemRegistration.EffectDescKey] =
                    "+30% получаемого урона.\n" +
                    "+50% к накоплению Кровавого долга.\n" +
                    "Пробуждает Рагнарёк.",

                // Шлем берсерка
                [BerserkerHelmetItemRegistration.ItemKey] = ItemNames.BerserkerHelmetRu,
                [BerserkerHelmetItemRegistration.ItemDescKey] =
                    "Тяжёлый шлем, который крепнет вместе с Яростью владельца. Чем яростнее бой, тем труднее сломить его натиск.",
                [BerserkerHelmetItemRegistration.EffectNameKey] = "Несокрушимая ярость",
                [BerserkerHelmetItemRegistration.EffectDescKey] =
                    "Каждая стадия Ярости:\n" +
                    "+4 брони.\n" +
                    "-15% накопления ошеломления.\n" +
                    "-1% получаемого урона.\n" +
                    "+1% восстановления HP атак топором.",

                // Осквернённый Бризингамен
                ["$item_brisingamen"] = ItemNames.BrisingamenRu,
                ["$item_brisingamen_desc"] =
                    "Легендарное золотое ожерелье, чья древняя сила облегчает даже самую тяжёлую ношу.",

                // Нить Норн
                [NornThreadItemRegistration.ItemKey] = ItemNames.NornThreadRu,
                [NornThreadItemRegistration.ItemDescKey] =
                    "Тонкая нить, сплетённая там, где даже смерть ещё не решена. Когда судьба обрывается, она рвётся первой.",
                [NornThreadItemRegistration.EffectNameKey] = "Последняя нить",
                [NornThreadItemRegistration.EffectDescKey] =
                    "Смертельный урон уничтожает амулет вместо владельца. Здоровье становится равным 50% от значения до смертельного удара. После разрыва — 1,5 с защиты от потери здоровья.",
                ["$msg_norn_thread_broken"] = "Нить судьбы оборвалась.",

                // Кираса безмолвной расплаты
                ["$item_chest_delayeddoom"] = ItemNames.SilentReckoningRu,
                ["$item_chest_delayeddoom_desc"] =
                    "Если удар после брони должен снести больше половины максимального здоровья, " +
                    "только часть урона проходит сразу, а остальное возвращается отложенной болью через несколько секунд.\n" +
                    "Она не спасает от расплаты — лишь даёт время нанести ответный удар.",
                ["$se_delayeddoom_regen"] = "Безмолвная живучесть",
                ["$se_delayeddoom_regen_desc"] = "+5 к восстановлению здоровья.",

                // Арбалет теневой охоты
                [ShadowCrossbowItemRegistration.ShadowCrossbowItemKey] = ItemNames.ShadowHuntRu,
                [ShadowCrossbowItemRegistration.ShadowCrossbowItemDescKey] =
                    "Тяжёлый арбалет для тех, кто предпочитает решать бой первым и единственным выстрелом. " +
                    "Особенно беспощаден к целям, которые ещё не поняли, что на них уже открыли охоту.",

                // Костоломы
                [CestusItemRegistration.CestusItemKey] = ItemNames.BoneCrushersRu,
                [CestusItemRegistration.CestusItemDescKey] =
                    "Рождённые в грязи и закалённые кровью, эти кастеты признают лишь один закон: " +
                    "кто выстоял под ударом, тот и забирает победу.\n" +
                    "Ни одна стойка врага не выдержит их бешеного ритма ударов.\n" +
                    "Получая урон, владелец накапливает боевой запал.\n" +
                    "Активное умение: при полном запасе боевого запала нажмите колесико мыши, " +
                    "чтобы выпустить круговой выброс силы и получить краткий защитный покров.",
                [CestusItemRegistration.CestusEffectNameKey] = "Закон арены",
                [CestusItemRegistration.CestusEffectDescKey] =
                    "+10 к максимуму здоровья.\n" +
                    "+90% к скорости атаки.\n" +
                    "-8 брони.",

                // Накидка хозяина ямы
                ["$item_chest_pitking"] = ItemNames.PitKingRu,
                ["$item_chest_pitking_desc"] =
                    "Её не создавали для защиты. " +
                    "Она создана для тех, кто выходит на арену не прятаться за сталью, а ломать кости голыми руками.",
                ["$se_pitking_vigor"] = "Воля хозяина ямы",
                ["$se_pitking_vigor_desc"] = "+60 к максимуму здоровья.",

                ["$itemset_steelheart"] = "Стальное сердце",
                ["$itemset_steelheart_desc"] =
                    "Сет с кастетом: активка кастета получает на 50% больше силы щита и урона ударной волны.",

                // Шкура Хельхейма
                [PeltOfHelheimItemRegistration.ItemKey] = ItemNames.PeltOfHelheimRu,
                [PeltOfHelheimItemRegistration.ItemDescKey] =
                    "Чёрная шкура, напитанная холодом Хельхейма. Она отвечает лишь тому, кто довёл охоту до совершенства.\n\n" +
                    "<color=#9EDFF2><b>Покров Хельхейма:</b></color> если надет полный сет Фенриса, в руках Клятва Хель и активно Касание Хель V, владелец получает <b>очень высокую устойчивость к колющему урону.</b>",
                [PeltOfHelheimItemRegistration.EffectNameKey] = "Покров Хельхейма",
                [PeltOfHelheimItemRegistration.EffectDescKey] =
                    "Касание Хель V завершает связь Фенриса с Хельхеймом.\nОчень высокая устойчивость к колющему урону.",

                // Плащ раненого зверя
                [WoundedBeastItemRegistration.WoundedBeastCapeItemKey] = ItemNames.WoundedBeastRu,
                ["$item_cape_woundedbeast_desc"] =
                    "Пока кровь ещё не остыла, зверь отказывается падать.\n" +
                    "Чем ближе смерть, тем яростнее он цепляется за жизнь — но каждая рана вгрызается глубже.",
                ["$se_cape_woundedbeast"] = "Раненый зверь",
                ["$se_cape_woundedbeast_desc"] =
                    "Получаемый урон +20%.\n" +
                    "Раз в секунду восстанавливает здоровье: чем ниже текущее здоровье, тем сильнее восстановление.\n" +
                    "Огонь, яд и дух подавляют регенерацию на 2 секунды.\n\n" +
                    "При использовании вместе с Кирасой безмолвной расплаты эффект лечения сокращается в 2 раза.",

                // Плащ выжженной стойкости
                [StaminaCapeItemRegistration.ItemKey] = ItemNames.BurnedResolveRu,
                [StaminaCapeItemRegistration.ItemDescKey] =
                    "Часть полученного урона принимает на себя выносливость. " +
                    "Но когда силы иссякают, расплата становится тяжелее.",
                [StaminaCapeItemRegistration.EffectNameKey] = "Выжженная стойкость",
                [StaminaCapeItemRegistration.EffectDescKey] =
                    "25% полученного урона уходит в выносливость, " +
                    "но плата за эту отсрочку — усиленное истощение. " +
                    "Если выносливости не хватает, недостающий урон проходит по здоровью на 75% сильнее.",
            }, true));

            loc.AddTranslation("English", WithConfiguredValues(new Dictionary<string, string>
            {
                ["$he_tooltip_scroll"] = "Scroll: mouse wheel / PgUp, PgDn",
                [HelOathItemRegistration.HelOathItemKey] = ItemNames.HelOathEn,
                [HelOathItemRegistration.HelOathItemDescKey] =
                    "Helheim's cold slumbers in its string. Every drop of blood it spills brings the Oath closer to fulfillment — and when it is fulfilled, Hel's Embrace is revealed.\n\n" +
                    "<color=#9EDFF2><b>Fenris — Hel's Touch:</b></color> wearing the full set awakens Hel's Touch through bow damage. Each stack improves the character's mobility.\n\n" +
                    "<color=#D5F5FF><b>Hel's Embrace:</b></color> bow damage charges the active ability. " +
                    "When activated, the next arrow explodes on impact. " +
                    "<b>the lower the maximum health and the higher the maximum stamina, the stronger Hel's Embrace becomes.</b> Hel's Touch V greatly empowers it.",
                [HelOathItemRegistration.HelOathPassiveNameKey] = "Hel's Sacrifice",
                [HelOathItemRegistration.HelOathPassiveDescKey] =
                    "50% of maximum health is converted into maximum stamina.",
                ["$he_hel_ability"] = "Hel's Embrace",
                ["$he_hel_prepared"] = "Oath Awakened",
                ["$se_hel_touch_1"] = "Hel's Touch I",
                ["$se_hel_touch_1_desc"] = "+5% movement speed.\n-4% stamina cost for all actions.",
                ["$se_hel_touch_2"] = "Hel's Touch II",
                ["$se_hel_touch_2_desc"] = "+10% movement speed.\n-8% stamina cost for all actions.",
                ["$se_hel_touch_3"] = "Hel's Touch III",
                ["$se_hel_touch_3_desc"] = "+15% movement speed.\n-12% stamina cost for all actions.",
                ["$se_hel_touch_4"] = "Hel's Touch IV",
                ["$se_hel_touch_4_desc"] = "+20% movement speed.\n-16% stamina cost for all actions.",
                ["$se_hel_touch_5"] = "Hel's Touch V",
                ["$se_hel_touch_5_desc"] =
                    "+25% movement speed.\n-20% stamina cost for all actions.\nHel's Embrace is greatly empowered.",
                // Berserker
                [BerserkerIds.BloodDebtKey] = "Blood Debt",
                [BerserkerIds.RagnarokAbilityKey] = "Ragnarok",
                [BerserkerIds.RagnarokNoTargetKey] = "Ragnarok: no target",
                [BerserkerIds.RagnarokCastKey] = "Ragnarok",
                [BerserkerIds.ExhaustionKey] = "Exhaustion",
                [BerserkerIds.ExhaustionDescriptionKey] =
                    "After Ragnarok, movement speed is reduced by 25% for 3 seconds.",

                [BerserkerItemRegistration.ItemKey] = ItemNames.BerserkerGreataxeEn,
                [BerserkerItemRegistration.ItemDescKey] =
                    "A mighty two-handed axe that thirsts for blood. " +
                    "The more wounded its wielder, the deadlier its blows.\n\n" +
                    "<color=#9EDFF2><b>Wrath (I–V)</b></color>: hits grant +10% attack speed " +
                    "and -5% stamina cost per stack. " +
                    "Wrath fades outside combat.\n\n" +
                    "<color=#9EDFF2><b>Blood Debt</b></color>: builds from wounds suffered and axe strikes. " +
                    "Empowers <color=#9EDFF2><b>Reckoning</b></color> and <color=#e72517><b>Ragnarök</b></color>. " +
                    "Gradually fades outside combat.\n\n" +
                    "<color=#9EDFF2><b>Reckoning (middle mouse)</b></color>: requires Wrath V and Blood Debt x1+. " +
                    "Consumes half the debt, empowers the strike, " +
                    "and heals for 30% of the damage dealt.\n\n" +
                    "<color=#e72517><b>Ragnarök (E)</b></color>: requires Wrath V, Blood Debt x5+, and the Berserker Cloak. " +
                    "Turns all <color=#9EDFF2><b>Wrath</b></color> and <color=#9EDFF2><b>Blood Debt</b></color> " +
                    "into a harbinger of the end, before which even the gods shall tremble.",

                [BerserkerIds.WrathKeyPrefix + "1"] = "Wrath I",
                [BerserkerIds.WrathKeyPrefix + "1_desc"] =
                    "+10% attack speed. -5% stamina cost for all actions. Keep attacking to drive Wrath higher.",
                [BerserkerIds.WrathKeyPrefix + "2"] = "Wrath II",
                [BerserkerIds.WrathKeyPrefix + "2_desc"] =
                    "+20% attack speed. -10% stamina cost for all actions. Without new hits Wrath will begin to fade.",
                [BerserkerIds.WrathKeyPrefix + "3"] = "Wrath III",
                [BerserkerIds.WrathKeyPrefix + "3_desc"] = "+30% attack speed. -15% stamina cost for all actions.",
                [BerserkerIds.WrathKeyPrefix + "4"] = "Wrath IV",
                [BerserkerIds.WrathKeyPrefix + "4_desc"] =
                    "+40% attack speed. -20% stamina cost for all actions. One step remains before Wrath breaks into Frenzy.",
                [BerserkerIds.WrathKeyPrefix + "5"] = "Wrath V",
                [BerserkerIds.WrathKeyPrefix + "5_desc"] =
                    "Frenzy. +50% attack speed. -25% stamina cost for all actions. Unlocks Reckoning; with the Berserker Cloak and enough Blood Debt, Ragnarok becomes available.",

                // Berserker Cloak
                [BerserkerCloakItemRegistration.ItemKey] = ItemNames.BerserkerCloakEn,
                [BerserkerCloakItemRegistration.ItemDescKey] =
                    "A cloak for those who take the blow instead of yielding. The bloodier the battle becomes, the closer Ragnarok draws.",
                [BerserkerCloakItemRegistration.EffectNameKey] = "Harbinger of Ragnarok",
                [BerserkerCloakItemRegistration.EffectDescKey] =
                    "+30% incoming damage.\n" +
                    "+50% Blood Debt generation.\n" +
                    "Awakens Ragnarok.",

                // Berserker Helmet
                [BerserkerHelmetItemRegistration.ItemKey] = ItemNames.BerserkerHelmetEn,
                [BerserkerHelmetItemRegistration.ItemDescKey] =
                    "A heavy helm that hardens with its wielder's Wrath. The fiercer the battle becomes, the harder the berserker is to break.",
                [BerserkerHelmetItemRegistration.EffectNameKey] = "Unbroken Wrath",
                [BerserkerHelmetItemRegistration.EffectDescKey] =
                    "Each Wrath stage:\n" +
                    "+4 armor.\n" +
                    "-15% incoming stagger buildup.\n" +
                    "-1% incoming damage.\n" +
                    "1% of axe damage dealt restored as HP.",

                // Corrupted Brisingamen
                ["$item_brisingamen"] = ItemNames.BrisingamenEn,
                ["$item_brisingamen_desc"] =
                    "A legendary golden necklace whose ancient power makes even the heaviest burden easier to bear.",

                // Thread of the Norns
                [NornThreadItemRegistration.ItemKey] = ItemNames.NornThreadEn,
                [NornThreadItemRegistration.ItemDescKey] =
                    "A slender thread woven where even death has not yet been decided. When fate breaks, the thread breaks first.",
                [NornThreadItemRegistration.EffectNameKey] = "The Last Thread",
                [NornThreadItemRegistration.EffectDescKey] =
                    "Lethal damage destroys the amulet instead of its wearer. Health becomes 50% of the amount held before the lethal hit. After breaking, health cannot be lost for 1.5 seconds.",
                ["$msg_norn_thread_broken"] = "The thread of fate has snapped.",

                // Cuirass of Silent Reckoning
                ["$item_chest_delayeddoom"] = ItemNames.SilentReckoningEn,
                ["$item_chest_delayeddoom_desc"] =
                    "If a hit would take more than half of your maximum health, " +
                    "only part of the damage is taken immediately, while the rest returns as increased delayed pain over the next few seconds.\n" +
                    "It does not save you from the reckoning — it only gives you time to strike back.",
                ["$se_delayeddoom_regen"] = "Silent Vitality",
                ["$se_delayeddoom_regen_desc"] = "+5 health regeneration.",

                // Crossbow of the Shadow Hunt
                [ShadowCrossbowItemRegistration.ShadowCrossbowItemKey] = ItemNames.ShadowHuntEn,
                [ShadowCrossbowItemRegistration.ShadowCrossbowItemDescKey] =
                    "A heavy crossbow for those who prefer to decide the battle with the first and only shot. " +
                    "Especially ruthless against targets that do not yet realize the hunt has already begun.",

                // Bone Crushers
                [CestusItemRegistration.CestusItemKey] = ItemNames.BoneCrushersEn,
                [CestusItemRegistration.CestusItemDescKey] =
                    "Born in the mud and tempered in blood, these cestus obey only one law: " +
                    "the one who endures the blow claims the victory.\n" +
                    "No enemy stance can withstand their savage rhythm of strikes.\n" +
                    "Taking damage builds battle fervor.\n" +
                    "Active ability: when battle fervor is full, press the middle mouse button " +
                    "to release a circular burst of force and gain a brief protective barrier.",
                [CestusItemRegistration.CestusEffectNameKey] = "Law of the Arena",
                [CestusItemRegistration.CestusEffectDescKey] =
                    "+10 to maximum health.\n" +
                    "+90% attack speed.\n" +
                    "-8 armor.",

                // Pit King's Cuirass
                ["$item_chest_pitking"] = ItemNames.PitKingEn,
                ["$item_chest_pitking_desc"] =
                    "It was not forged for protection. " +
                    "It was made for those who step into the pit not to hide behind steel, but to break bones with bare hands.",
                ["$se_pitking_vigor"] = "Pit King's Vigor",
                ["$se_pitking_vigor_desc"] = "+60 to maximum health.",

                ["$itemset_steelheart"] = "Steel Heart",
                ["$itemset_steelheart_desc"] =
                    "Set with the cestus: the cestus active ability gains 50% more barrier strength and shockwave damage.",

                // Pelt of Helheim
                [PeltOfHelheimItemRegistration.ItemKey] = ItemNames.PeltOfHelheimEn,
                [PeltOfHelheimItemRegistration.ItemDescKey] =
                    "A blackened pelt steeped in Helheim's cold. It answers only to a hunter who has brought the chase to perfection.\n\n" +
                    "<color=#9EDFF2><b>Helheim's Ward:</b></color> while wearing the full Fenris set, wielding Hel's Oath, and maintaining Hel's Touch V, the wearer becomes <b>very resistant to Pierce damage.</b>",
                [PeltOfHelheimItemRegistration.EffectNameKey] = "Helheim's Ward",
                [PeltOfHelheimItemRegistration.EffectDescKey] =
                    "Hel's Touch V completes the bond between Fenris and Helheim.\nVery resistant to Pierce damage.",

                // Cloak of the Wounded Beast
                [WoundedBeastItemRegistration.WoundedBeastCapeItemKey] = ItemNames.WoundedBeastEn,
                ["$item_cape_woundedbeast_desc"] =
                    "While the blood still burns, the beast refuses to fall.\n" +
                    "The closer death comes, the more fiercely it clings to life — but every wound bites deeper.",
                ["$se_cape_woundedbeast"] = "Wounded Beast",
                ["$se_cape_woundedbeast_desc"] =
                    "Incoming damage +20%.\n" +
                    "Restores health once per second: the lower your current health is, the stronger the regeneration becomes.\n" +
                    "Fire, poison, and spirit suppress regeneration for 2 seconds.\n\n" +
                    "When used together with the Cuirass of Silent Reckoning, its healing effect is halved.",

                // Cloak of Burned Resolve
                [StaminaCapeItemRegistration.ItemKey] = ItemNames.BurnedResolveEn,
                [StaminaCapeItemRegistration.ItemDescKey] =
                    "Part of the damage taken is absorbed by stamina. " +
                    "But when strength runs dry, the reckoning grows harsher.",
                [StaminaCapeItemRegistration.EffectNameKey] = "Burned Resolve",
                [StaminaCapeItemRegistration.EffectDescKey] =
                    "25% of damage taken is redirected into stamina, " +
                    "but this delayed pain drains stamina harder. " +
                    "If you do not have enough stamina, the missing damage hits health 75% harder.",
            }, false));
        }

        private static string Number(float value) =>
            value.ToString("0.##", CultureInfo.InvariantCulture);

        private static Dictionary<string, string> WithConfiguredValues(Dictionary<string, string> text, bool ru)
        {
            if (BerserkerConfiguration.HitsPerWrathStage == null) return text;
            float speed = BerserkerConfiguration.WrathAttackSpeedFractionPerStage * 100f;
            float stamina = BerserkerConfiguration.WrathStaminaReductionFractionPerStage * 100f;
            string axe = text[BerserkerItemRegistration.ItemDescKey];
            axe = axe.Replace("+10%", "+" + Number(speed) + "%").Replace("-5%", "-" + Number(stamina) + "%")
                .Replace("30%", Number(BerserkerConfiguration.ReckoningLifeStealFraction * 100f) + "%")
                .Replace("x1+", "x" + Number(BerserkerConfiguration.ReckoningMinimumDebtValue / 100f) + "+")
                .Replace("x5+", "x" + Number(BerserkerConfiguration.RagnarokMinimumDebtValue / 100f) + "+")
                .Replace("Поглощает половину долга",
                    "Поглощает " + Number(BerserkerConfiguration.ReckoningSpendFraction * 100f) + "% долга")
                .Replace("Consumes half the debt",
                    "Consumes " + Number(BerserkerConfiguration.ReckoningSpendFraction * 100f) + "% of the debt");
            if (BerserkerConfiguration.RagnarokButton != null)
                axe = axe.Replace("(E)", "(" + BerserkerConfiguration.RagnarokButton.Config.BoxedValue + ")");
            text[BerserkerItemRegistration.ItemDescKey] = axe;
            for (int stage = 1; stage <= 5; stage++)
            {
                string key = BerserkerIds.WrathKeyPrefix + stage + "_desc";
                text[key] = text[key].Replace("+" + (stage * 10) + "%",
                        "+" + Number(Math.Min(200f, stage * speed)) + "%")
                    .Replace("-" + (stage * 5) + "%", "-" + Number(Math.Min(90f, stage * stamina)) + "%");
            }

            text[BerserkerIds.ExhaustionDescriptionKey] = ru
                ? "После Рагнарёка скорость передвижения снижена на " +
                  Number(BerserkerConfiguration.RagnarokExhaustionMovePenaltyFraction * 100f) + "% на " +
                  Number(BerserkerConfiguration.RagnarokExhaustionDurationValue) + " сек."
                : "After Ragnarok, movement speed is reduced by " +
                  Number(BerserkerConfiguration.RagnarokExhaustionMovePenaltyFraction * 100f) + "% for " +
                  Number(BerserkerConfiguration.RagnarokExhaustionDurationValue) + " seconds.";
            // Jotunn replaces its map but does not overwrite already-loaded native words.
            if (Localization.instance != null && (Localization.instance.GetSelectedLanguage() == "Russian") == ru)
            {
                var addWord = AccessTools.Method(typeof(Localization), "AddWord");
                foreach (var pair in text)
                    addWord.Invoke(Localization.instance, new object[] { pair.Key.TrimStart('$'), pair.Value });
            }

            return text;
        }
    }
}