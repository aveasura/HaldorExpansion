using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HaldorExpansion.Presentation
{
    // The vanilla tooltip still owns visibility/lifetime. Only its overlong panel
    // is replaced, preserving the original text/fonts and leaving the prefab alone.
    internal sealed class LongItemTooltipView
    {
        internal UITooltip Owner { get; private set; }
        internal RectTransform OriginalPanel { get; private set; }
        private readonly GameObject tooltip;
        private readonly RectTransform ownerRect;
        private readonly RectTransform root;
        private readonly TMP_Text originalBody;
        private readonly TMP_Text originalTitle;
        private RectTransform panel;
        private TMP_Text title, body, hint;
        private RectTransform viewport, thumb, track;
        private TooltipLayoutMath.Layout layout;
        private float width, scaleX, scaleY, scrollOffset;
        private int screenWidth, screenHeight, wheelFrame = -1;
        private bool originalActive;
        private string lastBody, lastTitle;
        private readonly Vector3[] corners = new Vector3[4];

        private LongItemTooltipView(UITooltip owner, GameObject tooltipRoot, RectTransform original,
            TMP_Text sourceBody, TMP_Text sourceTitle)
        {
            Owner = owner;
            tooltip = tooltipRoot;
            root = tooltipRoot.transform as RectTransform;
            ownerRect = owner.transform as RectTransform;
            OriginalPanel = original;
            originalBody = sourceBody;
            originalTitle = sourceTitle;
        }

        internal static LongItemTooltipView TryCreate(UITooltip owner, GameObject tooltip)
        {
            if (owner == null || tooltip == null || tooltip.transform.childCount == 0) return null;
            RectTransform original = tooltip.transform.GetChild(0) as RectTransform;
            if (original == null || !(tooltip.transform is RectTransform) || !(owner.transform is RectTransform))
                return null;
            // A scrolling tooltip supplied by another UI mod owns this layout.
            if (tooltip.GetComponentInChildren<ScrollRect>(true) != null) return null;
            Transform bodyTransform = Utils.FindChild(original, "Text");
            Transform titleTransform = Utils.FindChild(original, "Topic");
            TMP_Text sourceBody = bodyTransform != null ? bodyTransform.GetComponent<TMP_Text>() : null;
            TMP_Text sourceTitle = titleTransform != null ? titleTransform.GetComponent<TMP_Text>() : null;
            if (sourceBody == null || sourceTitle == null) return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(original);
            var view = new LongItemTooltipView(owner, tooltip, original, sourceBody, sourceTitle);
            if (!view.NeedsReplacement()) return null;
            try
            {
                view.Build();
                view.Refresh();
                view.originalActive = original.gameObject.activeSelf;
                original.gameObject.SetActive(false);
                view.Place();
                return view;
            }
            catch
            {
                if (view.panel != null) Object.Destroy(view.panel.gameObject);
                throw;
            }
        }

        private bool NeedsReplacement()
        {
            // Hidden native layouts do not update their fitters. Rebuild while
            // temporarily active, then restore within this frame (no rendered flash).
            bool wasActive = OriginalPanel.gameObject.activeSelf;
            try
            {
                OriginalPanel.gameObject.SetActive(true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(OriginalPanel);
                return NativeLayoutOverflows();
            }
            finally
            {
                OriginalPanel.gameObject.SetActive(wasActive);
            }
        }

        private bool NativeLayoutOverflows()
        {
            Vector3 scale = root.lossyScale;
            // Valheim's ClampUIToScreen also works in overlay screen coordinates.
            // Avoid modifying rotated/degenerate canvases supplied by UI/VR mods.
            if (scale.x <= 0.001f || scale.y <= 0.001f ||
                Quaternion.Angle(root.rotation, Quaternion.identity) > 0.01f) return false;
            float nativeWidth = OriginalPanel.rect.width;
            if (nativeWidth <= 0f) return false;
            float bodyWidth = originalBody.rectTransform.rect.width;
            if (bodyWidth <= 0f) bodyWidth = Math.Max(1f, nativeWidth - 2f * TooltipLayoutMath.Padding);
            float content = originalBody.GetPreferredValues(originalBody.text, bodyWidth, Mathf.Infinity).y;
            float heading = originalTitle.GetPreferredValues(originalTitle.text, bodyWidth, Mathf.Infinity).y;
            float naturalHeight = Math.Max(OriginalPanel.rect.height,
                content + heading + 2f * TooltipLayoutMath.Padding + TooltipLayoutMath.Gap);
            return nativeWidth > TooltipLayoutMath.Width(Screen.width, scale.x, nativeWidth) + 0.5f ||
                   naturalHeight > TooltipLayoutMath.MaximumHeight(Screen.height, scale.y);
        }

        private void Build()
        {
            panel = NewRect("HE_LongItemTooltip", root);
            Image background = panel.gameObject.AddComponent<Image>();
            Image sourceBackground = OriginalPanel.GetComponent<Image>();
            if (sourceBackground == null) sourceBackground = OriginalPanel.GetComponentInChildren<Image>(true);
            if (sourceBackground != null)
            {
                background.sprite = sourceBackground.sprite;
                background.type = sourceBackground.type;
                background.color = sourceBackground.color;
                background.material = sourceBackground.material;
                background.pixelsPerUnitMultiplier = sourceBackground.pixelsPerUnitMultiplier;
            }
            else background.color = new Color(0.10f, 0.09f, 0.08f, 0.98f);

            background.raycastTarget = false;

            title = CopyText(originalTitle, "HE_Topic", panel);
            // Valheim font has no ellipsis glyph; avoid TMP warning and fallback.
            title.overflowMode = TextOverflowModes.Truncate;
            viewport = NewRect("HE_DescriptionViewport", panel);
            viewport.gameObject.AddComponent<RectMask2D>();
            body = CopyText(originalBody, "HE_Description", viewport);
            hint = CopyText(originalBody, "HE_ScrollHint", panel);
            hint.fontSize = Mathf.Min(16f, originalBody.fontSize * 0.8f);
            hint.color = new Color(0.82f, 0.76f, 0.63f, 1f);
            track = NewRect("HE_ScrollTrack", panel);
            Image trackImage = track.gameObject.AddComponent<Image>();
            trackImage.color = new Color(0.5f, 0.45f, 0.35f, 0.3f);
            trackImage.raycastTarget = false;
            thumb = NewRect("HE_ScrollThumb", track);
            Image thumbImage = thumb.gameObject.AddComponent<Image>();
            thumbImage.color = new Color(0.85f, 0.70f, 0.40f, 0.9f);
            thumbImage.raycastTarget = false;
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            return rect;
        }

        private static TMP_Text CopyText(TMP_Text source, string name, Transform parent)
        {
            TMP_Text copy = Object.Instantiate(source, parent, false);
            copy.name = name;
            copy.gameObject.SetActive(true);
            foreach (ContentSizeFitter fitter in copy.GetComponentsInChildren<ContentSizeFitter>(true))
                fitter.enabled = false;
            foreach (LayoutGroup group in copy.GetComponentsInChildren<LayoutGroup>(true)) group.enabled = false;
            foreach (Graphic graphic in copy.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            RectTransform rect = copy.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            copy.enableAutoSizing = false;
            copy.textWrappingMode = TextWrappingModes.Normal;
            copy.maskable = true;
            copy.overflowMode = TextOverflowModes.Overflow;
            copy.alignment = TextAlignmentOptions.TopLeft;
            return copy;
        }

        internal bool Alive => tooltip != null && Owner != null && panel != null && OriginalPanel != null;
        internal bool Visible => Alive && tooltip.activeInHierarchy;
        internal bool HasScroll => Visible && layout.MaxScroll > 0f;

        internal bool Refresh()
        {
            if (!Alive || !NeedsReplacement()) return false;
            lastBody = originalBody.text;
            lastTitle = originalTitle.text;
            body.text = lastBody;
            title.text = lastTitle;
            hint.text = Localization.instance.Localize("$he_tooltip_scroll");
            screenWidth = Screen.width;
            screenHeight = Screen.height;
            scaleX = root.lossyScale.x;
            scaleY = root.lossyScale.y;
            if (scaleX <= 0.001f || scaleY <= 0.001f) return false;
            width = TooltipLayoutMath.Width(screenWidth, scaleX, OriginalPanel.rect.width);
            float textWidth = Mathf.Max(1f, width - 2f * TooltipLayoutMath.Padding - TooltipLayoutMath.ScrollGutter);
            float header = title.GetPreferredValues(title.text, textWidth, Mathf.Infinity).y;
            float content = body.GetPreferredValues(body.text, textWidth, Mathf.Infinity).y;
            float footer = hint.GetPreferredValues(hint.text, textWidth, Mathf.Infinity).y;
            layout = TooltipLayoutMath.Measure(screenHeight, scaleY, header, content, footer);
            panel.sizeDelta = new Vector2(width, layout.Height);
            SetRect(title.rectTransform, TooltipLayoutMath.Padding, TooltipLayoutMath.Padding, textWidth,
                layout.HeaderHeight);
            float bodyTop = TooltipLayoutMath.Padding + layout.HeaderHeight + TooltipLayoutMath.Gap;
            SetRect(viewport, TooltipLayoutMath.Padding, bodyTop, textWidth, layout.BodyHeight);
            body.rectTransform.sizeDelta = new Vector2(textWidth, content);
            SetRect(hint.rectTransform, TooltipLayoutMath.Padding, bodyTop + layout.BodyHeight + TooltipLayoutMath.Gap,
                textWidth, layout.FooterHeight);
            hint.gameObject.SetActive(layout.MaxScroll > 0f);
            track.gameObject.SetActive(layout.MaxScroll > 0f);
            SetRect(track, width - TooltipLayoutMath.Padding - 4f, bodyTop, 4f, layout.BodyHeight);
            float thumbHeight = Mathf.Min(layout.BodyHeight,
                Mathf.Max(16f, layout.BodyHeight * layout.BodyHeight / Mathf.Max(content, 1f)));
            thumb.sizeDelta = new Vector2(4f, thumbHeight);
            Scroll(0f);
            return true;
        }

        private static void SetRect(RectTransform rect, float x, float top, float width, float height)
        {
            rect.anchoredPosition = new Vector2(x, -top);
            rect.sizeDelta = new Vector2(width, height);
        }

        internal bool Tick()
        {
            if (!Alive) return false;
            Vector3 scale = root.lossyScale;
            if (screenWidth != Screen.width || screenHeight != Screen.height ||
                Mathf.Abs(scale.x - scaleX) > 0.001f || Mathf.Abs(scale.y - scaleY) > 0.001f ||
                lastBody != originalBody.text || lastTitle != originalTitle.text)
                if (!Refresh())
                    return false;
            Place();
            if (!HasScroll) return true;
            if (PointerOnOwner((Vector2)ZInput.pointerPosition)) Wheel(ZInput.GetMouseScrollWheel());
            if (ZInput.GetKeyDown(KeyCode.PageDown)) Scroll(layout.BodyHeight * 0.8f);
            if (ZInput.GetKeyDown(KeyCode.PageUp)) Scroll(-layout.BodyHeight * 0.8f);
            return true;
        }

        internal bool PointerOnOwner(Vector2 point)
        {
            if (ownerRect == null) return false;
            return ownerRect.rect.Contains(ownerRect.InverseTransformPoint(new Vector3(point.x, point.y, 0f)));
        }

        internal void Wheel(float delta)
        {
            if (delta == 0f || wheelFrame == Time.frameCount || !HasScroll) return;
            wheelFrame = Time.frameCount;
            Scroll(-Math.Sign(delta) * 64f);
        }

        private void Scroll(float delta)
        {
            scrollOffset = TooltipLayoutMath.Scroll(scrollOffset, layout.MaxScroll, delta);
            body.rectTransform.anchoredPosition = new Vector2(0f, scrollOffset);
            float progress = layout.MaxScroll > 0f ? scrollOffset / layout.MaxScroll : 0f;
            thumb.anchoredPosition = new Vector2(0f, -(layout.BodyHeight - thumb.sizeDelta.y) * progress);
        }

        private void Place()
        {
            if (!Alive || ownerRect == null) return;
            ownerRect.GetWorldCorners(corners);
            var position = TooltipLayoutMath.Place(Screen.width, Screen.height, width * scaleX, layout.Height * scaleY,
                corners[0].x, corners[2].x, corners[2].y);
            // Set an absolute screen-space position each frame, never accumulate
            // edge corrections on yesterday's rect as ClampUIToScreen does.
            panel.position = new Vector3(position.Left, position.Top, root.position.z);
        }

        internal void Dispose()
        {
            if (OriginalPanel != null)
            {
                OriginalPanel.gameObject.SetActive(originalActive);
                if (originalActive)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(OriginalPanel);
                    Utils.ClampUIToScreen(OriginalPanel);
                }
            }

            if (panel != null)
            {
                panel.gameObject.SetActive(false);
                Object.Destroy(panel.gameObject);
            }

            panel = null;
        }
    }
}