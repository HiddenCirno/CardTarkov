using System.Collections.Generic;
using EFT.UI;
using UnityEngine;

namespace CardTarkov.Client.UI
{
    /// <summary>
    /// 只负责把「棋牌室」Tab 放到现有页签行末尾，不移动其它 Mod 的页签。
    ///
    /// <para>
    /// Operator Traits / TarkFX 各自有一套“把自己插到原生页签中间”的布局，
    /// 再叠加一套同样的布局会互相打架。这里改成只定位自己的 Tab：
    /// 每次 LateUpdate 找到最后一个可见页签，放在它右边，
    /// 必要时把页签行加宽，保证本 Tab 不被挤出背景。
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(2000)]
    internal sealed class CardTabPositioner : MonoBehaviour
    {
        private RectTransform _row;
        private RectTransform _self;
        private float _selfWidth;
        private bool _initialized;
        private bool _logged;

        internal void Initialize(Transform row, Transform self)
        {
            _row = row as RectTransform;
            _self = self as RectTransform;
            _selfWidth = _self != null ? _self.rect.width : 0f;
            _initialized = true;
            Apply();

            Plugin.Log?.LogInfo(
                $"[CardTarkov] Tab 定位初始化：row={(_row != null ? _row.name : "null")}，" +
                $"rowWidth={(_row != null ? _row.rect.width : 0f):F0}，" +
                $"selfPos={(_self != null ? _self.localPosition.x : 0f):F0}");
        }

        private void LateUpdate()
        {
            Apply();
        }

        private void Apply()
        {
            if (!_initialized || _row == null || _self == null)
                return;

            if (!_self.gameObject.activeSelf)
                return;

            var others = new List<RectTransform>();

            foreach (Tab tab in _row.GetComponentsInChildren<Tab>(true))
            {
                if (tab == null || tab.transform == _self)
                    continue;

                if (tab.transform.parent != _row)
                    continue;

                if (!tab.gameObject.activeSelf)
                    continue;

                others.Add((RectTransform)tab.transform);
            }

            if (others.Count == 0)
                return;

            others.Sort((a, b) => a.localPosition.x.CompareTo(b.localPosition.x));

            RectTransform last = others[others.Count - 1];
            float spacing;

            if (others.Count >= 2)
            {
                RectTransform previous = others[others.Count - 2];
                spacing = last.localPosition.x - previous.localPosition.x;
            }
            else
            {
                spacing = 0f;
            }

            if (spacing <= 1f)
                spacing = Mathf.Max(last.rect.width + 6f, 80f);

            Vector3 position = last.localPosition;
            position.x = last.localPosition.x + spacing;
            _self.localPosition = position;

            if (_selfWidth > 0f)
                _self.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _selfWidth);

            EnsureRowWideEnough(position.x);

            if (!_logged && others.Count >= 2)
            {
                _logged = true;
                Plugin.Log?.LogInfo(
                    $"[CardTarkov] Tab 已定位：last={last.name}@{last.localPosition.x:F0}，" +
                    $"spacing={spacing:F0}，self={position.x:F0}，" +
                    $"rowWidth={_row.rect.width:F0}");
            }
        }

        private void EnsureRowWideEnough(float tabCenterX)
        {
            if (_row == null)
                return;

            float pivotX = _row.pivot.x;
            float rightEdge = _row.rect.width * (1f - pivotX);
            float needed = tabCenterX + _selfWidth * 0.5f + 10f;

            if (needed <= rightEdge)
                return;

            float delta = needed - rightEdge;
            float multiplier = pivotX > 0.01f && pivotX < 0.99f ? 2f : 1f;
            _row.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                _row.rect.width + delta * multiplier);
        }
    }
}
