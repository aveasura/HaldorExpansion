using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx.Configuration;
using HaldorExpansion.Features.Berserker;
using HaldorExpansion.Features.DelayedDoom;
using HaldorExpansion.Features.HelOath;
using HaldorExpansion.Presentation;
using Jotunn.Managers;
using UnityEngine;

internal static class LocalizationTests
{
    private static int checks;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        checks++;
    }
    private static string Number(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);
    private static Dictionary<string, string> Words(string language) =>
        LocalizationManager.Instance.GetLocalization().Languages[language];

    private static void CheckCatalogs()
    {
        var ru = Words("Russian");
        var en = Words("English");
        Check(ru.Count > 60, "Expected complete item/status/UI catalog");
        Check(new HashSet<string>(ru.Keys).SetEquals(en.Keys), "Russian/English key parity");
        foreach (var language in new[] { "Russian", "English" })
        {
            foreach (var pair in Words(language))
            {
                Check(!string.IsNullOrWhiteSpace(pair.Value), language + " empty translation " + pair.Key);
                // Indexed format slots are consumed by status effects at runtime.
                Check(!Regex.IsMatch(pair.Value, @"\{(?!\d+(?::[^{}]+)?\})[^{}]+\}"), language + " unresolved placeholder " + pair.Key);
            }
            foreach (var lore in Words(language).Where(pair => pair.Key.EndsWith("_lore")))
            {
                var descriptions = Words(language).Where(pair => pair.Key != lore.Key &&
                    pair.Value.StartsWith(lore.Value, StringComparison.Ordinal)).ToArray();
                Check(descriptions.Length > 0, language + " lore has an inventory description " + lore.Key);
                foreach (var description in descriptions)
                {
                    string nativeStats = "$item_armor: <color=orange>25</color>\nPassive effect";
                    string full = ItemLoreTooltip.MoveToEnd(description.Key + "\n\n" + nativeStats,
                        description.Key, description.Value, lore.Value);
                    Check(full.StartsWith(nativeStats, StringComparison.Ordinal) && full.EndsWith(lore.Value, StringComparison.Ordinal),
                        language + " real item lore follows stats and effects " + description.Key);
                    Check(full.IndexOf(lore.Value, StringComparison.Ordinal) == full.LastIndexOf(lore.Value, StringComparison.Ordinal),
                        language + " real item lore appears only once " + description.Key);
                }
            }
            for (int stage = 1; stage <= HelOathState.MaxTouchStacks; stage++)
            {
                Check(Words(language).ContainsKey(HelOathIds.TouchNameKey(stage)), "Touch stage name exists");
                string description = Words(language)[HelOathIds.TouchDescriptionKey(stage)];
                Check(description.Contains("+" + Number(stage * HelOathState.TouchSpeedPerStack * 100f) + "%"), "Actual Hel movement constant");
                Check(description.Contains("-" + Number(stage * HelOathState.TouchStaminaReductionPerStack * 100f) + "%"), "Actual Hel stamina constant");
            }
            Check(Words(language).ContainsKey(DelayedDoomIds.DebtNameKey), "Published Doom debt name key exists");
            Check(Words(language).ContainsKey(DelayedDoomIds.DebtDescriptionKey), "Published Doom debt description exists");
            Check(Words(language).ContainsKey(DelayedDoomIds.RegenNameKey), "Doom regen name exists");
            Check(Words(language)[DelayedDoomIds.RegenDescriptionKey].Contains("+" + Number(DelayedDoomItemRegistration.DelayedDoomChestFoodRegenBonus)), "Actual Doom regeneration constant");
        }
    }

    private static void Main()
    {
        string directory = Path.Combine(Path.GetTempPath(), "he-localization-bind-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var balance = new ConfigFile(Path.Combine(directory, "balance.cfg"), false) { SaveOnConfigSet = false };
            var input = new ConfigFile(Path.Combine(directory, "input.cfg"), false) { SaveOnConfigSet = false };
            BerserkerConfiguration.Register(balance, input);
            ModLocalization.AddLocalizations();
            CheckCatalogs();
            EquipmentTooltipTests.Run(Check);
            AwakenedDescriptionTests.Run(Check);
            float[] stamina = { 14f, 21f, 26.6f, 31.5f, 35f };
            float[] sprint = { 6f, 9f, 11.4f, 13.5f, 15f };
            for (int stage = 1; stage <= 5; stage++)
            {
                Check(Math.Abs(BerserkerConfiguration.WrathStaminaReductionFraction(stage) * 100f - stamina[stage - 1]) < 0.001f,
                    "Front-loaded stamina bonus at stage " + stage);
                Check(Math.Abs(BerserkerConfiguration.WrathCloakRunSpeedBonusFraction(stage) * 100f - sprint[stage - 1]) < 0.001f,
                    "Front-loaded sprint bonus at stage " + stage);
            }
            Check(Words("English")[BerserkerIds.AxeDescriptionKey].Contains("Ragnarök (E)"), "Default bound Ragnarok key");
            float[] defaultSpeeds = { 20f, 30f, 38f, 45f, 50f };
            foreach (string language in new[] { "Russian", "English" })
            {
                string axeDescription = Words(language)[BerserkerIds.AxeDescriptionKey];
                foreach (float speed in new[] { 20f, 50f })
                    Check(axeDescription.Contains(Number(speed)), "Default curve appears in axe description");
                for (int stage = 1; stage <= 5; stage++)
                    Check(Words(language)[BerserkerIds.WrathKeyPrefix + stage + "_desc"]
                        .Contains("+" + Number(defaultSpeeds[stage - 1]) + "%"), "Default Wrath attack speed stage " + stage);
                Check(axeDescription.Contains(language == "Russian" ? "Ярость II+" : "Wrath II+"),
                    "Reckoning unlock requirement describes Wrath II");
                string exhaustionDefault = Words(language)[BerserkerIds.ExhaustionDescriptionKey];
                Check(exhaustionDefault.Contains("40%") && exhaustionDefault.Contains("25%") &&
                      exhaustionDefault.Contains("7"), "Default Exhaustion contains all penalties and duration");
            }
            foreach (string language in new[] { "Russian", "English" })
            {
                Localization.instance = new Localization { Language = language };
                BerserkerConfiguration.CloakIncomingDamageBonusPercent.Value = 30f;
                BerserkerConfiguration.CloakBloodDebtGainBonusPercent.Value = 50f;
                BerserkerConfiguration.WrathCloakRunSpeedBonusAtMaxPercent.Value = 15f;
                ModLocalization.AddLocalizations();
                string key = BerserkerIds.CloakEffectDescriptionKey;
                string fullTooltip = "Armor and effects\n" + key;
                Check(Localization.instance.Localize(key).Contains("+30%"), "Prime cached cloak effect");
                Check(Localization.instance.Localize(fullTooltip).Contains("6–15%"), "Prime cached full tooltip");
                BerserkerConfiguration.CloakIncomingDamageBonusPercent.Value = 42f;
                BerserkerConfiguration.CloakBloodDebtGainBonusPercent.Value = 63f;
                BerserkerConfiguration.WrathCloakRunSpeedBonusAtMaxPercent.Value = 20f;
                ModLocalization.AddLocalizations();
                foreach (string cachedText in new[] { key, fullTooltip })
                {
                    string updated = Localization.instance.Localize(cachedText);
                    Check(updated.Contains("+42%") && updated.Contains("+63%") && updated.Contains("8–20%"),
                        language + " cached cloak tooltip refreshes after config change");
                }
            }
            BerserkerConfiguration.WrathAttackSpeedPercentPerStage.Value = 13f;
            BerserkerConfiguration.WrathStaminaCostReductionPercentPerStage.Value = 7f;
            BerserkerConfiguration.ReckoningLifeStealPercent.Value = 37f;
            BerserkerConfiguration.ReckoningSpendPercent.Value = 63f;
            BerserkerConfiguration.ReckoningMinimumDebt.Value = 250f;
            BerserkerConfiguration.RagnarokMinimumDebt.Value = 750f;
            BerserkerConfiguration.RagnarokExhaustionMovePenaltyPercent.Value = 42f;
            BerserkerConfiguration.RagnarokExhaustionAttackPenaltyPercent.Value = 23f;
            BerserkerConfiguration.RagnarokExhaustionStaminaRegenPenaltyPercent.Value = 47f;
            BerserkerConfiguration.RagnarokExhaustionDuration.Value = 4.5f;
            BerserkerConfiguration.RagnarokButton.Config.BoxedValue = KeyCode.F;
            // Russian culture must not change decimal formatting in either catalog.
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
            Localization.instance = new Localization { Language = "Russian" };
            ModLocalization.AddLocalizations();
            CheckCatalogs();
            foreach (string language in new[] { "Russian", "English" })
            {
                var words = Words(language);
                string axe = words[BerserkerIds.AxeDescriptionKey];
                foreach (string fragment in new[] { "19.6–49%", "37%", "63%", "x2.5+", "x7.5+", "(F)" })
                    Check(axe.Contains(fragment), language + " configured axe fragment " + fragment);
                float[] expectedSpeeds = { 26f, 39f, 49.4f, 58.5f, 65f };
                for (int stage = 1; stage <= 5; stage++)
                {
                    string speed = Number(expectedSpeeds[stage - 1]);
                    string wrath = words[BerserkerIds.WrathKeyPrefix + stage + "_desc"];
                    Check(wrath.Contains("+" + speed + "%"), "Configured stage attack speed");
                    if (stage == 1 || stage == 5)
                        Check(axe.Contains(speed), "Configured speed range appears in axe description");
                    float[] expectedStamina = { 19.6f, 29.4f, 37.24f, 44.1f, 49f };
                    Check(wrath.Contains("-" + Number(expectedStamina[stage - 1]) + "%"), "Configured stage stamina");
                }
                string exhaustion = words[BerserkerIds.ExhaustionDescriptionKey];
                Check(exhaustion.Contains("42%") && exhaustion.Contains("23%") &&
                      exhaustion.Contains("47%") && exhaustion.Contains("4.5"),
                    "Configured movement, attack, regeneration and duration of Exhaustion");
            }
            Check(Localization.instance.Words[BerserkerIds.AxeDescriptionKey.TrimStart('$')] == Words("Russian")[BerserkerIds.AxeDescriptionKey], "Native current-language words refreshed");
            BerserkerConfiguration.WrathAttackSpeedPercentPerStage.Value = 100f;
            BerserkerConfiguration.WrathStaminaCostReductionPercentPerStage.Value = 19f;
            Localization.instance.Language = "English";
            ModLocalization.AddLocalizations();
            foreach (string language in new[] { "Russian", "English" })
            {
                string stageFive = Words(language)[BerserkerIds.WrathKeyPrefix + "5_desc"];
                Check(stageFive.Contains("+200%") && stageFive.Contains("-90%"), "Wrath display respects gameplay caps");
            }
            Check(Localization.instance.Words[BerserkerIds.AxeDescriptionKey.TrimStart('$')] == Words("English")[BerserkerIds.AxeDescriptionKey], "English native refresh uses English values");
            Console.WriteLine("PASS: " + checks + " localization assertions; real production localization/config/IDs, boundary doubles for Unity/Jotunn.");
        }
        finally { Directory.Delete(directory, true); }
    }
}
