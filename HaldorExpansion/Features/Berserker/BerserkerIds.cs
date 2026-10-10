namespace HaldorExpansion.Features.Berserker
{
    // Identifiers introduced with Berserker in 1.3.0. Do not rename published item IDs.
    internal static class BerserkerIds
    {
        internal const string Prefix = "aveasura_he_";

        internal const string AxePrefab = Prefix + "berserker_axe";
        internal const string CloakPrefab = Prefix + "berserker_cloak";
        internal const string HelmetPrefab = Prefix + "berserker_helmet";

        internal const string AxeRecipe = "recipe_" + AxePrefab;
        internal const string CloakRecipe = "recipe_" + CloakPrefab;
        internal const string HelmetRecipe = "recipe_" + HelmetPrefab;

        internal const string AxeItemKey = "$item_" + AxePrefab;
        internal const string AxeDescriptionKey = AxeItemKey + "_desc";
        internal const string CloakItemKey = "$item_" + CloakPrefab;
        internal const string CloakDescriptionKey = CloakItemKey + "_desc";
        internal const string HelmetItemKey = "$item_" + HelmetPrefab;
        internal const string HelmetDescriptionKey = HelmetItemKey + "_desc";

        internal const string CloakEffectKey = "$se_" + CloakPrefab;
        internal const string CloakEffectDescriptionKey = CloakEffectKey + "_desc";
        internal const string HelmetEffectKey = "$se_" + HelmetPrefab;
        internal const string HelmetEffectDescriptionKey = HelmetEffectKey + "_desc";
        internal const string CloakVisualStatus = Prefix + "berserker_cloak_visual";
        internal const string HelmetVisualStatus = Prefix + "berserker_helmet_visual";
        internal const string WrathStatus = Prefix + "berserker_wrath";
        internal const string ExhaustionStatus = Prefix + "berserker_ragnarok_exhaustion";

        internal const string BloodDebtKey = "$se_" + Prefix + "berserker_blood_debt";
        internal const string WrathKeyPrefix = "$se_" + Prefix + "berserker_wrath_";
        internal const string ExhaustionKey = "$se_" + ExhaustionStatus;
        internal const string ExhaustionDescriptionKey = ExhaustionKey + "_desc";
        internal const string RagnarokAbilityKey = "$ability_" + Prefix + "berserker_ragnarok";
        internal const string RagnarokNoTargetKey = "$msg_" + Prefix + "berserker_ragnarok_no_target";
        internal const string RagnarokCastKey = "$msg_" + Prefix + "berserker_ragnarok_cast";

        internal const string RagnarokButtonName = Prefix + "berserker_ragnarok";
        internal const string WrathStageZdoKey = Prefix + "berserker_wrath_stage";
        internal const string MeteorOwnerZdoKey = Prefix + "berserker_ragnarok_meteor_v1";
        internal const string MeteorIntensityZdoKey = Prefix + "berserker_meteor_intensity_v1";

        internal const string ReckoningDamageRpc = Prefix + "berserker_reckoning_damage_v1";
        internal const string ReckoningHealRpc = Prefix + "berserker_reckoning_heal_v1";
        internal const string HelmetLifeStealRpc = Prefix + "berserker_helmet_lifesteal_v1";
        internal const string ReckoningSwingRpc = Prefix + "berserker_reckoning_swing_fx_v1";
        internal const string ReckoningImpactRpc = Prefix + "berserker_reckoning_impact_fx_v1";
        internal const string ReckoningHealFxRpc = Prefix + "berserker_reckoning_heal_fx_v1";
        internal const string RagnarokCastRpc = Prefix + "berserker_ragnarok_cast_fx_v5";
        internal const string RagnarokStopRpc = Prefix + "berserker_ragnarok_stop_fx_v1";
        internal const string RagnarokReleaseRpc = Prefix + "berserker_ragnarok_release_fx_v1";
        internal const string RagnarokMeteorImpactRpc = Prefix + "berserker_ragnarok_meteor_impact_fx_v2";
    }
}