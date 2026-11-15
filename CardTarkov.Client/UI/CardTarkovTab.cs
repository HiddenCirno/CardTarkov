using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EFT;
using EFT.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardTarkov.Client.UI
{
    /// <summary>
    /// 在背包界面顶部注入“棋牌室”Tab。
    /// 做法与 TarkFX / OperatorTraits 相同：克隆 Skills Tab、替换本地化、
    /// 插入 _tabDictionary，让 InventoryScreen.Show 内部重建 TabGroup 时纳入。
    ///
    /// <para>补丁必须是 Prefix —— TabGroup 是在 Show 方法体里构造的。</para>
    /// </summary>
    internal static class CardTarkovTab
    {
        internal const string TabObjectName = "CardTarkovTab";
        internal const string PanelObjectName = "CardTarkovPanel";

        internal const EInventoryTab TabKey = (EInventoryTab)1001;

        internal static bool TryAttach(InventoryScreen screen)
        {
            if (screen == null)
                return false;

            if (FindDeep(screen.transform, TabObjectName) != null)
                return true;

            if (screen._tabDictionary == null ||
                !screen._tabDictionary.TryGetValue(EInventoryTab.Skills, out Tab skillsTab) ||
                skillsTab == null || screen._tasksScreen == null)
                return false;

            GameObject tabObject = Object.Instantiate(
                skillsTab.gameObject, skillsTab.transform.parent, false);

            tabObject.name = TabObjectName;
            tabObject.SetActive(true);
            tabObject.transform.SetAsLastSibling();

            Tab clonedTab = tabObject.GetComponent<Tab>();
            RemoveCopiedLocalization(tabObject, clonedTab);
            SetAllLabels(tabObject, Strings.TabName);

            TMP_Text template = tabObject
                .GetComponentsInChildren<TMP_Text>(true)
                .FirstOrDefault();

            GameObject panel = CreatePanel(screen, template);

            clonedTab.Init(new CardTabController(panel));
            clonedTab.UpdateVisual(false);
            clonedTab.SetInteractable(true);

            RegisterTab(screen, skillsTab, clonedTab);
            InstallPositioner(skillsTab.transform.parent, tabObject.transform);

            Plugin.Log?.LogInfo("[CardTarkov] 已注入棋牌室 Tab。");
            return true;
        }

        internal static void ShutdownForScreen(InventoryScreen screen)
        {
            if (screen == null)
                return;

            Transform panelTransform = FindDeep(screen.transform, PanelObjectName);
            if (panelTransform == null)
                return;

            CardTarkovPanel panel = panelTransform.GetComponent<CardTarkovPanel>();
            panel?.ShutdownGame("inventory-closed");
        }

        private static void RegisterTab(
            InventoryScreen screen, Tab skillsTab, Tab cardTab)
        {
            var tabs = new Dictionary<EInventoryTab, Tab>();

            foreach (KeyValuePair<EInventoryTab, Tab> entry in screen._tabDictionary)
            {
                if (entry.Value == skillsTab)
                    tabs.Add(TabKey, cardTab);

                tabs.Add(entry.Key, entry.Value);
            }

            screen._tabDictionary = tabs;
        }

        private static void InstallPositioner(Transform tabRow, Transform tab)
        {
            CardTabPositioner positioner =
                tabRow.gameObject.GetComponent<CardTabPositioner>() ??
                tabRow.gameObject.AddComponent<CardTabPositioner>();

            positioner.Initialize(tabRow, tab);
        }

        private static void SetAllLabels(GameObject root, string value)
        {
            bool any = false;

            foreach (TMP_Text label in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (string.IsNullOrWhiteSpace(label.text))
                    continue;

                label.text = value;
                label.enableAutoSizing = true;
                label.fontSizeMin = 10f;
                label.fontSizeMax = label.fontSize;
                label.enableWordWrapping = false;
                any = true;
            }

            if (!any)
            {
                TMP_Text first = root.GetComponentInChildren<TMP_Text>(true);
                if (first != null)
                {
                    first.text = value;
                    first.enableAutoSizing = true;
                    first.fontSizeMin = 10f;
                    first.enableWordWrapping = false;
                }
            }
        }

        private static void RemoveCopiedLocalization(GameObject tabObject, Tab tab)
        {
            foreach (LocalizedText localizedText in
                     tabObject.GetComponentsInChildren<LocalizedText>(true))
            {
                Object.DestroyImmediate(localizedText);
            }

            tab.LocalizedText = null;
        }

        private static GameObject CreatePanel(InventoryScreen screen, TMP_Text template)
        {
            Transform tasksScreen = screen._tasksScreen.transform;

            GameObject panel = new GameObject(
                PanelObjectName,
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(Image));

            panel.transform.SetParent(tasksScreen.parent, false);
            panel.transform.SetSiblingIndex(tasksScreen.GetSiblingIndex());

            RectTransform rect = (RectTransform)panel.transform;
            RectTransform nativeContent = screen._skillsAndMasteringScreen.transform as RectTransform;

            if (nativeContent != null)
            {
                rect.anchorMin = nativeContent.anchorMin;
                rect.anchorMax = nativeContent.anchorMax;
                rect.pivot = nativeContent.pivot;
                rect.anchoredPosition = nativeContent.anchoredPosition;
                rect.sizeDelta = nativeContent.sizeDelta;
            }

            Image background = panel.GetComponent<Image>();
            background.color = new Color(0.055f, 0.062f, 0.062f, 0.98f);
            background.raycastTarget = true;

            CardTarkovPanel controller = panel.AddComponent<CardTarkovPanel>();
            controller.Build(screen, template);

            panel.SetActive(false);
            panel.transform.SetAsLastSibling();
            return panel;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                    return child;
            }

            return null;
        }

        private sealed class CardTabController : ITabController
        {
            private readonly GameObject _panel;

            internal CardTabController(GameObject panel)
            {
                _panel = panel;
            }

            public void Show()
            {
                _panel.SetActive(true);
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(
                    _panel.transform as RectTransform);

                _panel.GetComponent<CardTarkovPanel>()?.OnPanelShown();
            }

            public Task<bool> TryHide()
            {
                // 切走页签 = 直接销毁牌局实例，回到设置页
                _panel.GetComponent<CardTarkovPanel>()?.ShutdownGame("tab-hidden");
                _panel.SetActive(false);
                return Task.FromResult(true);
            }
        }
    }
}
