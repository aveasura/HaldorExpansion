using System;
using System.Collections.Generic;
using HaldorExpansion.Features.Awakened;

internal static class AwakenedDescriptionTests
{
    internal static void Run(Action<bool, string> check)
    {
        Localization previous = Localization.instance;
        foreach (bool ru in new[] { true, false })
        {
            var words = new Dictionary<string, string>();
            AwakenedDescriptions.AddTranslations(words, ru);
            check(words.Count == 13, "Hidden potential and all twelve awakened entries");
            Localization.instance = new Localization();
            foreach (var pair in words) Localization.instance.Words[pair.Key.TrimStart('$')] = pair.Value;
            foreach (Artifact artifact in Enum.GetValues(typeof(Artifact)))
            {
                if (artifact == Artifact.None) continue;
                var item = new ItemDrop.ItemData { m_quality = 9,
                    m_shared = new ItemDrop.ItemData.SharedData { m_name = artifact.ToString() } };
                check(AwakenedDescriptions.ForItem(item, 9) == words[AwakenedDescriptions.HiddenKey], "Quality nine stays hidden " + artifact);
                check(AwakenedDescriptions.ForItem(item, 10) == words[AwakenedDescriptions.Key(artifact)], "Upgrade preview awakens " + artifact);
                check(item.m_quality == 9, "Preview preserves instance quality " + artifact);
                item.m_quality = 12;
                check(AwakenedDescriptions.ForItem(item, 0) == words[AwakenedDescriptions.Key(artifact)], "Instance quality fallback " + artifact);
                check(AwakenedDescriptions.ForItem(item, 12) == words[AwakenedDescriptions.Key(artifact)], "Quality above ten remains awake " + artifact);
            }
            check(words[AwakenedDescriptions.Key(Artifact.HelOath)] == (ru
                ? "Пробуждённый потенциал: Призраки последней охоты (в разработке)."
                : "Awakened Potential: Ghosts of the Last Hunt (Work in Progress)."), "Exact Hel placeholder");
            check(AwakenedDescriptions.ForItem(new ItemDrop.ItemData { m_quality = 10,
                m_shared = new ItemDrop.ItemData.SharedData { m_name = "Unrelated" } }, 10) == null, "Unrelated item excluded");
        }
        check(AwakenedDescriptions.InsertBeforeLore("Stats\n\nLore", "Potential", "Lore") == "Stats\n\nPotential\n\nLore", "Potential before lore");
        check(AwakenedDescriptions.InsertBeforeLore("Stats", "Potential", null) == "Stats\n\nPotential", "Items without lore supported");
        check(AwakenedDescriptions.InsertBeforeLore("Stats", null, null) == "Stats", "Unrelated tooltip unchanged");
        Localization.instance = previous;
    }
}

// Identity is a boundary here. Production identity/equipment behavior has its own tests.
namespace HaldorExpansion.Features.Awakened
{
    internal static class AwakenedEquipment
    {
        internal static Artifact Identify(ItemDrop.ItemData item) =>
            item?.m_shared != null && Enum.TryParse(item.m_shared.m_name, out Artifact artifact) ? artifact : Artifact.None;
    }
}

public class HitData
{
    public enum DamageModifier { SlightlyResistant }
}
