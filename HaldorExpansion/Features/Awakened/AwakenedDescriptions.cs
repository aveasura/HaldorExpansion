using System;
using System.Collections.Generic;
using System.Globalization;
using HaldorExpansion.Features.HelOath;

namespace HaldorExpansion.Features.Awakened
{
    internal static class AwakenedDescriptions
    {
        internal const string HiddenKey = "$haldor_awakened_hidden";
        private const string Prefix = "$haldor_awakened_";

        internal static string Key(Artifact artifact) => Prefix + artifact.ToString().ToLowerInvariant();

        internal static string ForItem(ItemDrop.ItemData item, int tooltipQuality)
        {
            Artifact artifact = AwakenedEquipment.Identify(item);
            if (artifact == Artifact.None || Localization.instance == null) return null;
            // Vanilla passes the prospective quality when previewing an upgrade.
            // Ordinary tooltips use this instance's quality; no shared data is changed.
            int quality = tooltipQuality > 0 ? tooltipQuality : item.m_quality;
            int threshold = AwakenedBalance.QualityThreshold;
            return quality >= threshold
                ? Localization.instance.Localize(Key(artifact))
                : string.Format(CultureInfo.InvariantCulture, Localization.instance.Localize(HiddenKey), threshold);
        }

        internal static string InsertBeforeLore(string tooltip, string potential, string lore)
        {
            if (string.IsNullOrEmpty(tooltip) || string.IsNullOrEmpty(potential)) return tooltip;
            if (!string.IsNullOrEmpty(lore) && tooltip.EndsWith(lore, StringComparison.Ordinal))
            {
                string body = tooltip.Substring(0, tooltip.Length - lore.Length).TrimEnd('\r', '\n');
                return body + (body.Length > 0 ? "\n\n" : "") + potential + "\n\n" + lore;
            }

            return tooltip + "\n\n" + potential;
        }

        private static string Number(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);
        private static string Percent(float value) => Number(value * 100f) + "%";

