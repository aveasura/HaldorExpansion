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
                // Lore-only previews at Haldor (inventory descriptions remain separate).
                [HelOathItemRegistration.HelOathItemKey + "_lore"] =
                    "<color=#C9A56B><b>I. Цена бессмертной охоты</b></color>" +
                    "\n<color=#B1ABA5><i>«Мёртвым не нужна жизнь. Но живым порой не хватает смерти.»</i></color>" +
                    "\n\nВ древних сказаниях говорится об охотнике, который однажды отправился туда, где кончаются тропы смертных." +
                    "\n\nОн преследовал добычу, которую не могли настичь ни люди, ни звери. Ночь за ночью он шёл по её следу, пока леса не сменились мёртвыми равнинами, а снег под ногами не стал чёрным." +
                    "\n\nКогда впереди показалась замёрзшая река, путь ему преградила сама Хель." +
                    "\n\n— Ты зашёл слишком далеко, смертный. Здесь обрывается след любой добычи." +
                    "\n\nНо охотник не склонил головы." +
                    "\n\n— Тогда дай мне силу продолжить охоту." +
                    "\n\nБогиня долго смотрела на него. Затем протянула ладонь." +
                    "\n\n— Половину твоей жизни. Ни мгновением меньше." +
                    "\n\nОхотник не попросил времени подумать." +
                    "\n\nОн отдал ей то, за что смертные цепляются до последнего, и почувствовал, как холод сковал его сердце. Кровь замедлилась в жилах, дыхание стало едва различимым, но тело наполнилось силой, неведомой живым." +
                    "\n\nХель вложила ему в руки лук, тетива которого была холоднее могильного камня." +
                    "\n\n— Ступай. Но помни: теперь каждый твой шаг принадлежит мне." +
                    "\n\nГоворят, с той ночи охотник двигался быстрее северного ветра. Ни усталость, ни холод не могли остановить его, а каждая стрела несла в себе отголосок царства мёртвых." +
                    "\n\nИ когда сила Хель окончательно пробуждалась в его руках, даже небеса содрогались от последнего выстрела." +
                    "\n\n<color=#B1ABA5><i>«Я отдал ей половину жизни. Оставшейся хватит, чтобы закончить охоту.»</i></color>",
                [NornThreadItemRegistration.ItemKey + "_lore"] =
                    "<color=#C9A56B><b>III. Последний дар</b></color>" +
                    "\n<color=#B1ABA5><i>«Судьбу нельзя обмануть дважды. Но порой достаточно одного раза.»</i></color>" +
                    "\n\nСтарая сага повествует о воине, которому Норны открыли день его гибели. Страшась предначертанного, он посвятил долгие годы поискам способа избежать смерти." +
                    "\n\nНи железная кираса, откладывавшая смертельные раны, ни плащ, обращавший боль в истощение, не смогли даровать ему настоящего спасения." +
                    "\n\nИ тогда он решился обратиться к тем, кто плетёт саму судьбу." +
                    "\n\nНикто не знает, как воин отыскал жилище Норн. Одни говорили, что он спустился к корням Иггдрасиля. Другие — что сами богини позволили ему приблизиться, желая увидеть, как далеко зайдёт смертный в своём упрямстве." +
                    "\n\nВернулся он лишь спустя три зимы." +
                    "\n\nНа его груди висел маленький амулет, внутри которого мерцала тончайшая серебряная нить. Говорили, что она была вырвана из самого полотна судьбы и могла однажды сохранить жизнь тому, чей час уже настал." +
                    "\n\nВоин больше не искал новых способов спасения." +
                    "\n\nОн знал, что теперь у него есть последний шанс." +
                    "\n\nНо в ночь, когда сбылось предсказание Норн, произошло то, чего не предвидели даже старые сказители." +
                    "\n\nГоворят, у ворот разрушенной крепости стоял израненный воин. Его кираса была рассечена, плащ истлел до чёрных лоскутьев, а ноги едва удерживали тело." +
                    "\n\nНа груди всё ещё мерцала Нить Норн." +
                    "\n\nНеподалёку кто-то плакал." +
                    "\n\nНикто не знает, кого он увидел среди руин. Но один из уцелевших позднее клялся, что видел, как воин снял амулет со своей шеи и вложил его в чужую дрожащую ладонь." +
                    "\n\nПосле этого он вновь поднял оружие и вышел навстречу врагам." +
                    "\n\nКогда наступило утро, его нашли среди павших. Кираса была пробита, от плаща остался лишь пепел." +
                    "\n\n<b>Но Нити Норн при нём не было.</b>" +
                    "\n\nОдни говорили, что богини забрали её обратно. Другие утверждали, будто где-то ещё живёт человек, которому воин подарил судьбу, предназначенную самому себе." +
                    "\n\nПравды никто так и не узнал." +
                    "\n\nЛишь на внутренней стороне кирасы сохранились слова, которых прежде никто не замечал:" +
                    "\n\n<color=#B1ABA5><i>«Я бросал вызов смерти ради собственной жизни. Но лишь в конце понял, ради чьей стоило сражаться.»</i></color>",
                [DelayedDoomIds.ItemKey + "_lore"] =
                    "<color=#C9A56B><b>I. Время взаймы</b></color>" +
                    "\n<color=#B1ABA5><i>«Смерть не забыла о тебе. Она лишь согласилась подождать.»</i></color>" +
                    "\n\nГоворят, однажды три Норны открыли воину день его гибели. Они поведали, где он падёт, от чьего клинка и сколько ударов успеет нанести прежде, чем оборвётся его нить." +
                    "\n\nНо воин не пожелал покориться предначертанному." +
                    "\n\nМного зим он провёл у кузнечного горна, пытаясь создать доспех, способный обмануть саму смерть. Семь раз железо трескалось под молотом, и семь раз он начинал заново." +
                    "\n\nНа восьмой ему удалось выковать кирасу, которая принимала сокрушительные удары, оставляя владельцу драгоценные мгновения жизни." +
                    "\n\nВоин возликовал. Он решил, что наконец одержал победу над судьбой." +
                    "\n\nНо вскоре обнаружил страшную истину." +
                    "\n\nРаны, которых он избежал в бою, возвращались позднее. Боль настигала его среди ночи, кровь проступала сквозь старые повязки, а сердце содрогалось от ударов, нанесённых много часов назад." +
                    "\n\nОн не победил смерть." +
                    "\n\n<b>Он лишь научился брать у неё время взаймы.</b>" +
                    "\n\nИ всё же воин продолжил свои поиски. Ведь пока оставалось хоть одно мгновение, предсказанный конец ещё можно было изменить." +
                    "\n\n<color=#B1ABA5><i>«У всякого долга есть срок. Даже если должник забыл, кому обязан.»</i></color>",
                [CestusItemRegistration.CestusItemKey + "_lore"] =
                    "<color=#C9A56B><b>I. Смех сквозь кровь</b></color>" +
                    "\n<color=#B1ABA5><i>«Бей сильнее. А то я подумаю, что ты меня жалеешь.»</i></color>" +
                    "\n\nВ старых сказаниях говорится о бойце, который выходил на арену без меча, без щита и без страха. На его кулаках были лишь тяжёлые железные Костоломы, испещрённые следами бесчисленных поединков." +
                    "\n\nПротив него выставляли лучших воинов. Они ломали ему рёбра, рассекали кожу и заставляли падать на залитый кровью песок." +
                    "\n\nНо всякий раз, когда толпа решала, что бой окончен, из ямы доносился смех." +
                    "\n\nОн поднимался, сплёвывал кровь и жестом подзывал противника ближе." +
                    "\n\nГоворят, Костоломы помнили каждый удар, нанесённый своему хозяину. И чем больше боли он принимал, тем страшнее становилась сила, дремавшая в его кулаках." +
                    "\n\nКогда же терпение воина истощалось, он обрушивал всю накопленную ярость одним сокрушительным ударом. Дрожали стены, трескался камень, а зрители забывали, за кого только что болели." +
                    "\n\nГоворили, он научился этому у зверя, которого однажды победил. Другие утверждали, что зверь просто встретил на арене себе подобного." +
                    "\n\n<color=#B1ABA5><i>«Запомни, сколько раз ты меня ударил. Я верну всё одним.»</i></color>",
                [PeltOfHelheimItemRegistration.ItemKey + "_lore"] =
                    "<color=#C9A56B><b>III. Покров мёртвого волка</b></color>" +
                    "\n<color=#B1ABA5><i>«Даже в царстве мёртвых есть создания, которые боятся темноты.»</i></color>" +
                    "\n\nСказывают, что на границе Хельхейма обитал огромный чёрный волк, чья шерсть не отражала света, а следы никогда не исчезали с мёрзлой земли." +
                    "\n\nНи один мертвец не осмеливался приблизиться к нему. Ни один живой не переживал встречи." +
                    "\n\nНо однажды волк почуял запах чужой крови." +
                    "\n\nПо безмолвным равнинам шёл охотник. Его сердце билось лишь вполсилы, а в руках покоился лук, отмеченный самой Хель." +
                    "\n\nЗверь преградил ему дорогу." +
                    "\n\nОхотник остановился." +
                    "\n\nДолгое время они смотрели друг другу в глаза. Один был рождён во тьме. Другой добровольно отдал ей половину своей жизни." +
                    "\n\nЗатем волк бросился." +
                    "\n\nГоворят, их схватка продолжалась до тех пор, пока мёртвые звёзды не исчезли с небосвода. Зверь разрывал землю когтями, а охотник уходил от каждого броска, позволяя холоду Хель всё глубже проникать в его тело." +
                    "\n\nКогда последнее Касание богини легло на его плечи, он наконец натянул тетиву." +
                    "\n\nОдна стрела." +
                    "\n\nОдна вспышка ледяного света." +
                    "\n\nИ тишина." +
                    "\n\nИз шкуры павшего зверя охотник изготовил плащ. Говорят, чёрный мех сохранил волю своего прежнего хозяина: лишь тому, кого Хель признала достойным, он позволял пройти сквозь град стрел, словно сквозь зимний снег." +
                    "\n\nНо стоило благословению богини ослабнуть, как древняя защита угасала вместе с ним." +
                    "\n\n<color=#B1ABA5><i>«Мёртвые не боятся стрел. Теперь и я знаю почему.»</i></color>",
                [StaminaCapeItemRegistration.ItemKey + "_lore"] =
                    "<color=#C9A56B><b>II. Цена сопротивления</b></color>" +
                    "\n<color=#B1ABA5><i>«Железо может выдержать тысячу ударов. Но сколько выдержит человек под ним?»</i></color>" +
                    "\n\nСтарые сказания повествуют о воине, которому Норны предрекли смерть. Не желая мириться с судьбой, он выковал кирасу, способную отсрочить страшные раны." +
                    "\n\nНо всякое мгновение, взятое у смерти взаймы, однажды приходилось возвращать." +
                    "\n\nПрошли годы. Воин пережил множество битв, и каждая оставляла на нём след. Кираса даровала время, но не могла избавить его от расплаты." +
                    "\n\nТогда он обратился к другому способу." +
                    "\n\nВ землях, где погребальные костры не угасали даже под зимними снегами, воин нашёл древнее полотно, пропитанное пеплом павших. Из него он изготовил плащ, способный обращать часть боли в истощение." +
                    "\n\nВпервые за долгие годы он почувствовал надежду." +
                    "\n\nКлинки рассекали воздух, копья пробивали защиту, но вместо глубоких ран его тело охватывала тяжесть невыносимой усталости." +
                    "\n\nПока оставались силы, он продолжал сражаться." +
                    "\n\nНо однажды, окружённый врагами, воин сделал шаг, на который уже не хватило сил." +
                    "\n\nПлащ почернел, словно его охватило невидимое пламя. Боль обрушилась на владельца с такой яростью, будто каждая предотвращённая рана вернулась во много раз страшнее." +
                    "\n\nКогда битва закончилась, его нашли лежащим среди пепла. Он ещё дышал, но не мог даже поднять руки." +
                    "\n\nВпервые воин задумался, не напрасно ли потратил столько лет, пытаясь избежать неизбежного." +
                    "\n\nОднако страх смерти оказался сильнее сомнений." +
                    "\n\nИ тогда он решил искать спасения там, куда прежде не осмеливался обратиться ни один смертный." +
                    "\n\n<b>У тех, кто плетёт саму судьбу.</b>" +
                    "\n\n<color=#B1ABA5><i>«Он сжёг собственные силы, лишь бы не упасть. Но даже пепел однажды остывает.»</i></color>",
                [UiTextIds.ShopAll] = "Всё",
                [UiTextIds.ShopEquipment] = "Снаряжение",
                [UiTextIds.ShopResources] = "Ресурсы",
                [UiTextIds.ShopOther] = "Прочее",
                [UiTextIds.TooltipScroll] = "Прокрутка: колесо мыши / PgUp, PgDn",
                [HelOathItemRegistration.HelOathItemKey] = ItemNames.HelOathRu,
                [HelOathItemRegistration.HelOathItemDescKey] =
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
                [BerserkerIds.AxeItemKey + "_lore"] =
                    "<color=#C9A56B><b>I. Жажда крови</b></color>\n" +
                    "<color=#B1ABA5><i>«Не всякая кровь, пролитая в битве, принадлежит побеждённому.»</i></color>\n\n" +
                    "Говорят, эту секиру однажды поднял воин, которого уже сочли мёртвым. " +
                    "Израненный и окружённый врагами, он сражался до тех пор, пока некому стало поднять оружие против него.\n\n" +
                    "С тех пор железо помнит вкус той битвы. Чем ближе его хозяин к смерти, тем сильнее удары. " +
                    "Каждая схватка разжигает Ярость, а пролитая кровь превращается в долг, который однажды придётся взыскать.\n\n" +
                    "<color=#B1ABA5><i>«Пока сердце бьётся, расплата не окончена.»</i></color>",
                [BerserkerIds.AxeDescriptionKey] =
                    "<color=#C9A56B><b>Сила реликвии</b></color>\n" +
                    "<color=#9EDFF2><b>Кровавая жажда</b></color>: +0,2% урона за каждый недостающий HP владельца.\n\n" +
                    "<color=#9EDFF2><b>Ярость (I–V)</b></color>: попадания секирой накапливают Ярость. " +
                    "Каждая стадия даёт +{wrath_speed}% скорости атаки и -{wrath_stamina}% затрат выносливости. " +
                    "Без новых попаданий Ярость постепенно угасает.\n\n" +
                    "<color=#9EDFF2><b>Кровавый долг</b></color>: накапливается от ран, нанесённых врагами, и попаданий секирой. " +
                    "Усиливает Расплату и Рагнарёк; вне боя постепенно угасает.\n\n" +
                    "<color=#9EDFF2><b>Расплата (колёсико)</b></color>: при Ярости V и долге x{reckoning_debt}+ " +
                    "альтернативный удар становится Расплатой. При попадании расходует {reckoning_spend}% долга, " +
                    "наносит дополнительный урон и исцеляет на {reckoning_heal}% от прямого урона цели.\n\n" +
                    "<color=#e72517><b>Рагнарёк ({ragnarok_key})</b></color>: требует Ярость V, долг x{ragnarok_debt}+ " +
                    "и надетый Плащ берсерка. Обрушивает на врагов метеоры, расходуя всю Ярость и Кровавый долг. " +
                    "После призыва наступает Истощение.",

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
                [BerserkerIds.CloakItemKey + "_lore"] =
                    "<color=#C9A56B><b>III. Предвестник Рагнарёка</b></color>\n" +
                    "<color=#B1ABA5><i>«Когда земля пресытится кровью, небеса продолжат бой.»</i></color>\n\n" +
                    "Этот истлевший плащ не укрывает от клинков. Каждый удар проникает глубже, " +
                    "каждая рана тяжелее прежней. И всё же его носитель не отступает.\n\n" +
                    "Древняя сага гласит, что в своей последней битве берсерк воззвал к самому концу времён. " +
                    "Небо почернело, и на землю обрушился дождь пылающих камней.\n\n" +
                    "Когда пламя угасло, не осталось ни врагов, ни победителя. " +
                    "Лишь изорванный плащ лежал среди пепла.\n\n" +
                    "<color=#B1ABA5><i>«Рагнарёк не различает победителей и павших.»</i></color>",
                [BerserkerIds.CloakEffectKey] = "Предвестник Рагнарёка",
                [BerserkerIds.CloakEffectDescriptionKey] =
                    "+30% получаемого урона.\n" +
                    "+50% к накоплению Кровавого долга.\n" +
                    "Рагнарёк: требуется секира берсерка, Ярость V и достаточный Кровавый долг. " +
                    "Призыв расходует всю Ярость и долг, затем наступает Истощение.",

                // Шлем берсерка
                [BerserkerIds.HelmetItemKey] = ItemNames.BerserkerHelmetRu,
                [BerserkerIds.HelmetItemKey + "_lore"] =
                    "<color=#C9A56B><b>II. Несломленная воля</b></color>\n" +
                    "<color=#B1ABA5><i>«Страх покидает человека задолго до того, как его покидает жизнь.»</i></color>\n\n" +
                    "Никто не знает, сколько ударов выдержал этот шлем. Его железо иссечено клинками, " +
                    "а под забралом навсегда остались следы чужой крови.\n\n" +
                    "Говорят, в последней битве его владелец перестал чувствовать боль. " +
                    "Удары врагов больше не могли сломить его, а каждый взмах секиры словно возвращал силы умирающему телу.\n\n" +
                    "Но стоило Ярости угаснуть, как все старые раны вновь напоминали о себе.\n\n" +
                    "<color=#B1ABA5><i>«Тот, кто забыл о боли, ещё не победил смерть.»</i></color>",
                [BerserkerIds.HelmetEffectKey] = "Несокрушимая ярость",
                [BerserkerIds.HelmetEffectDescriptionKey] =
                    "Каждая стадия Ярости:\n" +
                    "+4 брони.\n" +
                    "-15% накопления ошеломления.\n" +
                    "-1% получаемого урона.\n" +
                    "+1% лечения от фактически отнятого HP прямыми ударами секиры по врагам " +
                    "(до 5% на Ярости V).",

                // Осквернённый Бризингамен
                [BrisingamenIds.ItemKey] = ItemNames.BrisingamenRu,
                [BrisingamenIds.DescriptionKey] =
                    "<color=#C9A56B><b>Бремя золота</b></color>" +
                    "\n<color=#B1ABA5><i>«Боги не знают тяжести золота. Лишь смертные сгибают под ним спины.»</i></color>" +
                    "\n\nГоворят, жил некогда конунг, чьи сокровищницы были полны золота, а сердце оставалось пустым. Ни завоёванные земли, ни богатства павших врагов не могли утолить его жажду обладания." +
                    "\n\nОднажды он услышал сказание о Бризингамене — дивном ожерелье богини Фрейи, чья красота затмевала сияние самого солнца." +
                    "\n\nКонунг поклялся, что сокровище будет принадлежать ему." +
                    "\n\nМного зим его люди искали путь к божественной реликвии. И когда наконец принесли её своему владыке, тот приказал вырвать драгоценные камни из оправы, надеясь овладеть заключённым в них могуществом." +
                    "\n\nНо золото померкло, камни утратили сияние, а прекрасное творение превратилось в изуродованный обломок былого величия." +
                    "\n\nРазгневанный конунг велел выбросить бесполезную драгоценность." +
                    "\n\nЛишь один старый кузнец заметил, что в изуродованной оправе ещё тлела искра прежнего могущества." +
                    "\n\nНикто из смертных не ведал, какой силой обладал Бризингамен в руках самой Фрейи. Но даже ничтожной крупицы этой силы оказалось достаточно, чтобы тяжесть железа и золота перестала тяготить плечи его нового владельца." +
                    "\n\nСказывают, кузнец покинул королевство той же ночью, унеся столько сокровищ, сколько смог собрать до рассвета." +
                    "\n\nА конунг так и не узнал, что собственными руками отверг божественный дар, чью истинную ценность не сумел постичь." +
                    "\n\n<color=#B1ABA5><i>«Искал силу богов, чтобы владеть всем. Но не сумел сохранить даже то, что уже держал в руках.»</i></color>",
                [BrisingamenIds.EffectDescriptionKey] = "+150 к переносимому весу.",

                // Нить Норн
                [NornThreadItemRegistration.ItemKey] = ItemNames.NornThreadRu,
                [NornThreadItemRegistration.ItemDescKey] =
                    "",
                [NornThreadItemRegistration.EffectNameKey] = "Последняя нить",
                [NornThreadItemRegistration.EffectDescKey] =
                    "Смертельный урон уничтожает амулет вместо владельца и снимает весь долг отложенной расплаты. Здоровье становится равным 50% от значения до смертельного удара. После разрыва — 3 с защиты от потери здоровья.",
                [NornThreadItemRegistration.BrokenMessageKey] = "Нить судьбы оборвалась.",

                // Кираса безмолвной расплаты
                [DelayedDoomIds.ItemKey] = ItemNames.SilentReckoningRu,
                [DelayedDoomIds.DescriptionKey] =
                    "<color=#C9A56B><b>Сила реликвии</b></color>\n" +
                    "Если урон одного удара после защиты превышает 50% максимального здоровья или этот удар смертелен, кираса срабатывает. " +
                    "50% урона проходит сразу, ещё 70% исходного удара возвращается долгом в течение 6 секунд. " +
                    "Слишком сильный удар может убить даже после срабатывания.",
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
                    "<color=#C9A56B><b>Сила реликвии</b></color>\n<color=#9EDFF2><b>Боевой запал</b></color>: потеря собственного здоровья заряжает Костоломы; каждые 10% максимального HP, потерянные от урона, дают 20 единиц заряда. Без новых ран заряд постепенно угасает." +
                    "\n<color=#9EDFF2><b>Ответный удар (колёсико мыши)</b></color>: при полном заряде высвобождает круговую ударную волну и создаёт временный защитный покров. Пока покров активен, новый заряд не накапливается.",
                [CestusItemRegistration.CestusEffectNameKey] = "Закон арены",
                [CestusItemRegistration.CestusEffectDescKey] =
                    "+10 к максимуму здоровья.\n" +
                    "+90% к скорости атаки.\n" +
                    "-8 брони.",

                // Накидка хозяина ямы
                [PitKingItemRegistration.PitKingChestItemKey + "_lore"] =
                    "<color=#C9A56B><b>II. Король без короны</b></color>" +
                    "\n<color=#B1ABA5><i>«Они бросили его на самое дно. Оттуда он и начал своё восхождение.»</i></color>" +
                    "\n\nМного зим арена принадлежала тем, кто наблюдал за смертью с высоких каменных трибун. Богачи делали ставки, воины гибли ради их развлечения, а победителям доставалось лишь право сражаться снова." +
                    "\n\nСреди них был один, чьё имя толпа выкрикивала чаще всех." +
                    "\n\nНе за благородство. Не за милосердие." +
                    "\n\nЗа дерзость, с которой он встречал каждого нового противника, и за улыбку, не исчезавшую даже под градом ударов." +
                    "\n\nОднажды хозяева арены решили, что этот боец стал слишком опасен. Они приказали выпустить против него всех чемпионов разом." +
                    "\n\nНикто не знает, сколько длилась та битва." +
                    "\n\nКогда последние ворота распахнулись, на песке стоял лишь один человек. Израненный, едва державшийся на ногах, он поднял окровавленные кулаки к трибунам." +
                    "\n\nИ впервые за всю историю ямы толпа приветствовала не победителя." +
                    "\n\n<b>Она приветствовала своего короля.</b>" +
                    "\n\nГоворят, с того дня он носил накидку прежнего хозяина, но никогда не надевал корону." +
                    "\n\nВедь тому, кто завоевал власть собственными кулаками, не требовалось золото, чтобы доказать своё право на трон." +
                    "\n\n<color=#B1ABA5><i>«Можешь владеть ареной. Но пока я на ней стою — хозяин здесь я.»</i></color>",
                [PitKingItemRegistration.PitKingChestItemKey] = ItemNames.PitKingRu,
                [PitKingItemRegistration.PitKingChestItemDescKey] =
                    "",
                [PitKingItemRegistration.PitKingChestEffectNameKey] = "Воля хозяина ямы",
                [PitKingItemRegistration.PitKingChestEffectDescKey] =
                    "+20 к максимуму здоровья. \nС экипированными Костоломами: +60 вместо +20.",

                [SteelHeartSet.SteelHeartSetNameKey] = "Стальное сердце",
                [SteelHeartSet.SteelHeartSetDescKey] =
                    "При экипированных Костоломах сила защитного покрова и урон ударной волны «Ответного удара» увеличиваются на 50%.",

                // Шкура Хельхейма
                [PeltOfHelheimItemRegistration.ItemKey] = ItemNames.PeltOfHelheimRu,
                [PeltOfHelheimItemRegistration.ItemDescKey] =
                    "<color=#9EDFF2><b>Покров Хельхейма:</b></color> если надет полный сет Фенриса, в руках Клятва Хель и активно Касание Хель V, владелец получает <b>очень высокую устойчивость к колющему урону.</b>",
                [PeltOfHelheimItemRegistration.EffectNameKey] = "Покров Хельхейма",
                [PeltOfHelheimItemRegistration.EffectDescKey] =
                    "Касание Хель V завершает связь Фенриса с Хельхеймом.\nОчень высокая устойчивость к колющему урону.",

                // Плащ раненого зверя
                [WoundedBeastItemRegistration.WoundedBeastCapeItemKey + "_lore"] =
                    "<color=#C9A56B><b>III. Последний оскал</b></color>" +
                    "\n<color=#B1ABA5><i>«Раненый зверь опаснее голодного. Особенно когда он научился улыбаться.»</i></color>" +
                    "\n\nГоворят, однажды на арену выпустили зверя, которого держали без пищи семь дней и семь ночей. Его шкура была покрыта старыми шрамами, а в глазах не осталось ничего, кроме голода и ненависти." +
                    "\n\nПротив него выставили чемпиона, чьё имя уже гремело на всю яму." +
                    "\n\nТолпа ждала кровавой расправы." +
                    "\n\nЗверь бросился первым. Его когти разорвали плоть, клыки впились в плечо, и песок под ногами воина окрасился кровью." +
                    "\n\nНо чемпион не закричал." +
                    "\n\nОн рассмеялся." +
                    "\n\nГоворят, бой продолжался так долго, что даже самые азартные зрители перестали делать ставки. Зверь истекал кровью, воин едва стоял на ногах, но ни один не желал уступать." +
                    "\n\nКогда всё закончилось, чемпион опустился рядом с поверженным противником и долго смотрел в его угасающие глаза." +
                    "\n\nВпервые за много лет он увидел на арене существо, которое было так похоже на него самого." +
                    "\n\n<b>Оно не знало, когда следует сдаться.</b>" +
                    "\n\nИз шкуры зверя он велел сшить плащ. Не в память о победе, а в знак уважения к единственному противнику, который сражался с той же отчаянной жаждой жизни." +
                    "\n\nСказывают, что с тех пор, когда смерть подступала к чемпиону особенно близко, древняя шкура словно пробуждалась, заставляя раны затягиваться, а сердце — биться сильнее." +
                    "\n\nНо за эту силу приходилось платить: защищала она плохо, и каждый новый удар оставлял раны глубже прежних." +
                    "\n\n<color=#B1ABA5><i>«Пока я дышу, ты ещё не победил.»</i></color>",
                [WoundedBeastItemRegistration.WoundedBeastCapeItemKey] = ItemNames.WoundedBeastRu,
                [WoundedBeastItemRegistration.WoundedBeastCapeItemDescKey] =
                    "",
                [WoundedBeastItemRegistration.WoundedBeastCapeEffectNameKey] = "Раненый зверь",
                [WoundedBeastItemRegistration.WoundedBeastCapeEffectDescKey] =
                    "Получаемый урон +20%.\n" +
                    "Раз в секунду восстанавливает здоровье: чем ниже текущее здоровье, тем сильнее восстановление.\n" +
                    "Пока действуют огонь, яд или дух, регенерация от плаща снижается в 2 раза.\n\n" +
                    "При использовании вместе с Кирасой безмолвной расплаты эффект лечения сокращается в 2 раза.",

                // Плащ выжженной стойкости
                [StaminaCapeItemRegistration.ItemKey] = ItemNames.BurnedResolveRu,
                [StaminaCapeItemRegistration.ItemDescKey] =
                    "",
                [StaminaCapeItemRegistration.EffectNameKey] = "Выжженная стойкость",
                [StaminaCapeItemRegistration.EffectDescKey] =
                    "25% входящего урона перенаправляется в выносливость, " +
                    "а оставшиеся 75% наносятся здоровью. " +
                    "Если выносливости недостаточно для полного поглощения, " +
                    "непоглощённый урон по здоровью усиливается на 75%.",
            }, true));

            loc.AddTranslation("English", WithConfiguredValues(new Dictionary<string, string>
            {
                // Lore-only previews at Haldor (inventory descriptions remain separate).
                [HelOathItemRegistration.HelOathItemKey + "_lore"] =
                    "<color=#C9A56B><b>I. The Price of an Endless Hunt</b></color>" +
                    "\n<color=#B1ABA5><i>“The dead have no need of life. Yet sometimes the living have need of death.”</i></color>" +
                    "\n\nAncient tales speak of a hunter who once ventured beyond the paths where mortal footsteps end." +
                    "\n\nHe pursued prey that neither men nor beasts could catch. Night after night, he followed its trail until the forests gave way to lifeless plains and the snow beneath his feet turned black." +
                    "\n\nAt the banks of a frozen river, Hel herself stood in his path." +
                    "\n\n— You have come too far, mortal. All hunts end here." +
                    "\n\nBut the hunter did not bow his head." +
                    "\n\n— Then give me the strength to finish mine." +
                    "\n\nThe goddess regarded him for a long while. Then she held out her hand." +
                    "\n\n— Half your life. Not a moment less." +
                    "\n\nThe hunter asked for no time to consider." +
                    "\n\nHe surrendered what mortals cling to most desperately, and felt the cold close around his heart. His blood slowed, his breath dwindled to the faintest whisper, yet his body filled with a strength unknown to the living." +
                    "\n\nHel placed a bow in his hands, its string colder than the stone of a grave." +
                    "\n\n— Go. But remember: every step you take now belongs to me." +
                    "\n\nThey say that from that night onward, the hunter moved swifter than the northern wind. Neither weariness nor cold could halt him, and every arrow carried an echo of the realm of the dead." +
                    "\n\nAnd when Hel’s power fully awakened in his hands, even the heavens trembled at his final shot." +
                    "\n\n<color=#B1ABA5><i>“I gave her half my life. What remains will be enough to finish the hunt.”</i></color>",
                [NornThreadItemRegistration.ItemKey + "_lore"] =
                    "<color=#C9A56B><b>III. The Final Gift</b></color>" +
                    "\n<color=#B1ABA5><i>“Fate cannot be cheated twice. But sometimes once is enough.”</i></color>" +
                    "\n\nAn old saga tells of a warrior to whom the Norns revealed the day of his death. Fearing what had been foretold, he spent long years searching for a way to escape it." +
                    "\n\nNeither an iron cuirass that delayed deadly wounds nor a cloak that turned pain into exhaustion could grant him true salvation." +
                    "\n\nAnd so he resolved to seek out those who weave fate itself." +
                    "\n\nNo one knows how the warrior found the dwelling of the Norns. Some said he descended to the roots of Yggdrasil. Others claimed the goddesses themselves let him approach, curious how far mortal stubbornness would carry him." +
                    "\n\nHe returned only after three winters." +
                    "\n\nUpon his chest hung a small amulet, within which shimmered a thread of finest silver. It was said to have been torn from the very tapestry of fate, able once to preserve a life whose appointed hour had come." +
                    "\n\nThe warrior searched for no other means of salvation." +
                    "\n\nHe knew he now possessed one last chance." +
                    "\n\nBut on the night the Norns’ prophecy came to pass, something happened that even the old storytellers had not foreseen." +
                    "\n\nThey say a wounded warrior stood at the gates of a ruined fortress. His cuirass was split, his cloak hung in blackened tatters, and his legs could barely bear his weight." +
                    "\n\nThe Thread of the Norns still glimmered upon his chest." +
                    "\n\nNearby, someone was weeping." +
                    "\n\nNo one knows whom he saw among the ruins. Yet one survivor later swore he had watched the warrior remove the amulet from his neck and place it in another’s trembling hand." +
                    "\n\nThen he lifted his weapon once more and went out to meet his enemies." +
                    "\n\nWhen morning came, they found him among the fallen. His cuirass was pierced, and only ashes remained of his cloak." +
                    "\n\n<b>But the Thread of the Norns was gone.</b>" +
                    "\n\nSome said the goddesses had reclaimed it. Others claimed that somewhere a person still lived, gifted a fate the warrior had once reserved for himself." +
                    "\n\nNo one ever learned the truth." +
                    "\n\nOnly a few words remained, engraved inside the cuirass, where none had noticed them before:" +
                    "\n\n<color=#B1ABA5><i>“I challenged death for the sake of my own life. Only at the end did I learn whose life was worth fighting for.”</i></color>",
                [DelayedDoomIds.ItemKey + "_lore"] =
                    "<color=#C9A56B><b>I. Time Borrowed</b></color>" +
                    "\n<color=#B1ABA5><i>“Death has not forgotten you. It has merely agreed to wait.”</i></color>" +
                    "\n\nThey say the three Norns once revealed to a warrior the day of his death. They told him where he would fall, whose blade would strike him down, and how many blows he would land before his thread was cut." +
                    "\n\nBut the warrior refused to bow to what had been foretold." +
                    "\n\nFor many winters he labored at the forge, seeking to make armor that could cheat death itself. Seven times the iron split beneath his hammer, and seven times he began anew." +
                    "\n\nOn the eighth attempt he forged a cuirass that could endure crushing blows and leave its wearer a few precious moments of life." +
                    "\n\nThe warrior rejoiced. At last, he believed, he had conquered fate." +
                    "\n\nBut before long he discovered a terrible truth." +
                    "\n\nThe wounds he had escaped in battle returned later. Pain found him in the dead of night, blood seeped through old bandages, and his heart shuddered beneath blows struck hours before." +
                    "\n\nHe had not defeated death." +
                    "\n\n<b>He had only learned to borrow time from it.</b>" +
                    "\n\nStill, the warrior continued his search. So long as even one moment remained, perhaps the foretold end could yet be changed." +
                    "\n\n<color=#B1ABA5><i>“Every debt comes due. Even when the debtor forgets whom he owes.”</i></color>",
                [CestusItemRegistration.CestusItemKey + "_lore"] =
                    "<color=#C9A56B><b>I. Laughter Through Blood</b></color>" +
                    "\n<color=#B1ABA5><i>“Hit harder. I might think you’re going easy on me.”</i></color>" +
                    "\n\nOld tales speak of a fighter who entered the pit without sword, shield, or fear. Nothing clad his fists but heavy iron Bone Crushers, scored by countless battles." +
                    "\n\nThe finest warriors were sent against him. They broke his ribs, split his skin, and drove him into the blood-soaked sand." +
                    "\n\nYet whenever the crowd thought the fight was over, laughter rose from the pit." +
                    "\n\nHe would rise, spit blood, and beckon his opponent closer." +
                    "\n\nThey say the Bone Crushers remembered every blow their master suffered. The more pain he endured, the more terrible the strength sleeping inside his fists became." +
                    "\n\nAnd when his patience finally ran out, he returned all that suffering in one devastating strike. The walls shuddered, stone cracked, and the spectators forgot whom they had been cheering for." +
                    "\n\nSome said he had learned it from a beast he once defeated. Others claimed the beast had simply met its own kind in the arena." +
                    "\n\n<color=#B1ABA5><i>“Count how many times you hit me. I’ll pay it all back with one.”</i></color>",
                [PeltOfHelheimItemRegistration.ItemKey + "_lore"] =
                    "<color=#C9A56B><b>III. Mantle of the Dead Wolf</b></color>" +
                    "\n<color=#B1ABA5><i>“Even in the realm of the dead, there are creatures that fear the dark.”</i></color>" +
                    "\n\nThey tell of a great black wolf that roamed the borders of Helheim, its fur reflecting no light, its tracks never fading from the frozen earth." +
                    "\n\nNo dead soul dared approach it. No living soul had survived the encounter." +
                    "\n\nBut one day, the wolf caught the scent of unfamiliar blood." +
                    "\n\nAcross the silent plains walked a hunter. His heart beat with only half its former strength, and in his hands rested a bow marked by Hel herself." +
                    "\n\nThe beast barred his way." +
                    "\n\nThe hunter stopped." +
                    "\n\nFor a long while they stared into each other’s eyes. One had been born of darkness. The other had willingly surrendered half his life to it." +
                    "\n\nThen the wolf sprang." +
                    "\n\nThey say the struggle lasted until the dead stars vanished from the sky. The beast tore at the earth with its claws, while the hunter evaded each lunge, letting Hel’s cold seep ever deeper into his flesh." +
                    "\n\nWhen the goddess’s final Touch settled upon his shoulders, at last he drew the string." +
                    "\n\nOne arrow." +
                    "\n\nOne flash of icy light." +
                    "\n\nAnd silence." +
                    "\n\nFrom the fallen beast’s hide the hunter fashioned a cloak. They say its dark fur kept the will of its former master: only one deemed worthy by Hel could pass through a storm of arrows as though through winter snow." +
                    "\n\nBut as the goddess’s blessing faded, so too did that ancient protection." +
                    "\n\n<color=#B1ABA5><i>“The dead do not fear arrows. Now I know why.”</i></color>",
                [StaminaCapeItemRegistration.ItemKey + "_lore"] =
                    "<color=#C9A56B><b>II. The Price of Defiance</b></color>" +
                    "\n<color=#B1ABA5><i>“Iron may withstand a thousand blows. But how many can the man beneath it bear?”</i></color>" +
                    "\n\nOld sagas tell of a warrior to whom the Norns foretold death. Refusing to accept his fate, he forged a cuirass that could delay the worst of his wounds." +
                    "\n\nYet every moment borrowed from death had to be repaid in time." +
                    "\n\nYears passed. The warrior survived countless battles, and each left its mark upon him. His cuirass granted him time, but never freed him from the reckoning." +
                    "\n\nSo he sought another way." +
                    "\n\nIn a land where funeral pyres smoldered even beneath winter snow, he found an ancient cloth steeped in the ashes of the fallen. From it he fashioned a cloak that could turn part of his pain into exhaustion." +
                    "\n\nFor the first time in many years, he felt hope." +
                    "\n\nBlades cut through the air and spears pierced his defenses, yet instead of deep wounds he felt the crushing weight of unbearable weariness." +
                    "\n\nAs long as strength remained, he fought on." +
                    "\n\nBut one day, surrounded by enemies, the warrior took a step for which he had no strength left." +
                    "\n\nThe cloak blackened as if consumed by unseen flame. Pain crashed upon him with such fury that every wound he had escaped seemed to return many times worse." +
                    "\n\nWhen the battle ended, they found him lying among the ashes. He still breathed, but could not even lift his arms." +
                    "\n\nFor the first time, he wondered whether all those years spent fleeing the inevitable had been in vain." +
                    "\n\nYet his fear of death outweighed his doubts." +
                    "\n\nAnd so he resolved to seek salvation where no mortal had dared to look before." +
                    "\n\n<b>Among those who weave fate itself.</b>" +
                    "\n\n<color=#B1ABA5><i>“He burned through his own strength rather than fall. But even ashes grow cold.”</i></color>",
                [UiTextIds.ShopAll] = "All",
                [UiTextIds.ShopEquipment] = "Equipment",
                [UiTextIds.ShopResources] = "Resources",
                [UiTextIds.ShopOther] = "Other",
                [UiTextIds.TooltipScroll] = "Scroll: mouse wheel / PgUp, PgDn",
                [HelOathItemRegistration.HelOathItemKey] = ItemNames.HelOathEn,
                [HelOathItemRegistration.HelOathItemDescKey] =
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
                [BerserkerIds.AxeItemKey + "_lore"] =
                    "<color=#C9A56B><b>I. Thirst for Blood</b></color>\n" +
                    "<color=#B1ABA5><i>“Not all blood spilled in battle belongs to the vanquished.”</i></color>\n\n" +
                    "They say this axe was once raised by a warrior already counted among the dead. " +
                    "Wounded and surrounded, he fought until none remained to raise a weapon against him.\n\n" +
                    "Ever since, the iron remembers the taste of that battle. The closer its wielder draws to death, " +
                    "the deadlier each blow becomes. Every clash kindles Wrath, and spilled blood becomes a debt that must one day be collected.\n\n" +
                    "<color=#B1ABA5><i>“As long as the heart beats, the reckoning is not over.”</i></color>",
                [BerserkerIds.AxeDescriptionKey] =
                    "<color=#C9A56B><b>Power of the Relic</b></color>\n" +
                    "<color=#9EDFF2><b>Bloodthirst</b></color>: +0.2% axe damage for each point of missing HP.\n\n" +
                    "<color=#9EDFF2><b>Wrath (I–V)</b></color>: axe hits build Wrath. Each stage grants " +
                    "+{wrath_speed}% attack speed and -{wrath_stamina}% stamina cost. " +
                    "Wrath gradually fades without new hits.\n\n" +
                    "<color=#9EDFF2><b>Blood Debt</b></color>: builds from wounds dealt by enemies and from axe hits. " +
                    "Empowers Reckoning and Ragnarök; gradually fades outside combat.\n\n" +
                    "<color=#9EDFF2><b>Reckoning (middle mouse)</b></color>: at Wrath V and Blood Debt x{reckoning_debt}+, " +
                    "the secondary attack becomes Reckoning. On hit, spends {reckoning_spend}% of the debt, " +
                    "adds damage, and heals for {reckoning_heal}% of direct damage dealt to the target.\n\n" +
                    "<color=#e72517><b>Ragnarök ({ragnarok_key})</b></color>: requires Wrath V, Blood Debt x{ragnarok_debt}+, " +
                    "and the Berserker Cloak equipped. Calls down meteors, consuming all Wrath and Blood Debt. " +
                    "Exhaustion follows the cast.",

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
                [BerserkerIds.CloakItemKey + "_lore"] =
                    "<color=#C9A56B><b>III. Harbinger of Ragnarök</b></color>\n" +
                    "<color=#B1ABA5><i>“When the earth has drunk its fill of blood, the heavens will carry on the battle.”</i></color>\n\n" +
                    "This tattered cloak offers no shelter from blades. Every blow strikes deeper, " +
                    "every wound weighs heavier. Still, its wearer will not yield.\n\n" +
                    "An ancient saga tells that in his final battle, the berserker called upon the end of days itself. " +
                    "The sky turned black, and a rain of burning stones fell upon the earth.\n\n" +
                    "When the flames died, neither enemy nor victor remained. " +
                    "Only a torn cloak lay among the ashes.\n\n" +
                    "<color=#B1ABA5><i>“Ragnarök makes no distinction between victors and the fallen.”</i></color>",
                [BerserkerIds.CloakEffectKey] = "Harbinger of Ragnarok",
                [BerserkerIds.CloakEffectDescriptionKey] =
                    "+30% incoming damage.\n" +
                    "+50% Blood Debt generation.\n" +
                    "Ragnarök: requires the Berserker Greataxe, Wrath V, and sufficient Blood Debt. " +
                    "The cast consumes all Wrath and Debt, then inflicts Exhaustion.",

                // Berserker Helmet
                [BerserkerIds.HelmetItemKey] = ItemNames.BerserkerHelmetEn,
                [BerserkerIds.HelmetItemKey + "_lore"] =
                    "<color=#C9A56B><b>II. Unbroken Will</b></color>\n" +
                    "<color=#B1ABA5><i>“Fear abandons a man long before life does.”</i></color>\n\n" +
                    "No one knows how many blows this helm has endured. Its iron is scarred by blades, " +
                    "and beneath its visor the traces of another's blood remain forever.\n\n" +
                    "They say that in his final battle its wearer ceased to feel pain. " +
                    "His enemies' blows could no longer break him, and every swing of the axe seemed " +
                    "to restore strength to his dying body.\n\n" +
                    "But once his Wrath faded, every old wound made itself known again.\n\n" +
                    "<color=#B1ABA5><i>“He who has forgotten pain has not yet conquered death.”</i></color>",
                [BerserkerIds.HelmetEffectKey] = "Unbroken Wrath",
                [BerserkerIds.HelmetEffectDescriptionKey] =
                    "Each Wrath stage:\n" +
                    "+4 armor.\n" +
                    "-15% incoming stagger buildup.\n" +
                    "-1% incoming damage.\n" +
                    "+1% healing from actual HP lost by enemies to direct axe hits " +
                    "(up to 5% at Wrath V).",

                // Corrupted Brisingamen
                [BrisingamenIds.ItemKey] = ItemNames.BrisingamenEn,
                [BrisingamenIds.DescriptionKey] =
                    "<color=#C9A56B><b>The Weight of Gold</b></color>" +
                    "\n<color=#B1ABA5><i>“Gods know nothing of gold’s weight. Only mortals bend beneath it.”</i></color>" +
                    "\n\nThey say there once lived a king whose treasuries overflowed with gold, yet whose heart remained empty. Neither conquered lands nor the riches of fallen foes could satisfy his hunger to possess." +
                    "\n\nOne day he heard the tale of Brísingamen, the wondrous necklace of the goddess Freyja, whose beauty outshone the sun itself." +
                    "\n\nThe king swore that the treasure would be his." +
                    "\n\nFor many winters his men searched for a path to the divine relic. And when at last they brought it before their lord, he ordered the precious stones torn from their settings, hoping to claim the power bound within them." +
                    "\n\nBut the gold grew dull, the gems lost their radiance, and the beautiful creation became a mutilated remnant of its former glory." +
                    "\n\nIn his fury, the king ordered the worthless trinket thrown away." +
                    "\n\nOnly an old smith noticed that a spark of its former might still smoldered in the ruined setting." +
                    "\n\nNo mortal knew what power Brísingamen had possessed in Freyja’s own hands. Yet even the smallest ember of that power was enough to lift the weight of iron and gold from its new wearer’s shoulders." +
                    "\n\nThey say the smith left the kingdom that very night, carrying away as much treasure as he could gather before dawn." +
                    "\n\nAnd the king never learned that with his own hands he had cast aside a divine gift whose true worth he had failed to comprehend." +
                    "\n\n<color=#B1ABA5><i>“He sought the might of gods to possess all things, yet could not keep even what he already held.”</i></color>",
                [BrisingamenIds.EffectDescriptionKey] = "+150 maximum carry weight.",

                // Thread of the Norns
                [NornThreadItemRegistration.ItemKey] = ItemNames.NornThreadEn,
                [NornThreadItemRegistration.ItemDescKey] =
                    "",
                [NornThreadItemRegistration.EffectNameKey] = "The Last Thread",
                [NornThreadItemRegistration.EffectDescKey] =
                    "Lethal damage destroys the amulet instead of its wearer and clears all Delayed Doom debt. Health becomes 50% of the amount held before the lethal hit. After breaking, health cannot be lost for 3 seconds.",
                [NornThreadItemRegistration.BrokenMessageKey] = "The thread of fate has snapped.",

                // Cuirass of Silent Reckoning
                [DelayedDoomIds.ItemKey] = ItemNames.SilentReckoningEn,
                [DelayedDoomIds.DescriptionKey] =
                    "<color=#C9A56B><b>Relic Power</b></color>\n" +
                    "The cuirass activates if a single hit after defenses exceeds 50% of maximum health or would be lethal. " +
                    "50% of the damage strikes immediately; another 70% of the original hit returns as debt over 6 seconds. " +
                    "An overwhelming blow can still kill you even when the cuirass activates.",
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
                    "<color=#C9A56B><b>Power of the Relic</b></color>\n<color=#9EDFF2><b>Battle Fervor</b></color>: losing your own health charges the Bone Crushers; every 10% of maximum HP lost to damage grants 20 charge. Charge gradually fades without fresh wounds." +
                    "\n<color=#9EDFF2><b>Counterstrike (middle mouse)</b></color>: at full charge, releases a circular shockwave and grants a temporary protective barrier. Charge cannot build while the barrier is active.",
                [CestusItemRegistration.CestusEffectNameKey] = "Law of the Arena",
                [CestusItemRegistration.CestusEffectDescKey] =
                    "+10 to maximum health.\n" +
                    "+90% attack speed.\n" +
                    "-8 armor.",

                // Pit King's Cuirass
                [PitKingItemRegistration.PitKingChestItemKey + "_lore"] =
                    "<color=#C9A56B><b>II. King Without a Crown</b></color>" +
                    "\n<color=#B1ABA5><i>“They threw him to the very bottom. That was where his rise began.”</i></color>" +
                    "\n\nFor many winters, the arena belonged to those who watched death from high stone galleries. The wealthy wagered gold, warriors died for their amusement, and the victors earned only the right to fight again." +
                    "\n\nAmong them was one fighter whose name the crowd cried louder than any other." +
                    "\n\nNot for nobility. Not for mercy." +
                    "\n\nFor the insolence with which he greeted each new opponent, and for the grin that never vanished, even beneath a storm of blows." +
                    "\n\nOne day the masters of the pit decided he had become too dangerous. They ordered every champion they possessed to face him at once." +
                    "\n\nNo one knows how long that battle lasted." +
                    "\n\nWhen the final gates opened, only one man still stood upon the sand. Bloodied and barely able to keep his feet, he raised his broken fists toward the stands." +
                    "\n\nAnd for the first time in the pit’s history, the crowd did not hail a victor." +
                    "\n\n<b>They hailed their king.</b>" +
                    "\n\nFrom that day on, they say, he wore the old master’s mantle, but never a crown." +
                    "\n\nA man who had taken his throne with his bare fists needed no gold to prove his right to rule." +
                    "\n\n<color=#B1ABA5><i>“You may own the arena. But while I stand in it, I’m the one in charge.”</i></color>",
                [PitKingItemRegistration.PitKingChestItemKey] = ItemNames.PitKingEn,
                [PitKingItemRegistration.PitKingChestItemDescKey] =
                    "",
                [PitKingItemRegistration.PitKingChestEffectNameKey] = "Pit King's Vigor",
                [PitKingItemRegistration.PitKingChestEffectDescKey] =
                    "+20 maximum health. \nWith Bone Crushers equipped: +60 instead of +20.",

                [SteelHeartSet.SteelHeartSetNameKey] = "Steel Heart",
                [SteelHeartSet.SteelHeartSetDescKey] =
                    "With Bone Crushers equipped, Counterstrike's barrier strength " +
                    "and shockwave damage are increased by 50%.",

                // Pelt of Helheim
                [PeltOfHelheimItemRegistration.ItemKey] = ItemNames.PeltOfHelheimEn,
                [PeltOfHelheimItemRegistration.ItemDescKey] =
                    "<color=#9EDFF2><b>Helheim's Ward:</b></color> while wearing the full Fenris set, wielding Hel's Oath, and maintaining Hel's Touch V, the wearer becomes <b>very resistant to Pierce damage.</b>",
                [PeltOfHelheimItemRegistration.EffectNameKey] = "Helheim's Ward",
                [PeltOfHelheimItemRegistration.EffectDescKey] =
                    "Hel's Touch V completes the bond between Fenris and Helheim.\nVery resistant to Pierce damage.",

                // Cloak of the Wounded Beast
                [WoundedBeastItemRegistration.WoundedBeastCapeItemKey + "_lore"] =
                    "<color=#C9A56B><b>III. The Last Snarl</b></color>" +
                    "\n<color=#B1ABA5><i>“A wounded beast is more dangerous than a starving one. Especially when it has learned to smile.”</i></color>" +
                    "\n\nThey say a beast was once cast into the pit after seven days and seven nights without food. Its hide bore old scars, and nothing remained in its eyes but hunger and hatred." +
                    "\n\nThe champion whose name already thundered through the arena was sent to face it." +
                    "\n\nThe crowd expected a slaughter." +
                    "\n\nThe beast struck first. Its claws tore flesh, its fangs sank into the warrior’s shoulder, and the sand beneath them turned red." +
                    "\n\nBut the champion did not scream." +
                    "\n\nHe laughed." +
                    "\n\nThey say the fight lasted so long that even the most eager gamblers stopped placing bets. The beast bled, the warrior could scarcely stand, and neither would yield." +
                    "\n\nWhen it was over, the champion knelt beside his fallen foe and looked long into its fading eyes." +
                    "\n\nFor the first time in years, he had met something in the pit that was so much like himself." +
                    "\n\n<b>It did not know how to surrender.</b>" +
                    "\n\nHe had a cloak sewn from the beast’s hide. Not as a trophy, but in honor of the only opponent who had fought with the same desperate hunger to live." +
                    "\n\nThey say that ever afterward, whenever death crept too close, the ancient pelt would stir, closing wounds and urging its wearer’s heart to beat harder." +
                    "\n\nYet its gift came at a price: it offered little protection, and every fresh blow cut deeper than the last." +
                    "\n\n<color=#B1ABA5><i>“As long as I’m breathing, you haven’t won.”</i></color>",
                [WoundedBeastItemRegistration.WoundedBeastCapeItemKey] = ItemNames.WoundedBeastEn,
                [WoundedBeastItemRegistration.WoundedBeastCapeItemDescKey] =
                    "",
                [WoundedBeastItemRegistration.WoundedBeastCapeEffectNameKey] = "Wounded Beast",
                [WoundedBeastItemRegistration.WoundedBeastCapeEffectDescKey] =
                    "Incoming damage +20%.\n" +
                    "Restores health once per second: the lower your current health is, the stronger the regeneration becomes.\n" +
                    "While burning, poisoned, or affected by spirit damage, the cape regeneration is halved.\n\n" +
                    "When used together with the Cuirass of Silent Reckoning, its healing effect is halved.",

                // Cloak of Burned Resolve
                [StaminaCapeItemRegistration.ItemKey] = ItemNames.BurnedResolveEn,
                [StaminaCapeItemRegistration.ItemDescKey] =
                    "",
                [StaminaCapeItemRegistration.EffectNameKey] = "Burned Resolve",
                [StaminaCapeItemRegistration.EffectDescKey] =
                    "25% of incoming damage is redirected to stamina, " +
                    "while the remaining 75% is dealt to health. " +
                    "If there is not enough stamina to absorb the full amount, " +
                    "the remaining health damage is increased by 75%.",
            }, false));
        }

        private static string Number(float value) =>
            value.ToString("0.##", CultureInfo.InvariantCulture);

        private static Dictionary<string, string> WithConfiguredValues(Dictionary<string, string> text, bool ru)
        {
            text[HelOathItemRegistration.HelOathItemDescKey] = text[HelOathItemRegistration.HelOathItemKey + "_lore"] +
                                                               "\n\n" +
                                                               text[HelOathItemRegistration.HelOathItemDescKey];
            text[PeltOfHelheimItemRegistration.ItemDescKey] = text[PeltOfHelheimItemRegistration.ItemKey + "_lore"] +
                                                              "\n\n" + text[PeltOfHelheimItemRegistration.ItemDescKey];
            text[CestusItemRegistration.CestusItemDescKey] = text[CestusItemRegistration.CestusItemKey + "_lore"] +
                                                             "\n\n" + text[CestusItemRegistration.CestusItemDescKey];
            text[DelayedDoomIds.DescriptionKey] =
                text[DelayedDoomIds.ItemKey + "_lore"] + "\n\n" + text[DelayedDoomIds.DescriptionKey];
            text[StaminaCapeItemRegistration.ItemDescKey] = text[StaminaCapeItemRegistration.ItemKey + "_lore"];
            text[NornThreadItemRegistration.ItemDescKey] = text[NornThreadItemRegistration.ItemKey + "_lore"];
            text[PitKingItemRegistration.PitKingChestItemDescKey] =
                text[PitKingItemRegistration.PitKingChestItemKey + "_lore"];
            text[WoundedBeastItemRegistration.WoundedBeastCapeItemDescKey] =
                text[WoundedBeastItemRegistration.WoundedBeastCapeItemKey + "_lore"];
            text[BerserkerIds.AxeDescriptionKey] = text[BerserkerIds.AxeItemKey + "_lore"] + "\n\n" +
                                                   text[BerserkerIds.AxeDescriptionKey];
            text[BerserkerIds.CloakDescriptionKey] = text[BerserkerIds.CloakItemKey + "_lore"];
            text[BerserkerIds.HelmetDescriptionKey] = text[BerserkerIds.HelmetItemKey + "_lore"];
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