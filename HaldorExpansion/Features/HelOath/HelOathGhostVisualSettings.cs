using UnityEngine;

namespace HaldorExpansion.Features.HelOath
{
    // All ghost visuals here 
    internal static class HelOathGhostVisualSettings
    {
        internal const float BodyOpacity = 0.36f;
        internal const float BodyRed = 0.14f;
        internal const float BodyGreen = 0.90f;
        internal const float BodyBlue = 0.70f;
        internal const float BodyGlowStrength = 0.50f;
        internal const float BodyPulseAmount = 0.025f;
        internal const float BodyPulseSpeed = 1.2f;
        internal static readonly bool AuraEnabled = true;
        internal const float AuraOpacity = 0.17f;
        internal const float AuraBrightness = 0.65f;
        internal const float AuraRed = 0.16f;
        internal const float AuraGreen = 0.95f;
        internal const float AuraBlue = 0.64f;
        internal const float AuraTipRed = 0.10f;
        internal const float AuraTipGreen = 0.65f;
        internal const float AuraTipBlue = 0.56f;
        internal const float AuraRiseSpeed = 0.50f;
        internal const float AuraLifetime = 3.2f;
        internal const float AuraWidth = 0.90f;
        internal const float AuraLength = 1.00f;
        internal const float AuraParticlesPerSecond = 16f;
        internal const float AuraEmitterRadius = 0.56f;
        internal const float AuraEmitterHeight = 2.20f;
        internal const float AuraEmitterCenterY = 0.85f;
        internal const float AuraGroundCenterY = -0.15f;
        internal const float AuraGroundHeight = 0.30f;
        internal const float AuraGroundRiseSpeed = 0.18f;
        internal const float AuraGroundOpacityMultiplier = 0.60f;
        internal const float AuraGroundRateMultiplier = 0.50f;
        internal const float AuraNoiseStrength = 0.12f;
        internal const float AuraNoiseFrequency = 0.35f;
        internal const float AuraNoiseScrollSpeed = 0.12f;
        internal const float AuraVeilOpacityMultiplier = 0.32f;
        internal const float AuraVeilRateMultiplier = 0.42f;
        internal const float AuraReleaseDuration = 6f;
        internal const float AuraReleaseHeight = 7f;
        internal const float AuraReleaseEmissionFraction = 0.45f;
        internal const float AuraReleaseFadeStart = 0.35f;

        internal static readonly Color BodyTint = new Color(BodyRed, BodyGreen, BodyBlue, 1f);
        internal static readonly Color BodyEmission = BodyTint * BodyGlowStrength;
        internal static readonly Color AuraTint = new Color(AuraRed, AuraGreen, AuraBlue, 1f);
        internal static readonly Color AuraTipTint = new Color(AuraTipRed, AuraTipGreen, AuraTipBlue, 1f);
    }
}