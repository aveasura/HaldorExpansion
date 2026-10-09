using System;
using System.Reflection;
using HaldorExpansion.Trading;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HaldorExpansion.Presentation
{
    internal sealed class HaldorTabsView : IDisposable
    {
        private const float BandHeight = 42f;
        private static readonly FieldInfo BaseSize = AccessTools.Field(typeof(StoreGui), "m_itemlistBaseSize");
        private readonly float originalBaseSize;
        private readonly ScrollRect scroll;
        private readonly ScrollRect.ScrollbarVisibility horizontalVisibility, verticalVisibility;
        private readonly RectTransform viewport;
        private readonly Vector2 originalOffsetMax;
        private readonly GameObject root;
        private readonly Button[] buttons = new Button[4];
        private readonly TMP_Text[] labels = new TMP_Text[4];
        private readonly ColorBlock colors;
        internal StoreGui Gui { get; }

        internal static HaldorTabsView Create(StoreGui gui)
        {
            var scroll = gui.m_listRoot.GetComponentInParent<ScrollRect>();
            var viewport = scroll != null ? scroll.viewport : null;
            if (viewport == null) viewport = gui.m_listRoot.parent as RectTransform;
            var image = gui.m_buyButton.targetGraphic as Image;
            var text = gui.m_buyButton.GetComponentInChildren<TMP_Text>(true);
            if (viewport == null || viewport.parent == null || image == null || text == null)
                throw new InvalidOperationException("Shop list viewport or native button style is missing.");
            return new HaldorTabsView(gui, scroll, viewport, image, text);
        }

        private HaldorTabsView(StoreGui gui, ScrollRect scroll, RectTransform viewport, Image sourceImage,
            TMP_Text sourceText)
        {
            Gui = gui;
            this.viewport = viewport;
            this.scroll = scroll;
            originalBaseSize = (float)BaseSize.GetValue(gui);
            if (scroll != null)
            {
                horizontalVisibility = scroll.horizontalScrollbarVisibility;
                verticalVisibility = scroll.verticalScrollbarVisibility;
            }

            originalOffsetMax = viewport.offsetMax;
            colors = gui.m_buyButton.colors;
            root = new GameObject("aveasura_he_haldor_tabs", typeof(RectTransform));
            root.SetActive(false);
            try
            {
                var rect = (RectTransform)root.transform;
                rect.SetParent(viewport.parent, false);
                rect.anchorMin = new Vector2(viewport.anchorMin.x, viewport.anchorMax.y);
                rect.anchorMax = viewport.anchorMax;
                rect.pivot = new Vector2(0.5f, 1f);
                rect.offsetMin = new Vector2(viewport.offsetMin.x, originalOffsetMax.y - BandHeight + 6f);
                rect.offsetMax = originalOffsetMax;
                for (int i = 0; i < buttons.Length; i++) CreateButton(i, sourceImage, sourceText);
                // Expansion mode drives viewport offsets during Unity layout rebuilds.
                if (scroll != null)
                {
                    if (horizontalVisibility == ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport)
                        scroll.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
                    if (verticalVisibility == ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport)
                        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
                }

                BaseSize.SetValue(gui, Mathf.Max(0f, originalBaseSize - BandHeight));
                viewport.offsetMax = originalOffsetMax - new Vector2(0f, BandHeight);
                root.SetActive(true);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void CreateButton(int index, Image sourceImage, TMP_Text sourceText)
        {
            var go = new GameObject(((ShopCategory)index).ToString(), typeof(RectTransform), typeof(Image),
                typeof(Button));
            var rect = (RectTransform)go.transform;
            rect.SetParent(root.transform, false);
            rect.anchorMin = new Vector2(index / 4f, 0f);
            rect.anchorMax = new Vector2((index + 1) / 4f, 1f);
            rect.offsetMin = new Vector2(index == 0 ? 0f : 2f, 0f);
            rect.offsetMax = new Vector2(index == 3 ? 0f : -2f, 0f);
            var image = go.GetComponent<Image>();
            image.sprite = sourceImage.sprite;
            image.type = sourceImage.type;
            image.color = sourceImage.color;
            image.material = sourceImage.material;
            image.pixelsPerUnitMultiplier = sourceImage.pixelsPerUnitMultiplier;
            var button = go.GetComponent<Button>();
            buttons[index] = button;
            button.targetGraphic = image;
            button.colors = colors;
            button.transition = Selectable.Transition.ColorTint;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            ShopCategory category = (ShopCategory)index;
            button.onClick.AddListener(() => HaldorTabsController.Select(category));

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            var textRect = (RectTransform)textGo.transform;
            textRect.SetParent(rect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(4f, 0f);
            textRect.offsetMax = new Vector2(-4f, 0f);
            var label = textGo.GetComponent<TextMeshProUGUI>();
            labels[index] = label;
            label.font = sourceText.font;
            label.fontSharedMaterial = sourceText.fontSharedMaterial;
            label.fontStyle = sourceText.fontStyle;
            label.color = sourceText.color;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 10f;
            label.fontSizeMax = Mathf.Min(sourceText.fontSize, 18f);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            label.text = Localization.instance.Localize("$aveasura_he_shop_" + category.ToString().ToLowerInvariant());

            // Reuse the shop's own click/hover sounds without copying its purchase callback.
            var sourceSfx = Gui.m_buyButton.GetComponent<ButtonSfx>();
            if (sourceSfx != null)
            {
                var sfx = go.AddComponent<ButtonSfx>();
                sfx.m_sfxPrefab = sourceSfx.m_sfxPrefab;
                sfx.m_sfxPrefabVibrationOnly = sourceSfx.m_sfxPrefabVibrationOnly;
                sfx.m_selectSfxPrefab = sourceSfx.m_selectSfxPrefab;
                sfx.m_selectSfxPrefabVibrationOnly = sourceSfx.m_selectSfxPrefabVibrationOnly;
                sfx.m_enterSfxPrefab = sourceSfx.m_enterSfxPrefab;
                sfx.m_enterSfxPrefabVibrationOnly = sourceSfx.m_enterSfxPrefabVibrationOnly;
            }
        }

        internal void Cycle(ShopCategory current, int direction)
        {
            buttons[((int)current + direction + buttons.Length) % buttons.Length].onClick.Invoke();
        }

        internal void Select(ShopCategory category)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                bool selected = i == (int)category;
                var tint = colors;
                if (selected) tint.normalColor = tint.highlightedColor;
                buttons[i].colors = tint;
                labels[i].color = selected ? new Color(1f, 0.82f, 0.35f) : Color.white;
            }
        }

        public void Dispose()
        {
            if (scroll != null)
            {
                scroll.horizontalScrollbarVisibility = horizontalVisibility;
                scroll.verticalScrollbarVisibility = verticalVisibility;
            }

            if (Gui != null) BaseSize.SetValue(Gui, originalBaseSize);
            if (viewport != null) viewport.offsetMax = originalOffsetMax;
            if (root != null)
            {
                root.SetActive(false);
                Object.Destroy(root);
            }
        }
    }
}