using System.Collections.Generic;
using System.Linq;
using EFT.UI;
using UnityEngine;

namespace CardTarkov.Client.UI
{
    /// <summary>
    /// 顶部 Tab 行重排：原生 Tab 原先等距，多一个页签会重叠。
    ///
    /// <para>
    /// TarkFX 也有一套同样的布局组件。本组件用
    /// <see cref="DefaultExecutionOrderAttribute"/> 保证最后执行，
    /// 并把 "CardTarkovTab" 与 "TarkFXStockMarketTab" 都当作额外页签，
    /// 让两个 Mod 同时存在时尽量都能排开。
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(1000)]
    internal sealed class CardTabRowLayout : MonoBehaviour
    {
        private const string TarkFxTabName = "TarkFXStockMarketTab";

        private RectTransform _row;
        private float _originalRowWidth;

        private readonly List<RectTransform> _tabs = new List<RectTransform>();
        private readonly Dictionary<RectTransform, float> _originalCenters =
            new Dictionary<RectTransform, float>();
        private readonly Dictionary<RectTransform, float> _originalWidths =
            new Dictionary<RectTransform, float>();

        private int _lastTabSignature = -1;

        internal void Initialize()
        {
            _row = transform as RectTransform;

            if (_row != null)
                _originalRowWidth = _row.rect.width;

            _tabs.Clear();
            _tabs.AddRange(
                _row.GetComponentsInChildren<Tab>(true)
                    .Where(tab => tab != null && tab.transform.parent == _row)
                    .Select(tab => tab.transform as RectTransform)
                    .Where(rect => rect != null));

            _originalCenters.Clear();
            _originalWidths.Clear();

            foreach (RectTransform rect in _tabs)
            {
                _originalCenters[rect] = rect.localPosition.x;
                _originalWidths[rect] = rect.rect.width;
            }

            _lastTabSignature = Signature(_tabs);
            ApplyLayout();
        }

        private void LateUpdate()
        {
            if (_row == null)
                return;

            List<RectTransform> current = _row
                .GetComponentsInChildren<Tab>(true)
                .Where(tab => tab != null && tab.transform.parent == _row)
                .Select(tab => tab.transform as RectTransform)
                .Where(rect => rect != null)
                .ToList();

            if (Signature(current) != _lastTabSignature)
            {
                Initialize();
                return;
            }

            ApplyLayout();
        }

        private void ApplyLayout()
        {
            List<RectTransform> visible = _tabs
                .Where(rect => rect != null && rect.gameObject.activeSelf)
                .ToList();

            if (visible.Count == 0)
                return;

            List<RectTransform> natives = visible
                .Where(rect => !IsExtra(rect))
                .OrderBy(rect => _originalCenters[rect])
                .ToList();

            if (natives.Count < 2)
                return;

            float firstCenter = _originalCenters[natives[0]];
            float lastCenter = _originalCenters[natives[natives.Count - 1]];
            float nativeSpacing = (lastCenter - firstCenter) / (natives.Count - 1);

            if (nativeSpacing <= 0f)
                return;

            List<RectTransform> extras = visible
                .Where(IsExtra)
                .OrderBy(rect => _originalCenters[rect])
                .ToList();

            int extraCount = extras.Count;

            if (_row != null && _originalRowWidth > 0f)
            {
                _row.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Horizontal,
                    _originalRowWidth + nativeSpacing * extraCount);
            }

            for (int i = 0; i < natives.Count; i++)
            {
                float x = _originalCenters[natives[i]] + nativeSpacing * extraCount;
                SetX(natives[i], x);
            }

            for (int i = 0; i < extras.Count; i++)
            {
                SetX(extras[i], firstCenter + nativeSpacing * (i + 1));
            }
        }

        private void SetX(RectTransform rect, float x)
        {
            Vector3 position = rect.localPosition;
            position.x = x;
            rect.localPosition = position;

            rect.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                _originalWidths.TryGetValue(rect, out float width) ? width : rect.rect.width);
        }

        private static bool IsExtra(RectTransform rect) =>
            rect.name == CardTarkovTab.TabObjectName || rect.name == TarkFxTabName;

        private static int Signature(List<RectTransform> tabs)
        {
            int hash = 17;

            foreach (RectTransform rect in tabs)
                hash = hash * 31 + rect.name.GetHashCode();

            return hash;
        }
    }
}
