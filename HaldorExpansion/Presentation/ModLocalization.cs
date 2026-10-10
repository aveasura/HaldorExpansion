using System;
using System.Collections.Generic;
using System.Globalization;
using HaldorExpansion.Configuration;
using HaldorExpansion.Features.Berserker;
using HaldorExpansion.Features.Brisingamen;
using HaldorExpansion.Features.Cestus;
using HaldorExpansion.Features.DelayedDoom;
using HaldorExpansion.Features.HelOath;
using HaldorExpansion.Features.NornThread;
using HaldorExpansion.Features.PeltOfHelheim;
using HaldorExpansion.Features.PitKing;
using HaldorExpansion.Features.ShadowCrossbow;
using HaldorExpansion.Features.StaminaCape;
using HaldorExpansion.Features.SteelHeart;
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
                [UiTextIds.ShopAll] = "Всё",
                [UiTextIds.ShopEquipment] = "Снаряжение",
                [UiTextIds.ShopResources] = "Ресурсы",
                [UiTextIds.ShopOther] = "Прочее",
                [UiTextIds.TooltipScroll] = "Прокрутка: колесо мыши / PgUp, PgDn",
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
                [HelOathIds.AbilityKey] = "Объятия Хель",
                [HelOathIds.PreparedKey] = "Клятва пробуждена",
                [HelOathIds.TouchNameKey(1)] = "Касание Хель I",
                [HelOathIds.TouchDescriptionKey(1)] =
                    "+{touch_speed}% к скорости передвижения.\n-{touch_stamina}% затрат выносливости на все действия.",
                [HelOathIds.TouchNameKey(2)] = "Касание Хель II",
                [HelOathIds.TouchDescriptionKey(2)] =
                    "+{touch_speed}% к скорости передвижения.\n-{touch_stamina}% затрат выносливости на все действия.",
                [HelOathIds.TouchNameKey(3)] = "Касание Хель III",
                [HelOathIds.TouchDescriptionKey(3)] =
                    "+{touch_speed}% к скорости передвижения.\n-{touch_stamina}% затрат выносливости на все действия.",
                [HelOathIds.TouchNameKey(4)] = "Касание Хель IV",
                [HelOathIds.TouchDescriptionKey(4)] =
                    "+{touch_speed}% к скорости передвижения.\n-{touch_stamina}% затрат выносливости на все действия.",
                [HelOathIds.TouchNameKey(5)] = "Касание Хель V",
                [HelOathIds.TouchDescriptionKey(5)] =
                    "+{touch_speed}% к скорости передвижения.\n-{touch_stamina}% затрат выносливости на все действия.\nОбъятия Хель значительно усилены.",
                // Берсерк
                [BerserkerIds.BloodDebtKey] = "Кровавый долг",
                [BerserkerIds.RagnarokAbilityKey] = "Рагнарёк",
                [BerserkerIds.RagnarokNoTargetKey] = "Рагнарёк: нет цели",
                [BerserkerIds.RagnarokCastKey] = "Рагнарёк",
                [BerserkerIds.ExhaustionKey] = "Истощение",
                [BerserkerIds.ExhaustionDescriptionKey] =
                    "После Рагнарёка скорость передвижения снижена на 25% на 3 секунды.",

                [BerserkerIds.AxeItemKey] = ItemNames.BerserkerGreataxeRu,
                [BerserkerIds.AxeDescriptionKey] =
                    "Тяжёлая двуручная секира, жаждущая крови. " +
                    "Чем сильнее ранен владелец, тем смертоноснее удары.\n\n" +
                    "<color=#9EDFF2><b>Ярость (I–V)</b></color>: попадания дают +{wrath_speed}% скорости атаки " +
                    "и -{wrath_stamina}% затрат выносливости за стадию. " +
                    "Вне боя Ярость угасает.\n\n" +
                    "<color=#9EDFF2><b>Кровавый долг</b></color>: растёт от полученных ран и ударов секирой. " +
                    "Усиливает <color=#9EDFF2><b>Расплату</b></color> и <color=#e72517><b>Рагнарёк</b></color>. " +
                    "Постепенно угасает вне боя.\n\n" +
                    "<color=#9EDFF2><b>Расплата (колёсико)</b></color>: для активации требует - Ярость V, долг x{reckoning_debt}+. " +
                    "Поглощает {reckoning_spend}% долга, усиливает удар " +
                    "и исцеляет на {reckoning_heal}% от нанесенного урона.\n\n" +
                    "<color=#e72517><b>Рагнарёк ({ragnarok_key})</b></color>: для активации требует - Ярость V, долг x{ragnarok_debt}+, Плащ берсерка. " +
                    "Обращает всю <color=#9EDFF2><b>Ярость</b></color> и <color=#9EDFF2><b>Кровавый долг</b></color> " +
                    "в предвестие конца, перед которым содрогнутся даже боги.",

                [BerserkerIds.WrathKeyPrefix + "1"] = "Ярость I",
                [BerserkerIds.WrathKeyPrefix + "1_desc"] =
                    "+{stage_speed}% к скорости атаки. -{stage_stamina}% затрат выносливости на все действия. Продолжайте атаковать, чтобы усиливать Ярость.",
                [BerserkerIds.WrathKeyPrefix + "2"] = "Ярость II",
                [BerserkerIds.WrathKeyPrefix + "2_desc"] =
                    "+{stage_speed}% к скорости атаки. -{stage_stamina}% затрат выносливости на все действия. Без новых попаданий Ярость начнёт угасать.",
                [BerserkerIds.WrathKeyPrefix + "3"] = "Ярость III",
                [BerserkerIds.WrathKeyPrefix + "3_desc"] =
                    "+{stage_speed}% к скорости атаки. -{stage_stamina}% затрат выносливости на все действия.",
                [BerserkerIds.WrathKeyPrefix + "4"] = "Ярость IV",
                [BerserkerIds.WrathKeyPrefix + "4_desc"] =
                    "+{stage_speed}% к скорости атаки. -{stage_stamina}% затрат выносливости на все действия. Ещё немного — и Ярость сорвётся в Бешенство.",
                [BerserkerIds.WrathKeyPrefix + "5"] = "Ярость V",
                [BerserkerIds.WrathKeyPrefix + "5_desc"] =
                    "Бешенство. +{stage_speed}% к скорости атаки. -{stage_stamina}% затрат выносливости на все действия. Открывает Расплату; с Плащом берсерка и достаточным Кровавым долгом становится доступен Рагнарёк.",

                // Плащ берсерка
                [BerserkerIds.CloakItemKey] = ItemNames.BerserkerCloakRu,
                [BerserkerIds.CloakDescriptionKey] =
                    "Плащ для тех, кто принимает удар вместо того, чтобы отступить. Чем кровавее становится бой, тем быстрее приближается Рагнарёк.",
                [BerserkerIds.CloakEffectKey] = "Предвестник Рагнарёка",
                [BerserkerIds.CloakEffectDescriptionKey] =
                    "+30% получаемого урона.\n" +
                    "+50% к накоплению Кровавого долга.\n" +
                    "Пробуждает Рагнарёк.",

                // Шлем берсерка
                [BerserkerIds.HelmetItemKey] = ItemNames.BerserkerHelmetRu,
                [BerserkerIds.HelmetDescriptionKey] =
                    "Тяжёлый шлем, который крепнет вместе с Яростью владельца. Чем яростнее бой, тем труднее сломить его натиск.",
                [BerserkerIds.HelmetEffectKey] = "Несокрушимая ярость",
                [BerserkerIds.HelmetEffectDescriptionKey] =
                    "Каждая стадия Ярости:\n" +
                    "+4 брони.\n" +
                    "-15% накопления ошеломления.\n" +
                    "-1% получаемого урона.\n" +
                    "+1% восстановления HP атак топором.",

                // Осквернённый Бризингамен
                [BrisingamenIds.ItemKey] = ItemNames.BrisingamenRu,
                [BrisingamenIds.DescriptionKey] =
                    "Легендарное золотое ожерелье, чья древняя сила облегчает даже самую тяжёлую ношу.",

                // Нить Норн
                [NornThreadItemRegistration.ItemKey] = ItemNames.NornThreadRu,
                [NornThreadItemRegistration.ItemDescKey] =
                    "Тонкая нить, сплетённая там, где даже смерть ещё не решена. Когда судьба обрывается, она рвётся первой.",
                [NornThreadItemRegistration.EffectNameKey] = "Последняя нить",
                [NornThreadItemRegistration.EffectDescKey] =
                    "Смертельный урон уничтожает амулет вместо владельца. Здоровье становится равным 50% от значения до смертельного удара. После разрыва — 1,5 с защиты от потери здоровья.",
                [NornThreadItemRegistration.BrokenMessageKey] = "Нить судьбы оборвалась.",

                // Кираса безмолвной расплаты
                [DelayedDoomIds.ItemKey] = ItemNames.SilentReckoningRu,
                [DelayedDoomIds.DescriptionKey] =
                    "Если удар после брони должен снести больше половины максимального здоровья, " +
                    "только часть урона проходит сразу, а остальное возвращается отложенной болью через несколько секунд.\n" +
                    "Она не спасает от расплаты — лишь даёт время нанести ответный удар.",
                [DelayedDoomIds.DebtNameKey] = "Отложенная расплата",
                [DelayedDoomIds.DebtDescriptionKey] =
                    "Кираса безмолвной расплаты откладывает часть полученного урона. Остаток долга постепенно отнимает здоровье.",
                [DelayedDoomIds.RegenNameKey] = "Безмолвная живучесть",
                [DelayedDoomIds.RegenDescriptionKey] = "+{regen} к восстановлению здоровья.",

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
                [PitKingItemRegistration.PitKingChestItemKey] = ItemNames.PitKingRu,
                [PitKingItemRegistration.PitKingChestItemDescKey] =
                    "Её не создавали для защиты. " +
                    "Она создана для тех, кто выходит на арену не прятаться за сталью, а ломать кости голыми руками.",
                [PitKingItemRegistration.PitKingChestEffectNameKey] = "Воля хозяина ямы",
                [PitKingItemRegistration.PitKingChestEffectDescKey] =
                    "+20 к максимуму здоровья. \nС экипированными Костоломами: +60 вместо +20.",

                [SteelHeartSet.SteelHeartSetNameKey] = "Стальное сердце",
                [SteelHeartSet.SteelHeartSetDescKey] =
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
                [WoundedBeastItemRegistration.WoundedBeastCapeItemDescKey] =
                    "Пока кровь ещё не остыла, зверь отказывается падать.\n" +
                    "Чем ближе смерть, тем яростнее он цепляется за жизнь — но каждая рана вгрызается глубже.",
                [WoundedBeastItemRegistration.WoundedBeastCapeEffectNameKey] = "Раненый зверь",
                [WoundedBeastItemRegistration.WoundedBeastCapeEffectDescKey] =
                    "Получаемый урон +20%.\n" +
                    "Раз в секунду восстанавливает здоровье: чем ниже текущее здоровье, тем сильнее восстановление.\n" +
                    "Пока действуют огонь, яд или дух, регенерация от плаща снижается в 2 раза.\n\n" +
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
                [UiTextIds.ShopAll] = "All",
                [UiTextIds.ShopEquipment] = "Equipment",
                [UiTextIds.ShopResources] = "Resources",
                [UiTextIds.ShopOther] = "Other",
                [UiTextIds.TooltipScroll] = "Scroll: mouse wheel / PgUp, PgDn",
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
                [HelOathIds.AbilityKey] = "Hel's Embrace",
                [HelOathIds.PreparedKey] = "Oath Awakened",
                [HelOathIds.TouchNameKey(1)] = "Hel's Touch I",
                [HelOathIds.TouchDescriptionKey(1)] =
                    "+{touch_speed}% movement speed.\n-{touch_stamina}% stamina cost for all actions.",
                [HelOathIds.TouchNameKey(2)] = "Hel's Touch II",
                [HelOathIds.TouchDescriptionKey(2)] =
                    "+{touch_speed}% movement speed.\n-{touch_stamina}% stamina cost for all actions.",
                [HelOathIds.TouchNameKey(3)] = "Hel's Touch III",
                [HelOathIds.TouchDescriptionKey(3)] =
                    "+{touch_speed}% movement speed.\n-{touch_stamina}% stamina cost for all actions.",
                [HelOathIds.TouchNameKey(4)] = "Hel's Touch IV",
                [HelOathIds.TouchDescriptionKey(4)] =
                    "+{touch_speed}% movement speed.\n-{touch_stamina}% stamina cost for all actions.",
                [HelOathIds.TouchNameKey(5)] = "Hel's Touch V",
                [HelOathIds.TouchDescriptionKey(5)] =
                    "+{touch_speed}% movement speed.\n-{touch_stamina}% stamina cost for all actions.\nHel's Embrace is greatly empowered.",
                // Berserker
                [BerserkerIds.BloodDebtKey] = "Blood Debt",
                [BerserkerIds.RagnarokAbilityKey] = "Ragnarok",
                [BerserkerIds.RagnarokNoTargetKey] = "Ragnarok: no target",
                [BerserkerIds.RagnarokCastKey] = "Ragnarok",
                [BerserkerIds.ExhaustionKey] = "Exhaustion",
                [BerserkerIds.ExhaustionDescriptionKey] =
                    "After Ragnarok, movement speed is reduced by 25% for 3 seconds.",

                [BerserkerIds.AxeItemKey] = ItemNames.BerserkerGreataxeEn,
                [BerserkerIds.AxeDescriptionKey] =
                    "A mighty two-handed axe that thirsts for blood. " +
                    "The more wounded its wielder, the deadlier its blows.\n\n" +
                    "<color=#9EDFF2><b>Wrath (I–V)</b></color>: hits grant +{wrath_speed}% attack speed " +
                    "and -{wrath_stamina}% stamina cost per stack. " +
                    "Wrath fades outside combat.\n\n" +
                    "<color=#9EDFF2><b>Blood Debt</b></color>: builds from wounds suffered and axe strikes. " +
                    "Empowers <color=#9EDFF2><b>Reckoning</b></color> and <color=#e72517><b>Ragnarök</b></color>. " +
                    "Gradually fades outside combat.\n\n" +
                    "<color=#9EDFF2><b>Reckoning (middle mouse)</b></color>: requires Wrath V and Blood Debt x{reckoning_debt}+. " +
                    "Consumes {reckoning_spend}% of the debt, empowers the strike, " +
                    "and heals for {reckoning_heal}% of the damage dealt.\n\n" +
                    "<color=#e72517><b>Ragnarök ({ragnarok_key})</b></color>: requires Wrath V, Blood Debt x{ragnarok_debt}+, and the Berserker Cloak. " +
                    "Turns all <color=#9EDFF2><b>Wrath</b></color> and <color=#9EDFF2><b>Blood Debt</b></color> " +
                    "into a harbinger of the end, before which even the gods shall tremble.",

                [BerserkerIds.WrathKeyPrefix + "1"] = "Wrath I",
                [BerserkerIds.WrathKeyPrefix + "1_desc"] =
                    "+{stage_speed}% attack speed. -{stage_stamina}% stamina cost for all actions. Keep attacking to drive Wrath higher.",
                [BerserkerIds.WrathKeyPrefix + "2"] = "Wrath II",
                [BerserkerIds.WrathKeyPrefix + "2_desc"] =
                    "+{stage_speed}% attack speed. -{stage_stamina}% stamina cost for all actions. Without new hits Wrath will begin to fade.",
                [BerserkerIds.WrathKeyPrefix + "3"] = "Wrath III",
                [BerserkerIds.WrathKeyPrefix + "3_desc"] =
                    "+{stage_speed}% attack speed. -{stage_stamina}% stamina cost for all actions.",
                [BerserkerIds.WrathKeyPrefix + "4"] = "Wrath IV",
                [BerserkerIds.WrathKeyPrefix + "4_desc"] =
                    "+{stage_speed}% attack speed. -{stage_stamina}% stamina cost for all actions. One step remains before Wrath breaks into Frenzy.",
                [BerserkerIds.WrathKeyPrefix + "5"] = "Wrath V",
                [BerserkerIds.WrathKeyPrefix + "5_desc"] =
                    "Frenzy. +{stage_speed}% attack speed. -{stage_stamina}% stamina cost for all actions. Unlocks Reckoning; with the Berserker Cloak and enough Blood Debt, Ragnarok becomes available.",

                // Berserker Cloak
                [BerserkerIds.CloakItemKey] = ItemNames.BerserkerCloakEn,
                [BerserkerIds.CloakDescriptionKey] =
                    "A cloak for those who take the blow instead of yielding. The bloodier the battle becomes, the closer Ragnarok draws.",
                [BerserkerIds.CloakEffectKey] = "Harbinger of Ragnarok",
                [BerserkerIds.CloakEffectDescriptionKey] =
                    "+30% incoming damage.\n" +
                    "+50% Blood Debt generation.\n" +
                    "Awakens Ragnarok.",

                // Berserker Helmet
                [BerserkerIds.HelmetItemKey] = ItemNames.BerserkerHelmetEn,
                [BerserkerIds.HelmetDescriptionKey] =
                    "A heavy helm that hardens with its wielder's Wrath. The fiercer the battle becomes, the harder the berserker is to break.",
                [BerserkerIds.HelmetEffectKey] = "Unbroken Wrath",
                [BerserkerIds.HelmetEffectDescriptionKey] =
                    "Each Wrath stage:\n" +
                    "+4 armor.\n" +
                    "-15% incoming stagger buildup.\n" +
                    "-1% incoming damage.\n" +
                    "1% of axe damage dealt restored as HP.",

                // Corrupted Brisingamen
                [BrisingamenIds.ItemKey] = ItemNames.BrisingamenEn,
                [BrisingamenIds.DescriptionKey] =
                    "A legendary golden necklace whose ancient power makes even the heaviest burden easier to bear.",

                // Thread of the Norns
                [NornThreadItemRegistration.ItemKey] = ItemNames.NornThreadEn,
                [NornThreadItemRegistration.ItemDescKey] =
                    "A slender thread woven where even death has not yet been decided. When fate breaks, the thread breaks first.",
                [NornThreadItemRegistration.EffectNameKey] = "The Last Thread",
                [NornThreadItemRegistration.EffectDescKey] =
                    "Lethal damage destroys the amulet instead of its wearer. Health becomes 50% of the amount held before the lethal hit. After breaking, health cannot be lost for 1.5 seconds.",
                [NornThreadItemRegistration.BrokenMessageKey] = "The thread of fate has snapped.",

                // Cuirass of Silent Reckoning
                [DelayedDoomIds.ItemKey] = ItemNames.SilentReckoningEn,
                [DelayedDoomIds.DescriptionKey] =
                    "If a hit would take more than half of your maximum health, " +
                    "only part of the damage is taken immediately, while the rest returns as increased delayed pain over the next few seconds.\n" +
                    "It does not save you from the reckoning — it only gives you time to strike back.",
                [DelayedDoomIds.DebtNameKey] = "Delayed Reckoning",
                [DelayedDoomIds.DebtDescriptionKey] =
                    "The Cuirass of Silent Reckoning has delayed part of the damage taken. The remaining debt gradually drains health.",
                [DelayedDoomIds.RegenNameKey] = "Silent Vitality",
                [DelayedDoomIds.RegenDescriptionKey] = "+{regen} health regeneration.",

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
                [PitKingItemRegistration.PitKingChestItemKey] = ItemNames.PitKingEn,
                [PitKingItemRegistration.PitKingChestItemDescKey] =
                    "It was not forged for protection. " +
                    "It was made for those who step into the pit not to hide behind steel, but to break bones with bare hands.",
                [PitKingItemRegistration.PitKingChestEffectNameKey] = "Pit King's Vigor",
                [PitKingItemRegistration.PitKingChestEffectDescKey] =
                    "+20 maximum health. \nWith Bone Crushers equipped: +60 instead of +20.",

                [SteelHeartSet.SteelHeartSetNameKey] = "Steel Heart",
                [SteelHeartSet.SteelHeartSetDescKey] =
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
                [WoundedBeastItemRegistration.WoundedBeastCapeItemDescKey] =
                    "While the blood still burns, the beast refuses to fall.\n" +
                    "The closer death comes, the more fiercely it clings to life — but every wound bites deeper.",
                [WoundedBeastItemRegistration.WoundedBeastCapeEffectNameKey] = "Wounded Beast",
                [WoundedBeastItemRegistration.WoundedBeastCapeEffectDescKey] =
                    "Incoming damage +20%.\n" +
                    "Restores health once per second: the lower your current health is, the stronger the regeneration becomes.\n" +
                    "While burning, poisoned, or affected by spirit damage, the cape regeneration is halved.\n\n" +
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
            string axe = text[BerserkerIds.AxeDescriptionKey];
            axe = axe.Replace("{wrath_speed}", Number(speed)).Replace("{wrath_stamina}", Number(stamina))
                .Replace("{reckoning_heal}", Number(BerserkerConfiguration.ReckoningLifeStealFraction * 100f))
                .Replace("{reckoning_debt}", Number(BerserkerConfiguration.ReckoningMinimumDebtValue / 100f))
                .Replace("{ragnarok_debt}", Number(BerserkerConfiguration.RagnarokMinimumDebtValue / 100f))
                .Replace("{reckoning_spend}", Number(BerserkerConfiguration.ReckoningSpendFraction * 100f))
                .Replace("{ragnarok_key}", BerserkerConfiguration.RagnarokButton != null
                    ? BerserkerConfiguration.RagnarokButton.Config.BoxedValue.ToString()
                    : "E");
            text[BerserkerIds.AxeDescriptionKey] = axe;
            for (int stage = 1; stage <= 5; stage++)
            {
                string key = BerserkerIds.WrathKeyPrefix + stage + "_desc";
                text[key] = text[key].Replace("{stage_speed}", Number(Math.Min(200f, stage * speed)))
                    .Replace("{stage_stamina}", Number(Math.Min(90f, stage * stamina)));
            }

            for (int stage = 1; stage <= HelOathState.MaxTouchStacks; stage++)
            {
                string key = HelOathIds.TouchDescriptionKey(stage);
                text[key] = text[key].Replace("{touch_speed}", Number(stage * HelOathState.TouchSpeedPerStack * 100f))
                    .Replace("{touch_stamina}", Number(stage * HelOathState.TouchStaminaReductionPerStack * 100f));
            }

            text[DelayedDoomIds.RegenDescriptionKey] = text[DelayedDoomIds.RegenDescriptionKey]
                .Replace("{regen}", Number(DelayedDoomItemRegistration.DelayedDoomChestFoodRegenBonus));

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