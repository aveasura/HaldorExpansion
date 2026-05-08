using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace HaldorExpansion
{
    public partial class HaldorExpansionPlugin
    {
        private static Image CestusShieldOverlayImage;
        private static RectTransform CestusShieldOverlayRect;
        private static RectTransform CestusShieldReferenceRect;

        private static Text CestusShieldText;
        private static RectTransform CestusShieldTextRect;

        private static bool CestusShieldOverlayLoggedMissing;

        internal static void UpdateCestusShieldOverlay(Hud hud, Player player)
        {
            if (hud == null)
                return;

            EnsureCestusShieldOverlay(hud);

            if (CestusShieldOverlayImage == null ||
                CestusShieldOverlayRect == null ||
                CestusShieldReferenceRect == null ||
                CestusShieldText == null ||
                CestusShieldTextRect == null)
                return;

            if (player == null || player.IsDead())
            {
                if (CestusShieldOverlayImage.gameObject.activeSelf)
                    CestusShieldOverlayImage.gameObject.SetActive(false);

                if (CestusShieldText.gameObject.activeSelf)
                    CestusShieldText.gameObject.SetActive(false);

                return;
            }

            CestusState state = GetOrCreateCestusState(player);

            float shield01 = GetCestusShieldBarNormalized(player);
            bool visible = shield01 > 0.001f;

            if (CestusShieldOverlayImage.gameObject.activeSelf != visible)
                CestusShieldOverlayImage.gameObject.SetActive(visible);

            if (CestusShieldText.gameObject.activeSelf != visible)
                CestusShieldText.gameObject.SetActive(visible);

            if (!visible)
                return;

            float hp01 = GetCestusHealthNormalized(player);

            CopyRectTransform(CestusShieldReferenceRect, CestusShieldOverlayRect);

            float barWidth = CestusShieldReferenceRect.rect.width;
            Vector2 basePos = CestusShieldReferenceRect.anchoredPosition;
            float shieldPixelWidth = barWidth * shield01;
            float shieldStartX = barWidth * hp01;

            CestusShieldOverlayRect.anchoredPosition = basePos + new Vector2(shieldStartX, 0f);
            CestusShieldOverlayImage.fillAmount = shield01;

            CestusShieldTextRect.rotation = Quaternion.identity;
            
            CestusShieldText.text = Mathf.CeilToInt(state.ShieldRemaining).ToString();
            CestusShieldTextRect.sizeDelta = new Vector2(Mathf.Max(36f, shieldPixelWidth), 18f);
            CestusShieldTextRect.anchoredPosition = new Vector2(shieldPixelWidth * 0.5f, 0f);
        }

        private static void EnsureCestusShieldOverlay(Hud hud)
        {
            if (CestusShieldOverlayImage != null &&
                CestusShieldOverlayRect != null &&
                CestusShieldReferenceRect != null &&
                CestusShieldText != null &&
                CestusShieldTextRect != null)
                return;

            Component healthBar = FindReferenceHealthBarComponent(hud);
            if (healthBar == null)
            {
                if (!CestusShieldOverlayLoggedMissing)
                {
                    CestusShieldOverlayLoggedMissing = true;
                    DebugLog("[HaldorExpansion] Cestus segmented HUD: health bar component not found");
                }

                return;
            }

            RectTransform referenceRect = healthBar.GetComponent<RectTransform>();
            if (referenceRect == null || referenceRect.parent == null)
            {
                if (!CestusShieldOverlayLoggedMissing)
                {
                    CestusShieldOverlayLoggedMissing = true;
                    DebugLog("[HaldorExpansion] Cestus segmented HUD: reference RectTransform not found");
                }

                return;
            }

            GameObject overlayObject = new GameObject(
                "HaldorExpansion_CestusShieldSegment",
                typeof(RectTransform));

            RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
            overlayRect.SetParent(referenceRect.parent, false);

            CopyRectTransform(referenceRect, overlayRect);

            Image overlayImage = overlayObject.AddComponent<Image>();
            overlayImage.raycastTarget = false;
            overlayImage.type = Image.Type.Filled;
            overlayImage.fillMethod = Image.FillMethod.Horizontal;
            overlayImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            overlayImage.fillAmount = 0f;
            overlayImage.color = new Color(0.62f, 0.62f, 0.62f, 0.78f);

            Image referenceImage = healthBar.GetComponent<Image>();
            if (referenceImage == null)
                referenceImage = healthBar.GetComponentInChildren<Image>(true);

            if (referenceImage != null)
            {
                overlayImage.sprite = referenceImage.sprite;
                overlayImage.material = referenceImage.material;
                overlayImage.preserveAspect = referenceImage.preserveAspect;
            }

            overlayRect.SetSiblingIndex(referenceRect.GetSiblingIndex() + 1);
            overlayObject.SetActive(false);

            GameObject textObject = new GameObject(
                "HaldorExpansion_CestusShieldText",
                typeof(RectTransform));

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(overlayRect, false);
            textRect.anchorMin = new Vector2(0f, 0.5f);
            textRect.anchorMax = new Vector2(0f, 0.5f);
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

            Text referenceText = healthBar.GetComponent<Text>();
            if (referenceText == null)
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
            CestusShieldTextRect = textRect;
            CestusShieldOverlayLoggedMissing = false;

            DebugLog("[HaldorExpansion] Cestus segmented HUD with text created");
        }

        private static Component FindReferenceHealthBarComponent(Hud hud)
        {
            FieldInfo[] fields = typeof(Hud).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            Component fallback = null;

            foreach (FieldInfo field in fields)
            {
                if (field.FieldType == null || field.FieldType.Name != "GuiBar")
                    continue;

                Component bar = field.GetValue(hud) as Component;
                if (bar == null)
                    continue;

                string name = field.Name.ToLowerInvariant();

                if (!name.Contains("health") && !name.Contains("hp"))
                    continue;

                if (name.Contains("fast"))
                    return bar;

                if (fallback == null)
                    fallback = bar;
            }

            return fallback;
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