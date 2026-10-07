using System;
using HaldorExpansion.Features.Cestus;
using UnityEngine;
using UnityEngine.UI;

namespace HaldorExpansion.Features.Berserker
{
    // Visual-only Blood Debt gauge, anchored to the same vanilla health GuiBar
    // that Cestus uses. We never reparent/resize/modify the health bar itself.
    // Final layout is intentionally fixed in code so players cannot break the HUD through config.
    internal static class BerserkerDebtHudView
    {
        private static Hud attachedHud;
        private static RectTransform healthReference;
        private static RectTransform trackRect;
        private static Image fillImage;
        private static Text numberText;
        private static Sprite fallbackSprite;
        private static bool warnedMissingHealthBar;
        private static float nextGeometryCheckAt;
        private const float GeometryCheckInterval = 0.20f; // 5 checks/sec
        private const float DebtPerVisualCycle = 100f;
        private const float MeterWidth = 32f;
        private const float OffsetXFromHealth = 8f;
        private const float OffsetYFromHealth = 0f;

        internal static void Update(Hud hud, Player player)
        {
            if (hud == null) return;
            if (player == null || player != Player.m_localPlayer || player.IsDead())
            {
                SetVisible(false);
                return;
            }

            // Unequipping the axe must not abruptly erase the display of an
            // outstanding Blood Debt; keep it while the resource decays.
            bool visible = BerserkerEquipment.Equipped(player) || BerserkerRuntime.Debt.Amount >= 0.01d;
            if (!visible)
            {
                SetVisible(false);
                return;
            }

            Ensure(hud);
            if (trackRect == null || healthReference == null || fillImage == null || numberText == null)
                return;

            SetVisible(true);
            UpdateGeometryIfDue();

            double debt = BerserkerRuntime.Debt.Amount;
            const double perCycle = DebtPerVisualCycle;

            // The meter loops visually, but accumulated debt has no hard cap.
            long completedCycles = debt <= 0d ? 0L : (long)Math.Floor(debt / perCycle);
            double remainder = debt <= 0d ? 0d : debt - completedCycles * perCycle;
            double progress = remainder / perCycle;

            fillImage.fillAmount = (float)Math.Max(0d, Math.Min(1d, progress));
            numberText.text = "x" + completedCycles.ToString();
        }