        internal static void AddTranslations(Dictionary<string, string> text, bool ru)
        {
            text[HiddenKey] = "<color=#AA5555>" +
                              (ru
                                  ? "Скрытый потенциал: пробуждается на {0}-м уровне улучшения предмета."
                                  : "Hidden Potential: awakens at upgrade level {0}.") +
                              "</color>";

            string title = "<color=#FF5555><b>" +
                           (ru ? "Пробуждённый потенциал:" : "Awakened Potential:") +
                           "</b></color> ";

            text[Key(Artifact.DelayedDoomChest)] = title + (ru
                ? "При активном отложенном долге и здоровье не выше " +
                  Percent(AwakenedBalance.DelayedDoomChest.HealthThreshold) +
                  " восстанавливает " + Percent(AwakenedBalance.DelayedDoomChest.HealthPerSecond) +
                  " максимального здоровья в секунду."
                : "While delayed damage debt is active and health is at or below " +
                  Percent(AwakenedBalance.DelayedDoomChest.HealthThreshold) +
                  ", restores " + Percent(AwakenedBalance.DelayedDoomChest.HealthPerSecond) +
                  " of maximum health per second.");

            text[Key(Artifact.BerserkerHelmet)] = title + (ru
                ? "Пир павших. При активной Ярости каждое убийство обычной или альтернативной атакой Секиры берсерка восстанавливает " +
                  Percent(AwakenedBalance.BerserkerHelmet.MissingHealthOnKill) +
                  " недостающего здоровья."
                : "Feast of the Fallen. With Wrath active, each kill with a normal or alternate Berserker Greataxe attack restores " +
                  Percent(AwakenedBalance.BerserkerHelmet.MissingHealthOnKill) +
                  " of missing health.");

            text[Key(Artifact.BerserkerCloak)] = title + (ru
                ? "Рагнарёк сохраняет Ярость. Расход долга и истощение остаются прежними."
                : "Ragnarok preserves Wrath. Debt consumption and exhaustion remain unchanged.");

            text[Key(Artifact.ScorchedEnduranceCloak)] = title + (ru
                ? "Небольшая устойчивость к рубящему урону: снижение на " +
                  Percent(AwakenedBalance.ScorchedEnduranceCloak.SlashReduction) + "."
                : "Slight resistance to slash damage: " +
                  Percent(AwakenedBalance.ScorchedEnduranceCloak.SlashReduction) + " reduction.");

            text[Key(Artifact.WoundedBeastCloak)] = title + (ru
                ? "При ношении: +" + Number(AwakenedBalance.WoundedBeastCloak.Health) + " максимального здоровья и +" +
                  Number(AwakenedBalance.WoundedBeastCloak.Unarmed) +
                  " к навыку безоружного боя (не выше " + Number(AwakenedBalance.SkillCap) + ")."
                : "While equipped: +" + Number(AwakenedBalance.WoundedBeastCloak.Health) + " maximum health and +" +
                  Number(AwakenedBalance.WoundedBeastCloak.Unarmed) +
                  " Unarmed skill (capped at " + Number(AwakenedBalance.SkillCap) + ").");

            text[Key(Artifact.HelheimPelt)] = title + (ru
                ? "При ношении: +" + Number(AwakenedBalance.HelheimPelt.Stamina) +
                  " максимальной выносливости. Урон от падения снижен на " +
                  Percent(AwakenedBalance.HelheimPelt.FallDamageReduction) + "."
                : "While equipped: +" + Number(AwakenedBalance.HelheimPelt.Stamina) +
                  " maximum stamina. Fall damage reduced by " +
                  Percent(AwakenedBalance.HelheimPelt.FallDamageReduction) + ".");

            text[Key(Artifact.PitKingChest)] = title + (ru
                ? "[Стальное Сердце]: бонус сета заменяется на +" +
                  Percent(AwakenedBalance.PitKingChest.ShieldMultiplier - 1f)
                : "[Steel Heart]: replaces the set's bonus with +" +
                  Percent(AwakenedBalance.PitKingChest.ShieldMultiplier - 1f));

            text[Key(Artifact.BerserkerAxe)] = title + (ru
                ? "Кровавая жатва. Каждое убийство обычной или альтернативной атакой даёт " +
                  Number(AwakenedBalance.BerserkerAxe.DebtOnKill) + " кровавого долга."
                : "Bloody Harvest. Each kill with a normal or alternate attack grants " +
                  Number(AwakenedBalance.BerserkerAxe.DebtOnKill) + " Blood Debt.");

            text[Key(Artifact.BoneCrushers)] = title + (ru
                ? "Удары восстанавливают здоровье в размере " +
                  Percent(AwakenedBalance.BoneCrushers.LifeSteal) +
                  " фактически снятого здоровья врага. Не действует на Возмездие."
                : "Attacks restore health equal to " + Percent(AwakenedBalance.BoneCrushers.LifeSteal) +
                  " of the enemy health actually lost. Does not apply to the Retribution.");
            
            text[Key(Artifact.HelOath)] = title + (ru
                ? "<color=#BFE8DF><b>Призраки последней охоты</b></color>\n" +
                  "<i>Не всем павшим дозволено обрести покой. " +
                  "Некоторым суждено вновь услышать зов охоты.</i>\n\n" +
                  "Каждая стрела Клятвы Хель, настигшая врага, " +
                  "отзывается эхом во владениях мёртвых. " +
                  "На этот зов из холодной мглы возвращаются до трёх " +
                  "павших охотников, обречённых вновь натянуть тетивы.\n\n" +
                  "Их стрелы следуют за стрелами хозяина, сохраняя " +
                  Percent(HelOathGhostRules.NormalDamageMultiplier) +
                  " их силы. Ни стены, ни камень не властны над оружием тех, " +
                  "кто уже переступил порог смерти.\n\n" +
                  "Когда пробуждаются <color=#D5F5FF><b>Объятия Хель</b></color>, павшие исполняют " +
                  "последний приказ. Каждый выпускает усиленную стрелу, " +
                  "несущую " +
                  Percent(HelOathGhostRules.ActiveDamageMultiplier) +
                  " мощи способности, после чего души возвращаются " +
                  "во владения своей повелительницы.\n\n" +
                  "<color=#8ED8D1><b>Обмен душами</b></color>\n" +
                  "<i>Завеса между жизнью и смертью истончается перед тем, " +
                  "кто осмелится пожертвовать душой.</i>\n\n" +
                  "Ценой одного из призраков хозяин Клятвы Хель " +
                  "способен мгновенно преодолеть пространство, " +
                  "заняв место павшего охотника. Но даже такой переход " +
                  "требует жертвы собственной жизненной силой.\n" +
                  "Клавиша: E (по умолчанию). Стоимость: 1 душа и " +
                  Percent(HelOathSoulSwap.StaminaCostFraction) +
                  " максимальной выносливости.\n\n" +
                  "Но ни одна душа не принадлежит миру живых вечно. " +
                  "Когда новые стрелы перестают находить жертву, " +
                  "через " +
                  Number(HelOathGhostRules.InactivitySeconds) +
                  " с связь с Хель начинает слабеть. " +
                  "Один за другим павшие возвращаются в её чертоги - " +
                  "по одному каждые " +
                  Number(HelOathGhostRules.SoulDecaySeconds) +
                  " с. Стоит Клятве Хель покинуть руки хозяина, " +
                  "и призраки вновь растворяются в холодной мгле, " +
                  "а души продолжают угасать в том же ритме.\n\n" +
                  "<color=#A9B8BA><i>Стрелы павших не питают силу " +
                  "Клятвы Хель.</i></color>"
                : "<color=#BFE8DF><b>Ghosts of the Last Hunt</b></color>\n" +
                  "<i>Not all fallen are granted rest. " +
                  "Some are destined to hear the call of the hunt once more.</i>\n\n" +
                  "Every arrow of Hel's Oath that finds its mark " +
                  "echoes through the realm of the dead. " +
                  "Answering the call, up to three fallen hunters " +
                  "emerge from the cold mist, bound to draw their bows once more.\n\n" +
                  "Their arrows follow those of their master, retaining " +
                  Percent(HelOathGhostRules.NormalDamageMultiplier) +
                  " of their power. Neither walls nor stone hold sway " +
                  "over the weapons of those who have already " +
                  "crossed the threshold of death.\n\n" +
                  "When <color=#D5F5FF><b>Hel's Embrace</b></color> awakens, the fallen obey " +
                  "one final command. Each releases an empowered arrow, " +
                  "bearing " +
                  Percent(HelOathGhostRules.ActiveDamageMultiplier) +
                  " of the ability's power, before their souls " +
                  "return to the realm of their mistress.\n\n" +
                  "<color=#8ED8D1><b>Soul Swap</b></color>\n" +
                  "<i>The veil between life and death grows thin " +
                  "before one willing to sacrifice a soul.</i>\n\n" +
                  "At the cost of a single spirit, the bearer of Hel's Oath " +
                  "may cross the distance in an instant, taking the place " +
                  "of a fallen hunter. Yet even such passage demands " +
                  "a sacrifice of one's own vital strength.\n" +
                  "Key: E (default). Cost: 1 soul and " +
                  Percent(HelOathSoulSwap.StaminaCostFraction) +
                  " of maximum stamina.\n\n" +
                  "But no soul belongs to the living world forever. " +
                  "When arrows cease to find their prey, " +
                  "the bond with Hel begins to weaken after " +
                  Number(HelOathGhostRules.InactivitySeconds) +
                  " s. One by one, the fallen return to her halls - " +
                  "one soul every " +
                  Number(HelOathGhostRules.SoulDecaySeconds) +
                  " s. Should Hel's Oath leave its bearer's hands, " +
                  "the ghosts dissolve into the cold mist once more, " +
                  "their souls continuing to fade at the same pace.\n\n" +
                  "<color=#A9B8BA><i>The arrows of the fallen " +
                  "do not replenish the power of Hel's Oath.</i></color>");
            
            text[Key(Artifact.ShadowCrossbow)] = title + (ru
                ? "Пока арбалет экипирован: +" + Number(AwakenedBalance.ShadowCrossbow.Sneak) + " к скрытности и +" +
                  Number(AwakenedBalance.ShadowCrossbow.Crossbows) +
                  " к арбалетам (каждый навык не выше " + Number(AwakenedBalance.SkillCap) + ")."
                : "While equipped: +" + Number(AwakenedBalance.ShadowCrossbow.Sneak) + " Sneak and +" +
                  Number(AwakenedBalance.ShadowCrossbow.Crossbows) +
                  " Crossbows (each skill capped at " + Number(AwakenedBalance.SkillCap) + ").");

            text[Key(Artifact.Brisingamen)] = title + (ru
                ? "Благословение отвергнутой богини. При ношении: +" +
                  Percent(AwakenedBalance.Brisingamen.StaminaRegenBonus) + " к восстановлению выносливости."
                : "Blessing of the Rejected Goddess. While equipped: +" +
                  Percent(AwakenedBalance.Brisingamen.StaminaRegenBonus) + " stamina regeneration.");
        }
    }
}