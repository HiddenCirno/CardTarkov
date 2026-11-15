using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PokerUtils;

namespace CardTarkov.Client.Cards
{
    /// <summary>
    /// 一张牌的 UI。手牌可点击选中（向上抬起），出牌区/底牌为只读。
    /// </summary>
    internal sealed class CardView : MonoBehaviour
    {
        internal PokerCard Card { get; private set; }
        internal bool FaceUp { get; private set; }
        internal bool Selected { get; private set; }

        private Image _background;
        private TMP_Text _rank;
        private Image _suit;
        private Button _button;
        private RectTransform _rect;
        private Action<CardView> _onClick;
        private Vector2 _basePosition;
        private float _raise;

        internal static CardView Create(
            Transform parent, TMP_Text template,
            float width, float height, float rankSize, float raise,
            Action<CardView> onClick)
        {
            var go = new GameObject(
                "Card", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Button));

            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(width, height);

            var background = go.GetComponent<Image>();
            background.sprite = CardSprites.Background;
            background.type = Image.Type.Sliced;
            background.color = Color.white;
            background.raycastTarget = true;

            var button = go.GetComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.ColorTint;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.10f, 1.10f, 1.10f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.55f);
            colors.fadeDuration = 0.05f;
            button.colors = colors;

            var rankObject = new GameObject(
                "Rank", typeof(RectTransform), typeof(TextMeshProUGUI));
            rankObject.transform.SetParent(go.transform, false);

            var rank = rankObject.GetComponent<TextMeshProUGUI>();
            if (template != null)
            {
                rank.font = template.font;
                rank.fontSharedMaterial = template.fontSharedMaterial;
                rank.fontStyle = template.fontStyle;
            }

            rank.fontSize = rankSize;
            rank.enableAutoSizing = true;
            rank.fontSizeMin = Mathf.Max(8f, rankSize * 0.55f);
            rank.fontSizeMax = rankSize;
            rank.enableWordWrapping = false;
            rank.alignment = TextAlignmentOptions.TopLeft;
            rank.raycastTarget = false;

            RectTransform rankRect = rank.rectTransform;
            rankRect.anchorMin = new Vector2(0f, 1f);
            rankRect.anchorMax = new Vector2(0f, 1f);
            rankRect.pivot = new Vector2(0f, 1f);
            rankRect.anchoredPosition = new Vector2(7f, -4f);
            rankRect.sizeDelta = new Vector2(width - 14f, rankSize * 1.5f);

            var suitObject = new GameObject(
                "Suit", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            suitObject.transform.SetParent(go.transform, false);

            var suit = suitObject.GetComponent<Image>();
            suit.raycastTarget = false;
            suit.preserveAspect = true;

            RectTransform suitRect = suit.rectTransform;
            suitRect.anchorMin = new Vector2(0.5f, 0.16f);
            suitRect.anchorMax = new Vector2(0.5f, 0.16f);
            suitRect.pivot = new Vector2(0.5f, 0.5f);
            suitRect.sizeDelta = new Vector2(width * 0.42f, width * 0.42f);
            suitRect.anchoredPosition = Vector2.zero;

            var view = go.AddComponent<CardView>();
            view._rect = rect;
            view._background = background;
            view._rank = rank;
            view._suit = suit;
            view._button = button;
            view._onClick = onClick;
            view._raise = raise;

            if (onClick != null)
            {
                button.onClick.AddListener((UnityEngine.Events.UnityAction)(() =>
                {
                    view._onClick?.Invoke(view);
                }));
            }
            else
            {
                button.interactable = false;
            }

            return view;
        }

        internal void SetCard(PokerCard card, bool faceUp)
        {
            Card = card;
            FaceUp = faceUp;

            if (!faceUp)
            {
                _background.sprite = CardSprites.Back;
                _background.type = Image.Type.Sliced;
                _background.color = Color.white;
                _rank.text = "";
                _suit.enabled = false;
                return;
            }

            _background.sprite = CardSprites.Background;
            _background.type = Image.Type.Sliced;
            _background.color = Color.white;

            _rank.text = Strings.RankName(card);
            _rank.color = Strings.IsRed(card)
                ? new Color(0.78f, 0.13f, 0.14f, 1f)
                : new Color(0.10f, 0.10f, 0.10f, 1f);

            if (Strings.IsJoker(card))
            {
                _suit.enabled = false;
                return;
            }

            _suit.enabled = true;
            _suit.sprite = CardSprites.Suit(Strings.Suit(card));
            _suit.color = Strings.IsRed(card)
                ? new Color(0.78f, 0.13f, 0.14f, 1f)
                : new Color(0.10f, 0.10f, 0.10f, 1f);
        }

        internal void SetBasePosition(Vector2 position)
        {
            _basePosition = position;
            ApplyPosition();
        }

        internal void SetSelected(bool selected)
        {
            Selected = selected;
            ApplyPosition();
        }

        internal void SetInteractable(bool interactable)
        {
            if (_button != null)
                _button.interactable = interactable;
        }

        internal void DisposeView()
        {
            if (this != null)
                Destroy(gameObject);
        }

        private void ApplyPosition()
        {
            if (_rect == null)
                return;

            _rect.anchoredPosition = _basePosition +
                                     new Vector2(0f, Selected ? _raise : 0f);
        }
    }
}
