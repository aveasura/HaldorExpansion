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
                Check(!Regex.IsMatch(pair.Value, @"\{[^{}]+\}"), language + " unresolved placeholder " + pair.Key);
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
            Check(Words("English")[BerserkerIds.AxeDescriptionKey].Contains("Ragnarök (E)"), "Default bound Ragnarok key");
            BerserkerConfiguration.WrathAttackSpeedPercentPerStage.Value = 13f;
            BerserkerConfiguration.WrathStaminaCostReductionPercentPerStage.Value = 7f;
            BerserkerConfiguration.ReckoningLifeStealPercent.Value = 37f;
            BerserkerConfiguration.ReckoningSpendPercent.Value = 63f;
            BerserkerConfiguration.ReckoningMinimumDebt.Value = 250f;
            BerserkerConfiguration.RagnarokMinimumDebt.Value = 750f;
            BerserkerConfiguration.RagnarokExhaustionMovePenaltyPercent.Value = 42f;
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
                foreach (string fragment in new[] { "+13%", "-7%", "37%", "63%", "x2.5+", "x7.5+", "(F)" })
                    Check(axe.Contains(fragment), language + " configured axe fragment " + fragment);
                for (int stage = 1; stage <= 5; stage++)
                {
                    string wrath = words[BerserkerIds.WrathKeyPrefix + stage + "_desc"];
                    Check(wrath.Contains("+" + stage * 13 + "%"), "Configured stage attack speed");
                    Check(wrath.Contains("-" + stage * 7 + "%"), "Configured stage stamina");
                }
                string exhaustion = words[BerserkerIds.ExhaustionDescriptionKey];
                Check(exhaustion.Contains("42%") && exhaustion.Contains("4.5"), "Configured exhaustion");
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