        private static void Ensure(Hud hud)
        {
            if (attachedHud == hud && trackRect != null && healthReference != null &&
                trackRect.parent == healthReference.parent && fillImage != null && numberText != null)
                return;

            if (trackRect != null) UnityEngine.Object.Destroy(trackRect.gameObject);
            attachedHud = hud;
            healthReference = null;
            trackRect = null;
            fillImage = null;
            numberText = null;

            // Reuse Cestus' proven health-bar lookup, including its fast-health
            // preference. Do not rely on Canvas or any absolute screen position.
            Component healthBar = CestusHudView.FindReferenceHealthBarComponent(hud);
            RectTransform reference = healthBar != null ? healthBar.GetComponent<RectTransform>() : null;
            if (reference == null || reference.parent == null)
            {
                if (!warnedMissingHealthBar)
                {
                    warnedMissingHealthBar = true;
                    HaldorExpansionPlugin.Log?.LogWarning("[Berserker] Blood Debt HUD: health GuiBar not found.");
                }
                return;
            }

            healthReference = reference;
            GameObject meter = new GameObject("HaldorExpansion_BloodDebtBar", typeof(RectTransform));
            trackRect = meter.GetComponent<RectTransform>();
            trackRect.SetParent(reference.parent, false);
            trackRect.SetSiblingIndex(reference.GetSiblingIndex() + 1);

            Image referenceImage = healthBar.GetComponent<Image>();
            if (referenceImage == null)
                referenceImage = healthBar.GetComponentInChildren<Image>(true);

            Image trackImage = meter.AddComponent<Image>();
            ApplyVanillaBarStyle(trackImage, referenceImage);
            trackImage.color = new Color(0.12f, 0.11f, 0.13f, 0.82f);
            trackImage.raycastTarget = false;

            // Unlike the old floating panel, the actual meter has a fixed size.
            GameObject amount = new GameObject("BloodDebt_Fill", typeof(RectTransform));
            RectTransform amountRect = amount.GetComponent<RectTransform>();
            amountRect.SetParent(trackRect, false);
            amountRect.anchorMin = Vector2.zero;
            amountRect.anchorMax = Vector2.one;
            amountRect.pivot = new Vector2(0.5f, 0.5f);
            amountRect.offsetMin = Vector2.zero;
            amountRect.offsetMax = Vector2.zero;
            amountRect.localScale = Vector3.one;

            fillImage = amount.AddComponent<Image>();
            ApplyVanillaBarStyle(fillImage, referenceImage);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Vertical;
            fillImage.fillOrigin = (int)Image.OriginVertical.Bottom;
            fillImage.fillAmount = 0f;
            fillImage.color = new Color(0.62f, 0.29f, 0.35f, 0.72f); // Semi-transparent blood-red for more visual depth.
            fillImage.raycastTarget = false;

            // Rounded number is separate from the gauge fill (no title/large panel).
            GameObject label = new GameObject("BloodDebt_Number", typeof(RectTransform));
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.SetParent(trackRect, false);
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.sizeDelta = new Vector2(64f, 20f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.localScale = Vector3.one;

            numberText = label.AddComponent<Text>();
            numberText.raycastTarget = false;
            numberText.alignment = TextAnchor.MiddleCenter;
            numberText.horizontalOverflow = HorizontalWrapMode.Overflow;
            numberText.verticalOverflow = VerticalWrapMode.Overflow;
            numberText.fontSize = 13;
            numberText.fontStyle = FontStyle.Bold;
            numberText.color = new Color(0.95f, 0.91f, 0.91f, 1f);
            numberText.text = "x0";
            Text referenceText = healthBar.GetComponent<Text>();
            if (referenceText == null)
                referenceText = healthBar.GetComponentInChildren<Text>(true);
            numberText.font = referenceText != null && referenceText.font != null
                ? referenceText.font
                : Resources.GetBuiltinResource<Font>("Arial.ttf");
            Outline outline = label.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(1f, -1f);

            PositionBesideHealth();
            nextGeometryCheckAt = Time.unscaledTime + GeometryCheckInterval;
            warnedMissingHealthBar = false;
        }

        private static readonly Vector3[] HealthCorners = new Vector3[4];

        private static void UpdateGeometryIfDue()
        {
            float now = Time.unscaledTime;
            if (now + 0.0001f < nextGeometryCheckAt) return;
            nextGeometryCheckAt = now + GeometryCheckInterval;
            PositionBesideHealth();
        }


        private static void PositionBesideHealth()
        {
            if (trackRect == null || healthReference == null) return;
            RectTransform parent = trackRect.parent as RectTransform;
            if (parent == null) return;

            // The Valheim health-bar branch can be rotated by 90 degrees.  A
            // sibling with localRotation=identity *inherits* that rotation:
            // what should be our tall vertical gauge appears horizontal, and
            // its label is sideways. Work in WORLD axes instead.
            healthReference.GetWorldCorners(HealthCorners);
            float right = HealthCorners[0].x;
            float bottom = HealthCorners[0].y;
            float top = HealthCorners[0].y;
            for (int i = 1; i < HealthCorners.Length; i++)
            {
                if (HealthCorners[i].x > right) right = HealthCorners[i].x;
                if (HealthCorners[i].y < bottom) bottom = HealthCorners[i].y;
                if (HealthCorners[i].y > top) top = HealthCorners[i].y;
            }

            // Read the ACTUAL rendered height every HUD update, rather than
            // copying an initial RectTransform height. Valheim resizes the
            // vanilla HP meter as the player's max health changes (food etc.).
            float healthHeightWorld = top - bottom;
            if (float.IsNaN(healthHeightWorld) || float.IsInfinity(healthHeightWorld) ||
                healthHeightWorld < 0.001f) return;

            trackRect.anchorMin = Vector2.zero;
            trackRect.anchorMax = Vector2.zero;
            trackRect.pivot = Vector2.zero; // World-space position = bottom-left of the bar.
            trackRect.localScale = Vector3.one;
            // Cancel the *parent's* rotation, rather than setting a zero local
            // rotation (which is exactly what caused the sideways gauge).
            trackRect.rotation = Quaternion.identity;

            // The reference is rotated by Valheim's HUD, so rect.height is NOT
            // necessarily its visible vertical size. Convert its world-space
            // bounding height to the unrotated gauge's own layout units.
            // Reading both each frame also follows UI-scale/food changes.
            float worldYPerLocalUnit = Mathf.Abs(trackRect.TransformVector(Vector3.up).y);
            if (float.IsNaN(worldYPerLocalUnit) || float.IsInfinity(worldYPerLocalUnit) ||
                worldYPerLocalUnit < 0.0001f) return;
            float healthHeightUi = healthHeightWorld / worldYPerLocalUnit;
            trackRect.sizeDelta = new Vector2(MeterWidth, healthHeightUi);

            // Fixed HUD offsets are expressed in layout units. Convert them to
            // world units so X always moves visually RIGHT and Y visually UP,
            // regardless of how the vanilla HP hierarchy is rotated.
            float uiScale = parent.TransformVector(Vector3.right).magnitude;
            if (uiScale < 0.0001f) uiScale = 1f;
            trackRect.position = new Vector3(
                right + OffsetXFromHealth * uiScale,
                bottom + OffsetYFromHealth * uiScale,
                HealthCorners[0].z);
        }

        private static void ApplyVanillaBarStyle(Image dest, Image source)
        {
            dest.sprite = source != null && source.sprite != null ? source.sprite : SolidSprite();
            if (source != null)
            {
                dest.material = source.material;
            }
            // Reuse the vanilla texture, but fill our tall, narrow meter instead
            // of preserving the original (horizontal) HP sprite aspect ratio.
            dest.preserveAspect = false;
        }

        private static Sprite SolidSprite()
        {
            if (fallbackSprite == null)
                fallbackSprite = Sprite.Create(Texture2D.whiteTexture,
                    new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            return fallbackSprite;
        }

        private static void SetVisible(bool visible)
        {
            if (trackRect != null && trackRect.gameObject.activeSelf != visible)
                trackRect.gameObject.SetActive(visible);
        }
    }
}
