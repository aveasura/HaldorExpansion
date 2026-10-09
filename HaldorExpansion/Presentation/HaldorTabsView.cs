using System;
using System.Reflection;
using HaldorExpansion.Trading;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HaldorExpansion.Presentation
{
    internal sealed class HaldorTabsView : IDisposable
    {
        private const float ScreenMargin = 5f;
        private static readonly FieldInfo SellButtonField = AccessTools.Field(typeof(StoreGui), "m_sellButton");
        private readonly RectTransform panel;
        private readonly Canvas canvas;
        private readonly RectTransform rootRect;
        private readonly RectTransform[] holderRects = new RectTransform[4];
        private readonly RectTransform[] buttonRects = new RectTransform[4];
        private readonly Button[] buttons = new Button[4];
        private readonly Image[] icons = new Image[4];
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

            var iconGo = new GameObject("CategoryIcon", typeof(RectTransform), typeof(Image));
            var iconRect = (RectTransform)iconGo.transform;
            iconRect.SetParent(rect, false);
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            float iconSize = Mathf.Min(nativeSellRect.rect.width, nativeSellRect.rect.height) * 0.6f;
            iconRect.sizeDelta = new Vector2(iconSize, iconSize);
            icons[index] = iconGo.GetComponent<Image>();
            icons[index].sprite = CategoryIcon(category);
            icons[index].preserveAspect = true;
            icons[index].raycastTarget = false;

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

            var listTooltip = Gui.m_listElement.GetComponent<UITooltip>();
            var nativeTooltip = nativeSell.GetComponent<UITooltip>();
            var tooltipPrefab = nativeTooltip != null ? nativeTooltip.m_tooltipPrefab : null;
            if (tooltipPrefab == null && listTooltip != null) tooltipPrefab = listTooltip.m_tooltipPrefab;
            if (tooltipPrefab != null)
            {
                var tooltip = go.AddComponent<UITooltip>();
                tooltip.m_tooltipPrefab = tooltipPrefab;
                tooltip.m_topic =
                    Localization.instance.Localize("$aveasura_he_shop_" + category.ToString().ToLowerInvariant());
                tooltip.m_text = "";
            }

            go.SetActive(true);
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
                case ShopCategory.Equipment: prefab = "SwordBronze"; break;
                case ShopCategory.Resources: prefab = "Wood"; break;
                case ShopCategory.Other: prefab = "Ruby"; break;
            }

            if (prefab != null && ObjectDB.instance != null)
            {
                var item = ObjectDB.instance.GetItemPrefab(prefab)?.GetComponent<ItemDrop>();
                var shared = item?.m_itemData?.m_shared;
                if (shared?.m_icons != null && shared.m_icons.Length > 0 && shared.m_icons[0] != null)
                    return shared.m_icons[0];
            }

            return EmbeddedIconLoader.LoadUiIcon("shop_" + category.ToString().ToLowerInvariant() + ".png");
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

            float overflowBelow = Mathf.Max(0f, panel.rect.yMin - holderBottom);
            float firstHolderTop = panel.rect.yMax + overflowBelow;
            float totalHeight = holderHeight * holderRects.Length;
            float lastHolderBottom = firstHolderTop - totalHeight;

            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            Vector3 upperLeft = panel.TransformPoint(new Vector3(holderLeft, firstHolderTop, 0f));
            Vector3 lowerRight = panel.TransformPoint(new Vector3(holderLeft + holderWidth, lastHolderBottom, 0f));
            Vector2 topScreen = RectTransformUtility.WorldToScreenPoint(camera, upperLeft);
            Vector2 bottomScreen = RectTransformUtility.WorldToScreenPoint(camera, lowerRight);
            if (topScreen.x < ScreenMargin || bottomScreen.x > Screen.width - ScreenMargin ||
                topScreen.y > Screen.height - ScreenMargin || bottomScreen.y < ScreenMargin)
            {
                if (root.activeSelf) root.SetActive(false);
                return;
            }

            if (!root.activeSelf) root.SetActive(true);

            Vector2 holderSize = nativeHolderRect.rect.size;
            Vector2 buttonSize = nativeSellRect.rect.size;
            Vector2 holderBasePosition = new Vector2(holderLeft - panel.rect.xMin, firstHolderTop - panel.rect.yMax);
            Vector2 nativeButtonAnchored = nativeSellRect.anchoredPosition;
            Vector2 nativeButtonAnchorMin = nativeSellRect.anchorMin;
            Vector2 nativeButtonAnchorMax = nativeSellRect.anchorMax;
            Vector2 nativeButtonPivot = nativeSellRect.pivot;

            for (int i = 0; i < holderRects.Length; i++)
            {
                RectTransform holderRect = holderRects[i];
                Vector2 holderPosition = new Vector2(holderBasePosition.x, holderBasePosition.y - i * holderHeight);
                if (holderRect.anchoredPosition != holderPosition) holderRect.anchoredPosition = holderPosition;
                if (holderRect.sizeDelta != holderSize) holderRect.sizeDelta = holderSize;

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
            buttons[((int)current + direction + buttons.Length) % buttons.Length].onClick.Invoke();
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
                icons[i].color = selected ? new Color(1f, 0.88f, 0.51f, 1f) : Color.white;
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