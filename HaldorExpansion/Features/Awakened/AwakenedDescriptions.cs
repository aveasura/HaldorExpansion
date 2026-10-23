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
                ? "<b>Эхо охотника</b>\n" +
                  "Хель возвращает отголоски павших, чтобы они охотились рядом с вами.\n\n" +
                  "Обычные попадания по врагам собирают до трёх душ. Призраки повторяют ваши выстрелы, нанося " +
                  Percent(HelOathGhostRules.NormalDamageMultiplier) + " их урона. " +
                  "Их призрачным стрелам не преграда стены и камень. " +
                  "При Объятиях Хель каждый выпускает усиленную стрелу с " +
                  Percent(HelOathGhostRules.ActiveDamageMultiplier) +
                  " урона способности, после чего покидает этот мир. " +
                  "Даже при промахе усиленная стрела взрывается в намеченной точке. " +
                  "Атаки призраков не заряжают способности.\n\n" +
                  "<b>Обмен душами.</b> Нажмите E (по умолчанию), чтобы сложить печать и перенестись " +
                  "к проявившемуся призраку, поглотив его душу.\n" +
                  "Стоимость: 1 душа и " + Percent(HelOathSoulSwap.StaminaCostFraction) +
                  " максимальной выносливости.\n\n" +
                  "Без обычных попаданий души начинают исчезать через " +
                  Number(HelOathGhostRules.InactivitySeconds) + " с, затем по одной каждые " +
                  Number(HelOathGhostRules.SoulDecaySeconds) + " с. " +
                  "При снятом луке призраки скрываются, а души убывают по одной каждые " +
                  Number(HelOathGhostRules.SoulDecaySeconds) + " с."
                : "<b>Hunter's Echo</b>\n" +
                  "Hel returns the echoes of the fallen to hunt at your side.\n\n" +
                  "Normal hits against enemies gather up to three souls. Ghosts echo your shots for " +
                  Percent(HelOathGhostRules.NormalDamageMultiplier) + " of their damage. " +
                  "Walls and stone cannot stop their spectral arrows. " +
                  "When Hel's Embrace fires, each ghost releases an empowered arrow for " +
                  Percent(HelOathGhostRules.ActiveDamageMultiplier) +
                  " of the ability's damage, then departs this world. " +
                  "Even on a miss, empowered arrows explode at the intended point. " +
                  "Ghost attacks do not charge abilities.\n\n" +
                  "<b>Soul Swap.</b> Press E (default) to form a seal and teleport to a materialized spirit, " +
                  "consuming its soul.\n" +
                  "Cost: 1 soul and " + Percent(HelOathSoulSwap.StaminaCostFraction) +
                  " of maximum stamina.\n\n" +
                  "Without normal hits, souls begin to fade after " + Number(HelOathGhostRules.InactivitySeconds) +
                  " s, then one every " + Number(HelOathGhostRules.SoulDecaySeconds) + " s. " +
                  "While the bow is unequipped, ghosts are hidden and one soul fades every " +
                  Number(HelOathGhostRules.SoulDecaySeconds) + " s.");

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