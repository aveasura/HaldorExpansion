using UnityEngine;

namespace HaldorExpansion.Features.HelOath
{
    // Все настройки внешности призраков - здесь.
    internal static class HelOathGhostVisualSettings
    {
        // ТЕЛО. Opacity: 0 = невидимый, 1 = непрозрачный. Было 0.46.
        internal const float BodyOpacity = 0.36f;

        // RGB от 0 до 1: больше G = зеленее, больше B = голубее, меньше R = насыщеннее.
        internal const float BodyRed = 0.14f;
        internal const float BodyGreen = 0.90f;

        internal const float BodyBlue = 0.70f;

        // Цветное свечение не зависит от тёмных текстур брони. 0 = выключено.
        internal const float BodyGlowStrength = 0.50f;

        // Небольшое спокойное изменение прозрачности; 0 отключает пульсацию.
        internal const float BodyPulseAmount = 0.025f;
        internal const float BodyPulseSpeed = 1.2f;

        // АУРА. Opacity — непрозрачность ОДНОГО облачка; наложение усиливает эффект.
        internal static readonly bool AuraEnabled = true;

        internal const float AuraOpacity = 0.17f;

        // Яркость RGB отдельно от прозрачности. 1 = исходный цвет, 0.65 = приглушённый.
        internal const float AuraBrightness = 0.65f;
        internal const float AuraRed = 0.16f;
        internal const float AuraGreen = 0.95f;

        internal const float AuraBlue = 0.64f;

        // Цвет верхних, растворяющихся частей потока.
        internal const float AuraTipRed = 0.10f;
        internal const float AuraTipGreen = 0.65f;

        internal const float AuraTipBlue = 0.56f;

        // Подъём в м/с и жизнь в секундах: их произведение примерно задаёт высоту подъёма.
        internal const float AuraRiseSpeed = 0.50f;

        internal const float AuraLifetime = 3.2f;

        // Ширина и высота мягкого облачка в метрах. Близкие значения убирают "иглы".
        internal const float AuraWidth = 0.90f;

        internal const float AuraLength = 1.00f;

        // Плотность. Увеличивать осторожно: больше наложений = более плотная аура.
        internal const float AuraParticlesPerSecond = 16f;

        // Непрерывный объём вокруг всего тела, одинаковая ширина спереди и сбоку.
        // Нижняя граница = CenterY - Height/2 = -0.25 м, верхняя = 1.95 м.
        // Облачка расширяются и поднимаются выше этой области, закрывая макушку.
        internal const float AuraEmitterRadius = 0.56f;
        internal const float AuraEmitterHeight = 2.20f;

        internal const float AuraEmitterCenterY = 0.85f;

        // Нижняя дымка: медленно выходит из земли, перекрывая область ступней.
        // Не привязана к костям ног. При уходе души поднимается со всей оболочкой.
        internal const float AuraGroundCenterY = -0.15f;
        internal const float AuraGroundHeight = 0.30f;
        internal const float AuraGroundRiseSpeed = 0.18f;
        internal const float AuraGroundOpacityMultiplier = 0.60f;

        internal const float AuraGroundRateMultiplier = 0.50f;

        // Слабое плавное колыхание. Для абсолютно прямого подъёма поставить Strength = 0.
        internal const float AuraNoiseStrength = 0.12f;
        internal const float AuraNoiseFrequency = 0.35f;

        internal const float AuraNoiseScrollSpeed = 0.12f;

        // Второй, более широкий и прозрачный слой связывает облачка в дымку.
        internal const float AuraVeilOpacityMultiplier = 0.32f;

        internal const float AuraVeilRateMultiplier = 0.42f;

        // УХОД ДУШИ. Аура отделяется от тела и продолжает жить сама.
        internal const float AuraReleaseDuration = 6f;

        internal const float AuraReleaseHeight = 7f;

        // Доля времени, в течение которой плавно затихает появление новых облачков.
        internal const float AuraReleaseEmissionFraction = 0.45f;

        // С какой доли подъёма начинает угасать весь шлейф (0–0.95).
        internal const float AuraReleaseFadeStart = 0.35f;

        internal static readonly Color BodyTint = new Color(BodyRed, BodyGreen, BodyBlue, 1f);
        internal static readonly Color BodyEmission = BodyTint * BodyGlowStrength;
        internal static readonly Color AuraTint = new Color(AuraRed, AuraGreen, AuraBlue, 1f);
        internal static readonly Color AuraTipTint = new Color(AuraTipRed, AuraTipGreen, AuraTipBlue, 1f);
    }
}