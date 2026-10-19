using System;
using HaldorExpansion.Features.HelOath;
using System.Collections.Generic;
using System.Globalization;

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
            return Localization.instance.Localize(quality >= AwakenedBalance.QualityThreshold
                ? Key(artifact)
                : HiddenKey);
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
                                  ? "Скрытый потенциал: пробуждается на " +
                                    AwakenedBalance.QualityThreshold +
                                    "-м уровне улучшения предмета."
                                  : "Hidden Potential: awakens at upgrade level " +
                                    AwakenedBalance.QualityThreshold + ".") +
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
                ? "Эхо охотника. Успешные обычные попадания собирают до трёх душ параллельно с зарядом Объятий Хель. " +
                  "Призраки с небольшим запозданием повторяют обычные выстрелы с " + Percent(HelOathGhostRules.NormalDamageMultiplier) + " урона. " +
                  "При Объятиях Хель на любой стадии Касания каждый призрак выпускает стрелу с " + Percent(HelOathGhostRules.ActiveDamageMultiplier) +
                  " урона активки и освобождается. Урон призраков не заряжает способности. " +
                  "При снятом луке призраки скрыты, души убывают по одной за " + Number(HelOathGhostRules.SoulDecaySeconds) +
                  " с. Без обычных попаданий первая душа уходит через " + Number(HelOathGhostRules.InactivitySeconds) +
                  " с, остальные — с интервалом " + Number(HelOathGhostRules.SoulDecaySeconds) + " с.\n" +
                  "Дань Хель. Три души принимают смертельный удар, оставляя 25% здоровья, имевшегося перед ударом (минимум 1), " +
                  "и дают 3 с неуязвимости. Восстановление: 180 с. Души расходуются раньше Нити Норн."
                : "Hunter's Echo. Successful normal hits gather up to three souls alongside Hel's Embrace charge. " +
                  "Ghosts echo normal shots after a short delay for " + Percent(HelOathGhostRules.NormalDamageMultiplier) + " damage. " +
                  "Whenever Hel's Embrace fires, each ghost fires for " + Percent(HelOathGhostRules.ActiveDamageMultiplier) +
                  " of the active shot's damage and is released. Ghost damage never charges abilities. " +
                  "While the bow is unequipped, ghosts are hidden and lose one soul per " + Number(HelOathGhostRules.SoulDecaySeconds) +
                  " s. Without normal hits, the first soul leaves after " + Number(HelOathGhostRules.InactivitySeconds) +
                  " s, then one every " + Number(HelOathGhostRules.SoulDecaySeconds) + " s.\n" +
                  "Hel's Tribute. Three souls absorb a lethal hit, leaving 25% of health held before the hit (minimum 1) " +
                  "and granting 3 s invulnerability. Recovery: 180 s. Souls are spent before Norn's Thread.");
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