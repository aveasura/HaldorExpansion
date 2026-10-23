using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace HaldorExpansion.Features.Cestus
{
    internal static class CestusHudView
    {
        private static Hud attachedHud;
        private static bool attachedToHealth;
        private static bool capturedHealthSize;
        private static Vector2 detachedHealthSize;
        private static readonly Vector3[] HealthCorners = new Vector3[4];
        private static readonly FieldInfo[] HealthBarFields = FindHealthBarFields();
        private static Image CestusShieldOverlayImage;
        private static RectTransform CestusShieldOverlayRect;
        private static RectTransform CestusShieldReferenceRect;

        private static Text CestusShieldText;
        private static RectTransform CestusShieldTextRect;
        private static int? CestusShieldDisplayedValue;

        private static bool CestusShieldOverlayLoggedMissing;

        internal static void UpdateCestusShieldOverlay(Hud hud, Player player)
        {
            if (hud == null)
            {
                Clear();
                return;
            }

            if (attachedHud != hud) Clear();
            if (!CestusHudConfiguration.Visible || player == null || player.IsDead())
            {
                SetVisible(false);
                return;
            }

            EnsureCestusShieldOverlay(hud);
            if (CestusShieldOverlayImage == null || CestusShieldOverlayRect == null ||
                CestusShieldText == null || CestusShieldTextRect == null)
                return;

            CestusStateStore.CestusState state = CestusStateStore.GetOrCreateCestusState(player);
            // A detached gauge represents the shield's own capacity, so both its
            // location and fill are independent of current/max HP.
            float shield01 = attachedToHealth
                ? CestusShieldRuntime.GetCestusShieldBarNormalized(player)
                : CestusShieldRuntime.GetCestusShieldNormalized(player);
            if (float.IsNaN(shield01) || float.IsInfinity(shield01)) shield01 = 0f;
            shield01 = Mathf.Clamp01(shield01);
            bool visible = attachedToHealth ? shield01 > 0.001f : state.ShieldRemaining > 0.001f;
            SetVisible(visible);
            if (!visible) return;

            if (attachedToHealth)
            {
                // Keep the original copying, HP-relative start and fill math
                // exactly as before with the default settings.
                CopyRectTransform(CestusShieldReferenceRect, CestusShieldOverlayRect);
                if (!CestusHudConfiguration.AutoSize)
                {
                    CestusShieldOverlayRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                        CestusHudConfiguration.Width);
                    CestusShieldOverlayRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                        CestusHudConfiguration.Height);
                }

                float hp01 = CestusShieldRuntime.GetCestusHealthNormalized(player);
                if (float.IsNaN(hp01) || float.IsInfinity(hp01)) hp01 = 0f;
                float shieldStartX = CestusShieldReferenceRect.rect.width * Mathf.Clamp01(hp01);
                CestusShieldOverlayRect.anchoredPosition = CestusShieldReferenceRect.anchoredPosition +
                                                           new Vector2(shieldStartX, 0f);
                CestusShieldOverlayRect.localScale *= CestusHudConfiguration.ScaleValue;
                ApplyScreenAxisOffset();
                float shieldPixelWidth = CestusShieldOverlayRect.rect.width * shield01;
                CestusShieldTextRect.sizeDelta = new Vector2(Mathf.Max(36f, shieldPixelWidth), 18f);
                CestusShieldTextRect.anchoredPosition = new Vector2(shieldPixelWidth * 0.5f, 0f);
            }
            else
            {
                PositionDetached();
                // Keep the value centered on the gauge even near empty.
                CestusShieldTextRect.sizeDelta = new Vector2(Mathf.Max(36f, CestusShieldOverlayRect.rect.width), 18f);
                CestusShieldTextRect.anchoredPosition = Vector2.zero;
            }

            CestusShieldOverlayImage.fillAmount = shield01;
            CestusShieldTextRect.rotation = Quaternion.identity;
            int displayedValue = Mathf.CeilToInt(state.ShieldRemaining);
            if (CestusShieldDisplayedValue != displayedValue)
            {
                CestusShieldText.text = displayedValue.ToString();
                CestusShieldDisplayedValue = displayedValue;
            }
        }

        private static void SetVisible(bool visible)
        {
            if (CestusShieldOverlayImage != null && CestusShieldOverlayImage.gameObject.activeSelf != visible)
                CestusShieldOverlayImage.gameObject.SetActive(visible);
            if (CestusShieldText != null && CestusShieldText.gameObject.activeSelf != visible)
                CestusShieldText.gameObject.SetActive(visible);
        }

        internal static void Clear()
        {
            SetVisible(false); // Destroy is deferred until the end of the frame.
            if (CestusShieldOverlayRect != null)
                Object.Destroy(CestusShieldOverlayRect.gameObject);
            else if (CestusShieldTextRect != null)
                Object.Destroy(CestusShieldTextRect.gameObject);
            attachedHud = null;
            CestusShieldOverlayImage = null;
            CestusShieldOverlayRect = null;
            CestusShieldReferenceRect = null;
            CestusShieldText = null;
            CestusShieldTextRect = null;
            CestusShieldDisplayedValue = null;
            capturedHealthSize = false;
        }

        private static void ApplyScreenAxisOffset()
        {
            float x = CestusHudConfiguration.OffsetXValue;
            float y = CestusHudConfiguration.OffsetYValue;
            if (x == 0f && y == 0f) return;
            // Vanilla's HP hierarchy may be rotated. Offsets are still screen right/up.
            float uiScale = CestusShieldOverlayRect.parent.TransformVector(Vector3.right).magnitude;
            if (float.IsNaN(uiScale) || float.IsInfinity(uiScale) || uiScale < 0.0001f) uiScale = 1f;
            CestusShieldOverlayRect.position += new Vector3(x * uiScale, y * uiScale, 0f);
        }

        private static void PositionDetached()
        {
            RectTransform rect = CestusShieldOverlayRect;
            rect.anchorMin = rect.anchorMax = new Vector2(CestusHudConfiguration.X, CestusHudConfiguration.Y);
            rect.pivot = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.rotation = Quaternion.identity;
            rect.anchoredPosition =
                new Vector2(CestusHudConfiguration.OffsetXValue, CestusHudConfiguration.OffsetYValue);
            if (CestusHudConfiguration.AutoSize)
            {
                if (!capturedHealthSize)
                {
                    // Capture once, never chase changing HP geometry while detached.
                    detachedHealthSize = new Vector2(CestusHudConfiguration.Width, CestusHudConfiguration.Height);
                    if (CestusShieldReferenceRect != null)
                    {
                        CestusShieldReferenceRect.GetWorldCorners(HealthCorners);
                        float left = HealthCorners[0].x, right = left;
                        float bottom = HealthCorners[0].y, top = bottom;
                        for (int i = 1; i < HealthCorners.Length; i++)
                        {
                            left = Mathf.Min(left, HealthCorners[i].x);
                            right = Mathf.Max(right, HealthCorners[i].x);
                            bottom = Mathf.Min(bottom, HealthCorners[i].y);
                            top = Mathf.Max(top, HealthCorners[i].y);
                        }

                        float width = (right - left) / rect.TransformVector(Vector3.right).magnitude;
                        float height = (top - bottom) / rect.TransformVector(Vector3.up).magnitude;
                        if (FinitePositive(width) && FinitePositive(height))
                            detachedHealthSize = new Vector2(width, height);
                    }

                    capturedHealthSize = true;
                }

                rect.sizeDelta = detachedHealthSize;
            }
            else
            {
                capturedHealthSize = false;
                rect.sizeDelta = new Vector2(CestusHudConfiguration.Width, CestusHudConfiguration.Height);
            }

            rect.localScale = Vector3.one * CestusHudConfiguration.ScaleValue;
        }

        private static bool FinitePositive(float value)
            => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0.001f;

        private static void EnsureCestusShieldOverlay(Hud hud)
        {
            bool attach = CestusHudConfiguration.Attached;
            if (attachedHud == hud && attachedToHealth == attach &&
                CestusShieldOverlayImage != null && CestusShieldOverlayRect != null &&
                CestusShieldText != null && CestusShieldTextRect != null &&
                CestusShieldOverlayRect.parent != null &&
                (!attach || (CestusShieldReferenceRect != null &&
                             CestusShieldOverlayRect.parent == CestusShieldReferenceRect.parent)))
                return;

            Clear();
            Component healthBar = FindReferenceHealthBarComponent(hud);
            RectTransform referenceRect = healthBar != null ? healthBar.GetComponent<RectTransform>() : null;
            Transform parent = referenceRect != null ? referenceRect.parent : null;
            if (!attach)
            {
                Canvas canvas = referenceRect != null ? referenceRect.GetComponentInParent<Canvas>() : null;
                if (canvas == null) canvas = hud.GetComponentInParent<Canvas>();
                if (canvas == null) canvas = hud.GetComponentInChildren<Canvas>(true);
                parent = canvas != null ? canvas.rootCanvas.transform : null;
            }

            if (parent == null)
            {
                if (!CestusShieldOverlayLoggedMissing)
                {
                    CestusShieldOverlayLoggedMissing = true;
                    HaldorExpansionPlugin.DebugLog(
                        "[HaldorExpansion] Cestus shield HUD: health bar or HUD canvas not found");
                }

                return;
            }

            attachedHud = hud;
            attachedToHealth = attach;

            GameObject overlayObject = new GameObject(
                "HaldorExpansion_CestusShieldSegment",
                typeof(RectTransform));

            RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
            overlayRect.SetParent(parent, false);

            if (attach) CopyRectTransform(referenceRect, overlayRect);

            Image overlayImage = overlayObject.AddComponent<Image>();
            overlayImage.raycastTarget = false;
            overlayImage.type = Image.Type.Filled;
            overlayImage.fillMethod = attach ? Image.FillMethod.Horizontal : Image.FillMethod.Vertical;
            overlayImage.fillOrigin = attach ? (int)Image.OriginHorizontal.Left : (int)Image.OriginVertical.Bottom;
            overlayImage.fillAmount = 0f;
            overlayImage.color = new Color(0.62f, 0.62f, 0.62f, 0.78f);

            Image referenceImage = healthBar != null ? healthBar.GetComponent<Image>() : null;
            if (referenceImage == null && healthBar != null)
                referenceImage = healthBar.GetComponentInChildren<Image>(true);

            if (referenceImage != null)
            {
                overlayImage.sprite = referenceImage.sprite;
                overlayImage.material = referenceImage.material;
                overlayImage.preserveAspect = attach && referenceImage.preserveAspect;
            }

            if (attach) overlayRect.SetSiblingIndex(referenceRect.GetSiblingIndex() + 1);
            overlayObject.SetActive(false);

            GameObject textObject = new GameObject(
                "HaldorExpansion_CestusShieldText",
                typeof(RectTransform));

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(overlayRect, false);
            textRect.anchorMin = new Vector2(attach ? 0f : 0.5f, 0.5f);
            textRect.anchorMax = new Vector2(attach ? 0f : 0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);

            textRect.sizeDelta = new Vector2(64f, 18f);
            textRect.localScale = Vector3.one;
            textRect.rotation = Quaternion.identity;
            textRect.anchoredPosition = Vector2.zero;

            Text shieldText = textObject.AddComponent<Text>();
            shieldText.raycastTarget = false;
            shieldText.alignment = TextAnchor.MiddleCenter;
            shieldText.horizontalOverflow = HorizontalWrapMode.Overflow;
            shieldText.verticalOverflow = VerticalWrapMode.Overflow;
            shieldText.resizeTextForBestFit = false;
            shieldText.fontSize = 14;
            shieldText.fontStyle = FontStyle.Bold;
            shieldText.color = new Color(1f, 1f, 1f, 0.95f);
            shieldText.text = string.Empty;

            Text referenceText = healthBar != null ? healthBar.GetComponent<Text>() : null;
            if (referenceText == null && healthBar != null)
                referenceText = healthBar.GetComponentInChildren<Text>(true);

            if (referenceText != null && referenceText.font != null)
            {
                shieldText.font = referenceText.font;
            }
            else
            {
                shieldText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            Outline outline = textObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(1f, -1f);

            textObject.SetActive(false);

            CestusShieldOverlayImage = overlayImage;
            CestusShieldOverlayRect = overlayRect;
            CestusShieldReferenceRect = referenceRect;
            CestusShieldText = shieldText;
            CestusShieldDisplayedValue = null;
            CestusShieldTextRect = textRect;
            CestusShieldOverlayLoggedMissing = false;

            HaldorExpansionPlugin.DebugLog("[HaldorExpansion] Cestus segmented HUD with text created");
        }

        internal static Component FindReferenceHealthBarComponent(Hud hud)
        {
            if (hud == null) return null;
            for (int i = 0; i < HealthBarFields.Length; i++)
            {
                Component bar = HealthBarFields[i].GetValue(hud) as Component;
                if (bar != null) return bar;
            }

            return null;
        }

        private static FieldInfo[] FindHealthBarFields()
        {
            FieldInfo[] fields =
                typeof(Hud).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var matches = new List<FieldInfo>();
            int fastCount = 0;
            foreach (FieldInfo field in fields)
            {
                if (field.FieldType.Name != "GuiBar") continue;
                string name = field.Name.ToLowerInvariant();
                if (!name.Contains("health") && !name.Contains("hp")) continue;
                if (name.Contains("fast")) matches.Insert(fastCount++, field);
                else matches.Add(field);
            }

            return matches.ToArray();
        }

        private static void CopyRectTransform(RectTransform source, RectTransform target)
        {
            target.anchorMin = source.anchorMin;
            target.anchorMax = source.anchorMax;
            target.pivot = source.pivot;
            target.anchoredPosition = source.anchoredPosition;
            target.sizeDelta = source.sizeDelta;
            target.localScale = source.localScale;
            target.localRotation = source.localRotation;
            target.offsetMin = source.offsetMin;
            target.offsetMax = source.offsetMax;
        }
    }
}