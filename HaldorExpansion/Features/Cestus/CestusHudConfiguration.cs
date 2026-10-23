using BepInEx.Configuration;
using Jotunn.Extensions;
using UnityEngine;

namespace HaldorExpansion.Features.Cestus
{
    // Register against the local/general ConfigFile, never the balance configuration.
    internal static class CestusHudConfiguration
    {
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<bool> AttachToHealthBar;
        internal static ConfigEntry<float> ScreenPositionX;
        internal static ConfigEntry<float> ScreenPositionY;
        internal static ConfigEntry<int> OffsetX;
        internal static ConfigEntry<int> OffsetY;
        internal static ConfigEntry<float> Scale;
        internal static ConfigEntry<bool> MatchHealthSize;
        internal static ConfigEntry<float> CustomWidth;
        internal static ConfigEntry<float> CustomHeight;

        internal static void Register(ConfigFile local)
        {
            const string section = "Bone Crushers Shield HUD / HUD щита Костоломов";
            Enabled = local.BindConfig(section, "Enable HUD", true,
                "Show the Cestus / Bone Crushers shield. Local visual preference; does not disable the shield mechanic.",
                synced: false);
            AttachToHealthBar = local.BindConfig(section, "Attach to Health Bar", true,
                "Keep the original shield segment at the current HP endpoint (default). Disable for a separate vertical gauge at the normalized screen position.",
                synced: false);
            ScreenPositionX = local.BindConfig(section, "Screen Position X", 0.5f,
                "Custom gauge bottom-left horizontal screen anchor: 0 = left, 1 = right. Used only when detached from HP.",
                synced: false, acceptableValues: new AcceptableValueRange<float>(0f, 1f));
            ScreenPositionY = local.BindConfig(section, "Screen Position Y", 0.2f,
                "Custom gauge bottom-left vertical screen anchor: 0 = bottom, 1 = top. Used only when detached from HP.",
                synced: false, acceptableValues: new AcceptableValueRange<float>(0f, 1f));
            OffsetX = local.BindConfig(section, "Position X", 0,
                "Horizontal offset in UI units; positive moves right. Applied to the HP segment or custom screen anchor.",
                synced: false, acceptableValues: new AcceptableValueRange<int>(-600, 600));
            OffsetY = local.BindConfig(section, "Position Y", 0,
                "Vertical offset in UI units; positive moves up. Applied to the HP segment or custom screen anchor.",
                synced: false, acceptableValues: new AcceptableValueRange<int>(-400, 400));
            Scale = local.BindConfig(section, "Scale", 1f,
                "Scale multiplier in both automatic and custom size modes. Default 1 preserves the original shield HUD.",
                synced: false, acceptableValues: new AcceptableValueRange<float>(0.5f, 2f));
            MatchHealthSize = local.BindConfig(section, "Match HP Bar Size", true,
                "Match the HP bar size. Detached mode captures its size on placement and keeps it stable as HP changes. Disable to use Custom Width and Custom Height.",
                synced: false);
            CustomWidth = local.BindConfig(section, "Custom Width", 32f,
                "Manual width in UI units when Match HP Bar Size is false. In attached mode this is the segment's local fill-axis length.",
                synced: false, acceptableValues: new AcceptableValueRange<float>(8f, 800f));
            CustomHeight = local.BindConfig(section, "Custom Height", 220f,
                "Manual height in UI units when Match HP Bar Size is false. In attached mode this is the segment's local thickness.",
                synced: false, acceptableValues: new AcceptableValueRange<float>(16f, 1600f));
        }

        internal static bool Visible => Enabled == null || Enabled.Value;
        internal static bool Attached => AttachToHealthBar == null || AttachToHealthBar.Value;
        internal static bool AutoSize => MatchHealthSize == null || MatchHealthSize.Value;
        internal static float X => Read(ScreenPositionX, 0f, 1f, 0.5f);
        internal static float Y => Read(ScreenPositionY, 0f, 1f, 0.2f);
        internal static float OffsetXValue => OffsetX == null ? 0f : Mathf.Clamp(OffsetX.Value, -600, 600);
        internal static float OffsetYValue => OffsetY == null ? 0f : Mathf.Clamp(OffsetY.Value, -400, 400);
        internal static float ScaleValue => Read(Scale, 0.5f, 2f, 1f);
        internal static float Width => Read(CustomWidth, 8f, 800f, 32f);
        internal static float Height => Read(CustomHeight, 16f, 1600f, 220f);

        private static float Read(ConfigEntry<float> entry, float min, float max, float fallback)
        {
            float value = entry == null ? fallback : entry.Value;
            return float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max);
        }
    }
}