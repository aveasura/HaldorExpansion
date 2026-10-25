using System;
using System.Reflection;
using HaldorExpansion.Configuration;
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
        private static readonly int CategoryCount = Enum.GetValues(typeof(ShopCategory)).Length;
        private GameObject tooltipPrefab;
        private const float ScreenMargin = 5f;
        private static readonly FieldInfo SellButtonField = AccessTools.Field(typeof(StoreGui), "m_sellButton");
        private readonly RectTransform panel;
        private readonly Canvas canvas;
        private readonly RectTransform rootRect;
        private readonly RectTransform[] holderRects = new RectTransform[CategoryCount];
        private readonly RectTransform[] buttonRects = new RectTransform[CategoryCount];
        private readonly Button[] buttons = new Button[CategoryCount];
        private readonly Image[] icons = new Image[CategoryCount];
        private readonly Vector3[] corners = new Vector3[4];
        private readonly ColorBlock nativeColors;
        private readonly RectTransform nativeHolderRect;
        private readonly RectTransform nativeSellRect;
        private readonly GameObject root;
        private ShopCategory selectedCategory;
        internal StoreGui Gui { get; }

        internal static HaldorTabsView Create(StoreGui gui)
        {
            var panel = gui.m_rootPanel.transform as RectTransform;
            var sell = SellButtonField != null ? SellButtonField.GetValue(gui) as Button : null;
            if (panel == null || sell == null || !(sell.transform is RectTransform))
                throw new InvalidOperationException("Shop root panel or native sell-button frame is missing.");
            return new HaldorTabsView(gui, panel, sell);
        }

        private HaldorTabsView(StoreGui gui, RectTransform panel, Button sell)
        {
            Gui = gui;
            this.panel = panel;
            canvas = panel.GetComponentInParent<Canvas>();
            nativeColors = sell.colors;
            nativeSellRect = (RectTransform)sell.transform;
            nativeHolderRect = FindNativeHolder(nativeSellRect, panel);
            root = new GameObject("aveasura_he_haldor_tabs", typeof(RectTransform));
            root.SetActive(false);
            rootRect = (RectTransform)root.transform;
            try
            {
                rootRect.SetParent(panel, false);
                rootRect.SetAsFirstSibling();
                rootRect.anchorMin = rootRect.anchorMax = new Vector2(0f, 1f);
                rootRect.pivot = new Vector2(0f, 1f);
                rootRect.anchoredPosition = Vector2.zero;
                rootRect.sizeDelta = Vector2.zero;
                tooltipPrefab = ResolveTooltipPrefab(sell);
                for (int i = 0; i < buttons.Length; i++) CreateHolder(i, sell);
                root.SetActive(true);
                UpdateLayout();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private static RectTransform FindNativeHolder(RectTransform sellRect, RectTransform shopPanel)
        {
            var holder = sellRect.parent as RectTransform;
            if (holder == null || holder == shopPanel || holder.GetComponent<Image>() == null)
                throw new InvalidOperationException("Native SellPanel Image not found.");
            var nestedButtons = holder.GetComponentsInChildren<Button>(true);
            if (nestedButtons.Length != 1 || nestedButtons[0].transform != sellRect)
                throw new InvalidOperationException("Unexpected native SellPanel button hierarchy.");
            return holder;
        }

        private void CreateHolder(int index, Button nativeSell)
        {
            var holderGo = new GameObject("Holder_" + index, typeof(RectTransform), typeof(Image));
            holderGo.SetActive(false);
            var holderRect = (RectTransform)holderGo.transform;
            holderRect.SetParent(rootRect, false);
            holderRect.anchorMin = holderRect.anchorMax = new Vector2(0f, 1f);
            holderRect.pivot = new Vector2(0f, 1f);
            holderRects[index] = holderRect;
            CopyImage(nativeHolderRect.GetComponent<Image>(), holderGo.GetComponent<Image>());
            CreateButton(index, nativeSell, holderRect);
            holderGo.SetActive(true);
        }

        private void CreateButton(int index, Button nativeSell, RectTransform parent)
        {
            ShopCategory category = (ShopCategory)index;
            var go = new GameObject(category.ToString(), typeof(RectTransform), typeof(Image), typeof(Button));
            go.SetActive(false);
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            buttonRects[index] = rect;

            var background = go.GetComponent<Image>();
            CopyImage(nativeSell.GetComponent<Image>(), background);
            background.raycastTarget = true;

            Sprite sprite = CategoryIcon(category);
            if (sprite != null)
            {
                var iconGo = new GameObject("CategoryIcon", typeof(RectTransform), typeof(Image));
                var iconRect = (RectTransform)iconGo.transform;
                iconRect.SetParent(rect, false);
                iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);
                iconRect.anchoredPosition = Vector2.zero;
                float iconSize = Mathf.Min(nativeSellRect.rect.width, nativeSellRect.rect.height) * 0.6f;
                iconRect.sizeDelta = new Vector2(iconSize, iconSize);
                icons[index] = iconGo.GetComponent<Image>();
                icons[index].sprite = sprite;
                icons[index].preserveAspect = true;
                icons[index].raycastTarget = false;
            }

            var button = go.GetComponent<Button>();
            buttons[index] = button;
            button.targetGraphic = background;
            button.colors = nativeColors;
            button.transition = Selectable.Transition.ColorTint;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => HaldorTabsController.Select(category));

            var sourceSfx = nativeSell.GetComponent<ButtonSfx>() ?? Gui.m_buyButton.GetComponent<ButtonSfx>();
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

            var tooltip = go.AddComponent<UITooltip>();
            tooltip.m_tooltipPrefab = tooltipPrefab;
            // Native templates are not guaranteed to contain Topic. Text is the common field.
            // Keep the localization key so tooltips follow a language change without reopening.
            tooltip.m_topic = "";
            tooltip.m_text = UiTextIds.ShopCategoryKey(category);

            go.SetActive(true);
        }

        private static TMP_Text CreateText(Transform parent, TMP_Text source, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            // Assign the game font before TMP is enabled; avoid its missing default-font lookup.
            go.SetActive(false);
            var text = go.AddComponent<TextMeshProUGUI>();
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(4f, 2f);
            rect.offsetMax = new Vector2(-4f, -2f);
            if (source != null)
            {
                text.font = source.font;
                text.fontSharedMaterial = source.fontSharedMaterial;
                text.fontSize = source.fontSize;
            }
            else text.fontSize = 18f;

            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 10f;
            text.fontSizeMax = source != null ? source.fontSize : 18f;
            text.raycastTarget = false;
            go.SetActive(true);
            return text;
        }

        private GameObject ResolveTooltipPrefab(Button nativeSell)
        {
            // Search children too; mods/game versions may move the tooltip component.
            foreach (var tooltip in Gui.GetComponentsInChildren<UITooltip>(true))
                if (tooltip.m_tooltipPrefab != null)
                    return tooltip.m_tooltipPrefab;
            if (Gui.m_listElement != null)
                foreach (var tooltip in Gui.m_listElement.GetComponentsInChildren<UITooltip>(true))
                    if (tooltip.m_tooltipPrefab != null)
                        return tooltip.m_tooltipPrefab;

            // No native template: a small owned template using the shop's own frame/font.
            var prefab = new GameObject("CategoryTooltip", typeof(RectTransform));
            prefab.SetActive(false);
            prefab.transform.SetParent(rootRect, false);
            var popup = new GameObject("Popup", typeof(RectTransform), typeof(Image));
            var popupRect = (RectTransform)popup.transform;
            popupRect.SetParent(prefab.transform, false);
            popupRect.pivot = new Vector2(0f, 1f);
            popupRect.sizeDelta = new Vector2(240f, 48f);
            CopyImage(nativeHolderRect.GetComponent<Image>(), popup.GetComponent<Image>());
            // Item-name font supports the current language. button-title font may lack Cyrillic.
            var itemName = Gui.m_listElement != null ? Gui.m_listElement.transform.Find("name") : null;
            var textSource = itemName != null ? itemName.GetComponent<TMP_Text>() : null;
            CreateText(popupRect, textSource ?? nativeSell.GetComponentInChildren<TMP_Text>(true), "Text");
            return prefab;
        }

        private static void CopyImage(Image source, Image target)
        {
            target.sprite = source.sprite;
            target.type = source.type;
            target.color = source.color;
            target.material = source.material;
            target.preserveAspect = source.preserveAspect;
            target.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
            target.fillCenter = source.fillCenter;
            target.fillMethod = source.fillMethod;
            target.fillOrigin = source.fillOrigin;
            target.fillAmount = source.fillAmount;
            target.fillClockwise = source.fillClockwise;
            target.useSpriteMesh = source.useSpriteMesh;
            target.raycastTarget = false;
        }

        private static Sprite CategoryIcon(ShopCategory category)
        {
            string prefab = null;
            switch (category)
            {
                case ShopCategory.Equipment: return EmbeddedIconLoader.LoadUiIcon("brisingamen.png");
                case ShopCategory.Wood: prefab = "Wood"; break;
                case ShopCategory.Metals: prefab = "CopperOre"; break;
                case ShopCategory.Resources: prefab = "Sap"; break;
                case ShopCategory.Idols: prefab = HaldorIdolCatalog.AdamantineId; break;
                case ShopCategory.Other: prefab = "Ruby"; break;
            }

            if (prefab != null && ObjectDB.instance != null)
            {
                var item = ObjectDB.instance.GetItemPrefab(prefab)?.GetComponent<ItemDrop>();
                var shared = item?.m_itemData?.m_shared;
                if (shared?.m_icons != null && shared.m_icons.Length > 0 && shared.m_icons[0] != null)
                    return shared.m_icons[0];
            }

            return EmbeddedIconLoader.LoadUiIcon("shop_" +
                                                 (category == ShopCategory.Wood || category == ShopCategory.Metals
                                                     ? "resources"
                                                     : category.ToString().ToLowerInvariant()) + ".png");
        }

        internal void UpdateLayout()
        {
            if (panel == null || rootRect == null || nativeHolderRect == null || nativeSellRect == null) return;

            nativeHolderRect.GetWorldCorners(corners);
            Vector3 holderBottomLeft = panel.InverseTransformPoint(corners[0]);
            Vector3 holderTopRight = panel.InverseTransformPoint(corners[2]);
            float holderLeft = holderBottomLeft.x;
            float holderTop = holderTopRight.y;
            float holderBottom = holderBottomLeft.y;
            float holderWidth = holderTopRight.x - holderLeft;
            float holderHeight = holderTop - holderBottom;
            if (holderWidth <= 1f || holderHeight <= 1f) return;

            // Include the native button if it protrudes beyond its decorative holder.
            // Both copied images must have the same edge gap as the untouched coin frame.
            nativeSellRect.GetWorldCorners(corners);
            float buttonBottom = panel.InverseTransformPoint(corners[0]).y;
            float buttonTop = panel.InverseTransformPoint(corners[2]).y;
            float frameTop = Mathf.Max(holderTop, buttonTop);
            float frameBottom = Mathf.Min(holderBottom, buttonBottom);
            float frameTopOffset = frameTop - holderTop;
            float overflowBelow = Mathf.Max(0f, panel.rect.yMin - frameBottom);
            float firstHolderTop = panel.rect.yMax + overflowBelow;
            int visibleCount = 0;
            for (int i = 0; i < holderRects.Length; i++)
            {
                bool visible = TradingUiConfiguration.IsCategoryEnabled((ShopCategory)i);
                holderRects[i].gameObject.SetActive(visible);
                if (visible) visibleCount++;
            }

            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            Vector2 topScreen = RectTransformUtility.WorldToScreenPoint(camera,
                panel.TransformPoint(new Vector3(holderLeft, firstHolderTop, 0f)));
            Vector2 stepScreen = RectTransformUtility.WorldToScreenPoint(camera,
                panel.TransformPoint(new Vector3(holderLeft + holderWidth, firstHolderTop - holderHeight, 0f)));
            float pixelsPerHeight = Mathf.Max(1f, Mathf.Abs(topScreen.y - stepScreen.y)) / holderHeight;
            float pixelsPerWidth = Mathf.Max(1f, Mathf.Abs(stepScreen.x - topScreen.x)) / holderWidth;

            // The vanilla frame is the fixed eighth slot. Divide the whole strip
            // evenly, including the gap between the last category and that frame.
            float columnTop = firstHolderTop - Mathf.Max(0f,
                topScreen.y - (Screen.height - ScreenMargin)) / pixelsPerHeight;
            float step = HaldorTabsLayout.CalculateStep(visibleCount, columnTop - frameTop);

            float pixelWidth = holderWidth * pixelsPerWidth;
            float horizontalPixels = Mathf.Max(0f, ScreenMargin - topScreen.x);
            horizontalPixels -= Mathf.Max(0f,
                topScreen.x + pixelWidth + horizontalPixels - (Screen.width - ScreenMargin));
            holderLeft += horizontalPixels / pixelsPerWidth;
            if (!root.activeSelf) root.SetActive(true);

            Vector2 holderSize = nativeHolderRect.rect.size;
            Vector2 buttonSize = nativeSellRect.rect.size;
            Vector2 holderBasePosition = new Vector2(holderLeft - panel.rect.xMin,
                columnTop - frameTopOffset - panel.rect.yMax);
            Vector3 holderScale = new Vector3(
                holderWidth / holderSize.x, holderHeight / holderSize.y, 1f);
            Vector2 nativeButtonAnchored = nativeSellRect.anchoredPosition;
            Vector2 nativeButtonAnchorMin = nativeSellRect.anchorMin;
            Vector2 nativeButtonAnchorMax = nativeSellRect.anchorMax;
            Vector2 nativeButtonPivot = nativeSellRect.pivot;

            int slot = 0;
            foreach (ShopCategory category in HaldorTabsLayout.CategoryOrder)
            {
                int i = (int)category;
                if (!TradingUiConfiguration.IsCategoryEnabled(category)) continue;
                RectTransform holderRect = holderRects[i];
                Vector2 holderPosition = new Vector2(holderBasePosition.x,
                    holderBasePosition.y - slot * step);
                slot++;
                if (holderRect.anchoredPosition != holderPosition) holderRect.anchoredPosition = holderPosition;
                if (holderRect.sizeDelta != holderSize) holderRect.sizeDelta = holderSize;
                if (holderRect.localScale != holderScale) holderRect.localScale = holderScale;

                RectTransform buttonRect = buttonRects[i];
                buttonRect.anchorMin = nativeButtonAnchorMin;
                buttonRect.anchorMax = nativeButtonAnchorMax;
                buttonRect.pivot = nativeButtonPivot;
                if (buttonRect.anchoredPosition != nativeButtonAnchored)
                    buttonRect.anchoredPosition = nativeButtonAnchored;
                if (buttonRect.sizeDelta != buttonSize) buttonRect.sizeDelta = buttonSize;
            }
        }

        internal void Cycle(ShopCategory current, int direction)
        {
            var order = HaldorTabsLayout.CategoryOrder;
            int index = Array.IndexOf(order, current);
            for (int i = 0; i < order.Length; i++)
            {
                index = (index + direction + order.Length) % order.Length;
                ShopCategory category = order[index];
                if (!TradingUiConfiguration.IsCategoryEnabled(category)) continue;
                buttons[(int)category].onClick.Invoke();
                return;
            }
        }

        internal void Select(ShopCategory category)
        {
            selectedCategory = category;
            for (int i = 0; i < buttons.Length; i++)
            {
                bool selected = i == (int)selectedCategory;
                var tint = nativeColors;
                if (selected)
                {
                    tint.normalColor = new Color(1f, 0.77f, 0.31f, 1f);
                    tint.highlightedColor = new Color(1f, 0.88f, 0.50f, 1f);
                    tint.selectedColor = tint.normalColor;
                }

                buttons[i].colors = tint;
                Color color = selected ? new Color(1f, 0.88f, 0.51f, 1f) : Color.white;
                if (icons[i] != null) icons[i].color = color;
            }
        }

        public void Dispose()
        {
            if (root != null)
            {
                root.SetActive(false);
                Object.Destroy(root);
            }
        }
    }
}