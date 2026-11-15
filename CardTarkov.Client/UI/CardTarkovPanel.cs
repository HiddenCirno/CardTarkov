using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CardTarkov.Client.Cards;
using CardTarkov.Core.Ai;
using CardTarkov.Core.Game;
using EFT.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PokerUtils;

namespace CardTarkov.Client.UI
{
    /// <summary>
    /// 棋牌室面板：设置页 + 斗地主牌桌 + 结算页。
    ///
    /// <para>
    /// v0.1 纯客户端：牌局跑在本地 Core，AI 在 UI 层延迟 2–5 秒后行动。
    /// 切换背包页签或关闭背包会销毁当前牌局并回到设置页。
    /// </para>
    /// </summary>
    internal sealed class CardTarkovPanel : MonoBehaviour
    {
        private InventoryScreen _screen;
        private TMP_Text _template;
        private RectTransform _content;

        private bool _built;
        private bool _panelShown;

        private GameObject _settingsRoot;
        private GameObject _gameRoot;
        private GameObject _resultRoot;

        private DdzGame _game;
        private DdzConfig _lastConfig;
        private bool _noShuffle;
        private bool _seedWasRandom = true;

        private readonly List<CardView> _handViews = new List<CardView>();
        private readonly List<CardView> _centerViews = new List<CardView>();
        private readonly List<CardView> _bottomViews = new List<CardView>();
        private readonly HashSet<PokerCard> _selected = new HashSet<PokerCard>();
        private readonly string[] _lastAction = new string[3];

        private Coroutine _aiRoutine;
        private int _aiEpoch;
        private bool _aiBusy;
        private int _lastDealNumber;

        // 设置页
        private Button _noShuffleButton;
        private TMP_Text _noShuffleLabel;
        private TMP_InputField _seedField;
        private TMP_Text _settingsStatus;

        // 牌桌
        private TMP_Text _metaText;
        private TMP_Text _counterText;
        private TMP_Text _statusText;
        private TMP_Text _humanRole;

        private TMP_Text _leftName;
        private TMP_Text _leftCount;
        private TMP_Text _leftAction;
        private GameObject _leftBadge;

        private TMP_Text _rightName;
        private TMP_Text _rightCount;
        private TMP_Text _rightAction;
        private GameObject _rightBadge;

        private Transform _bottomRoot;
        private Transform _centerRoot;
        private TMP_Text _centerLabel;
        private Transform _handRoot;

        private GameObject _bidGroup;
        private GameObject _playGroup;
        private GameObject _waitingGroup;
        private Button _callButton;
        private Button _noCallButton;
        private Button _playButton;
        private Button _passButton;
        private Button _hintButton;
        private Button _exitButton;

        private TMP_Text _resultTitle;
        private TMP_Text _resultDetail;
        private Button _againButton;
        private Button _backButton;

        // ══════════════════ 构建 ══════════════════

        internal void Build(InventoryScreen screen, TMP_Text template)
        {
            _screen = screen;
            _template = template;

            CreateLabel(
                transform, "Header", Strings.PanelTitle, 24f,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(20f, -52f), new Vector2(-20f, -12f),
                TextAlignmentOptions.MidlineLeft);

            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(transform, false);

            _content = (RectTransform)content.transform;
            _content.anchorMin = Vector2.zero;
            _content.anchorMax = Vector2.one;
            _content.offsetMin = new Vector2(14f, 14f);
            _content.offsetMax = new Vector2(-14f, -60f);

            BuildSettings(_content);
            BuildGame(_content);
            BuildResult(_content);

            _built = true;
            ShowSettings();
        }

        private void BuildSettings(Transform parent)
        {
            _settingsRoot = CreatePanel(
                parent, "Settings",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-340f, -250f), new Vector2(340f, 250f),
                new Color(0.075f, 0.082f, 0.082f, 0.98f));

            CreateLabel(
                _settingsRoot.transform, "Title", Strings.Settings, 24f,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(20f, -56f), new Vector2(-20f, -16f),
                TextAlignmentOptions.Center);

            CreateLabel(
                _settingsRoot.transform, "NoShuffleLabel", Strings.NoShuffle, 18f,
                new Vector2(0f, 1f), new Vector2(0.32f, 1f),
                new Vector2(28f, -112f), new Vector2(0f, -72f),
                TextAlignmentOptions.MidlineLeft);

            _noShuffleButton = CreateButton(
                _settingsRoot.transform, "NoShuffle", "",
                new Vector2(0.32f, 1f), new Vector2(0.96f, 1f),
                new Vector2(0f, -112f), new Vector2(0f, -72f),
                new Color(0.16f, 0.18f, 0.18f, 0.95f));

            _noShuffleLabel = _noShuffleButton.GetComponentInChildren<TMP_Text>();
            _noShuffleButton.onClick.AddListener((UnityEngine.Events.UnityAction)OnToggleNoShuffle);

            CreateLabel(
                _settingsRoot.transform, "AiLabel", Strings.AiStrength, 18f,
                new Vector2(0f, 1f), new Vector2(0.32f, 1f),
                new Vector2(28f, -168f), new Vector2(0f, -128f),
                TextAlignmentOptions.MidlineLeft);

            Button aiButton = CreateButton(
                _settingsRoot.transform, "AiStrength", Strings.AiNormal,
                new Vector2(0.32f, 1f), new Vector2(0.96f, 1f),
                new Vector2(0f, -168f), new Vector2(0f, -128f),
                new Color(0.12f, 0.14f, 0.14f, 0.9f));

            aiButton.interactable = false;

            CreateLabel(
                _settingsRoot.transform, "SeedLabel", Strings.Seed, 18f,
                new Vector2(0f, 1f), new Vector2(0.32f, 1f),
                new Vector2(28f, -224f), new Vector2(0f, -184f),
                TextAlignmentOptions.MidlineLeft);

            _seedField = CreateInput(
                _settingsRoot.transform, "SeedInput",
                new Vector2(0.32f, 1f), new Vector2(0.96f, 1f),
                new Vector2(0f, -224f), new Vector2(0f, -184f),
                Strings.SeedPlaceholder, 11);

            CreateLabel(
                _settingsRoot.transform, "MaxMultiplierLabel", Strings.MaxMultiplier, 18f,
                new Vector2(0f, 1f), new Vector2(0.32f, 1f),
                new Vector2(28f, -280f), new Vector2(0f, -240f),
                TextAlignmentOptions.MidlineLeft);

            CreateLabel(
                _settingsRoot.transform, "MaxMultiplierValue", Strings.MaxMultiplierDisabled, 17f,
                new Vector2(0.32f, 1f), new Vector2(0.96f, 1f),
                new Vector2(0f, -280f), new Vector2(0f, -240f),
                TextAlignmentOptions.MidlineLeft)
                .color = new Color(0.55f, 0.55f, 0.50f, 1f);

            Button startButton = CreateButton(
                _settingsRoot.transform, "Start", Strings.StartGame,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-130f, -360f), new Vector2(130f, -310f),
                new Color(0.20f, 0.36f, 0.24f, 0.98f), 20f);

            startButton.onClick.AddListener((UnityEngine.Events.UnityAction)StartGame);

            _settingsStatus = CreateLabel(
                _settingsRoot.transform, "SettingsStatus", "", 15f,
                new Vector2(0.05f, 1f), new Vector2(0.95f, 1f),
                new Vector2(0f, -400f), new Vector2(0f, -368f),
                TextAlignmentOptions.Center);

            TMP_Text hint = CreateLabel(
                _settingsRoot.transform, "RulesHint", Strings.RulesHint, 14f,
                new Vector2(0.05f, 1f), new Vector2(0.95f, 1f),
                new Vector2(0f, -486f), new Vector2(0f, -408f),
                TextAlignmentOptions.Top);

            hint.color = new Color(0.58f, 0.58f, 0.53f, 1f);
            hint.enableWordWrapping = true;
            hint.overflowMode = TextOverflowModes.Overflow;

            UpdateNoShuffleLabel();
        }

        private void BuildGame(Transform parent)
        {
            _gameRoot = new GameObject("Game", typeof(RectTransform));
            _gameRoot.transform.SetParent(parent, false);

            RectTransform gameRect = (RectTransform)_gameRoot.transform;
            gameRect.anchorMin = Vector2.zero;
            gameRect.anchorMax = Vector2.one;
            gameRect.offsetMin = Vector2.zero;
            gameRect.offsetMax = Vector2.zero;

            _metaText = CreateLabel(
                _gameRoot.transform, "Meta", "", 15f,
                new Vector2(0f, 1f), new Vector2(0.88f, 1f),
                new Vector2(12f, -30f), new Vector2(0f, -4f),
                TextAlignmentOptions.MidlineLeft);

            _exitButton = CreateButton(
                _gameRoot.transform, "ExitGame", Strings.ExitGame,
                new Vector2(0.88f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -52f), new Vector2(-6f, -8f),
                new Color(0.32f, 0.16f, 0.16f, 0.95f), 15f);

            _exitButton.onClick.AddListener((UnityEngine.Events.UnityAction)OnExitToSettings);

            _counterText = CreateLabel(
                _gameRoot.transform, "Counter", "", 15f,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(12f, -62f), new Vector2(-12f, -34f),
                TextAlignmentOptions.MidlineLeft);

            // ── 牌桌 ──
            GameObject table = CreatePanel(
                _gameRoot.transform, "Table",
                new Vector2(0f, 0.37f), new Vector2(1f, 1f),
                new Vector2(10f, 0f), new Vector2(-10f, -72f),
                new Color(0.045f, 0.075f, 0.062f, 0.75f));

            _leftBadge = CreateBadge(
                table.transform, "LeftBadge",
                new Vector2(0.02f, 0.80f), new Vector2(0.10f, 0.94f));

            _rightBadge = CreateBadge(
                table.transform, "RightBadge",
                new Vector2(0.90f, 0.80f), new Vector2(0.98f, 0.94f));

            _leftName = CreateLabel(
                table.transform, "LeftName", "", 18f,
                new Vector2(0.02f, 0.62f), new Vector2(0.22f, 0.80f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft);

            _leftCount = CreateLabel(
                table.transform, "LeftCount", "", 16f,
                new Vector2(0.02f, 0.52f), new Vector2(0.22f, 0.63f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft);

            _leftAction = CreateLabel(
                table.transform, "LeftAction", "", 16f,
                new Vector2(0.02f, 0.40f), new Vector2(0.22f, 0.52f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft);

            _rightName = CreateLabel(
                table.transform, "RightName", "", 18f,
                new Vector2(0.78f, 0.62f), new Vector2(0.98f, 0.80f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineRight);

            _rightCount = CreateLabel(
                table.transform, "RightCount", "", 16f,
                new Vector2(0.78f, 0.52f), new Vector2(0.98f, 0.63f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineRight);

            _rightAction = CreateLabel(
                table.transform, "RightAction", "", 16f,
                new Vector2(0.78f, 0.40f), new Vector2(0.98f, 0.52f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineRight);

            GameObject bottomRootObject = new GameObject("BottomCards", typeof(RectTransform));
            bottomRootObject.transform.SetParent(table.transform, false);

            RectTransform bottomRect = (RectTransform)bottomRootObject.transform;
            bottomRect.anchorMin = new Vector2(0.38f, 0.78f);
            bottomRect.anchorMax = new Vector2(0.62f, 0.99f);
            bottomRect.offsetMin = Vector2.zero;
            bottomRect.offsetMax = Vector2.zero;
            _bottomRoot = bottomRootObject.transform;

            GameObject centerObject = new GameObject("Played", typeof(RectTransform));
            centerObject.transform.SetParent(table.transform, false);

            RectTransform centerRect = (RectTransform)centerObject.transform;
            centerRect.anchorMin = new Vector2(0.20f, 0.04f);
            centerRect.anchorMax = new Vector2(0.80f, 0.66f);
            centerRect.offsetMin = Vector2.zero;
            centerRect.offsetMax = Vector2.zero;
            _centerRoot = centerObject.transform;

            _centerLabel = CreateLabel(
                table.transform, "CenterLabel", "", 16f,
                new Vector2(0.20f, 0.66f), new Vector2(0.80f, 0.75f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center);

            // ── 底部：玩家信息 / 状态 / 按钮 / 手牌 ──
            _humanRole = CreateLabel(
                _gameRoot.transform, "HumanRole", "", 17f,
                new Vector2(0.03f, 0.255f), new Vector2(0.30f, 0.295f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft);

            _statusText = CreateLabel(
                _gameRoot.transform, "Status", "", 16f,
                new Vector2(0.30f, 0.255f), new Vector2(0.97f, 0.295f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft);

            GameObject buttonRoot = new GameObject("Buttons", typeof(RectTransform));
            buttonRoot.transform.SetParent(_gameRoot.transform, false);

            RectTransform buttonRect = (RectTransform)buttonRoot.transform;
            buttonRect.anchorMin = new Vector2(0.25f, 0.30f);
            buttonRect.anchorMax = new Vector2(0.75f, 0.36f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;

            _bidGroup = new GameObject("BidGroup", typeof(RectTransform));
            _bidGroup.transform.SetParent(buttonRoot.transform, false);
            Stretch((RectTransform)_bidGroup.transform);

            _callButton = CreateButton(
                _bidGroup.transform, "Call", Strings.CallLandlord,
                new Vector2(0f, 0f), new Vector2(0.48f, 1f),
                Vector2.zero, Vector2.zero,
                new Color(0.20f, 0.34f, 0.22f, 0.98f), 18f);

            _callButton.onClick.AddListener(
                (UnityEngine.Events.UnityAction)(() => HumanBid(true)));

            _noCallButton = CreateButton(
                _bidGroup.transform, "NoCall", Strings.NoCall,
                new Vector2(0.52f, 0f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero,
                new Color(0.30f, 0.16f, 0.16f, 0.98f), 18f);

            _noCallButton.onClick.AddListener(
                (UnityEngine.Events.UnityAction)(() => HumanBid(false)));

            _playGroup = new GameObject("PlayGroup", typeof(RectTransform));
            _playGroup.transform.SetParent(buttonRoot.transform, false);
            Stretch((RectTransform)_playGroup.transform);

            _playButton = CreateButton(
                _playGroup.transform, "Play", Strings.Play,
                new Vector2(0f, 0f), new Vector2(0.31f, 1f),
                Vector2.zero, Vector2.zero,
                new Color(0.20f, 0.34f, 0.22f, 0.98f), 18f);

            _playButton.onClick.AddListener((UnityEngine.Events.UnityAction)HumanPlay);

            _passButton = CreateButton(
                _playGroup.transform, "Pass", Strings.Pass,
                new Vector2(0.345f, 0f), new Vector2(0.655f, 1f),
                Vector2.zero, Vector2.zero,
                new Color(0.26f, 0.24f, 0.20f, 0.98f), 18f);

            _passButton.onClick.AddListener((UnityEngine.Events.UnityAction)HumanPass);

            _hintButton = CreateButton(
                _playGroup.transform, "Hint", Strings.Hint,
                new Vector2(0.69f, 0f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero,
                new Color(0.18f, 0.24f, 0.34f, 0.98f), 18f);

            _hintButton.onClick.AddListener((UnityEngine.Events.UnityAction)HumanHint);

            _waitingGroup = new GameObject("Waiting", typeof(RectTransform));
            _waitingGroup.transform.SetParent(buttonRoot.transform, false);
            Stretch((RectTransform)_waitingGroup.transform);

            TMP_Text waitingLabel = CreateLabel(
                _waitingGroup.transform, "WaitingLabel", Strings.WaitingAi, 18f,
                Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center);

            waitingLabel.color = new Color(0.70f, 0.68f, 0.58f, 1f);

            GameObject handObject = new GameObject("Hand", typeof(RectTransform));
            handObject.transform.SetParent(_gameRoot.transform, false);

            RectTransform handRect = (RectTransform)handObject.transform;
            handRect.anchorMin = new Vector2(0.03f, 0.02f);
            handRect.anchorMax = new Vector2(0.97f, 0.245f);
            handRect.offsetMin = Vector2.zero;
            handRect.offsetMax = Vector2.zero;
            _handRoot = handObject.transform;

            _gameRoot.SetActive(false);
        }

        private void BuildResult(Transform parent)
        {
            _resultRoot = CreatePanel(
                parent, "Result",
                Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero,
                new Color(0f, 0f, 0f, 0.72f));

            GameObject box = CreatePanel(
                _resultRoot.transform, "ResultBox",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-280f, -170f), new Vector2(280f, 170f),
                new Color(0.075f, 0.082f, 0.082f, 0.99f));

            _resultTitle = CreateLabel(
                box.transform, "ResultTitle", "", 28f,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(20f, -80f), new Vector2(-20f, -20f),
                TextAlignmentOptions.Center);

            _resultDetail = CreateLabel(
                box.transform, "ResultDetail", "", 16f,
                new Vector2(0.05f, 1f), new Vector2(0.95f, 1f),
                new Vector2(0f, -170f), new Vector2(0f, -84f),
                TextAlignmentOptions.Top);

            _resultDetail.enableWordWrapping = true;

            _againButton = CreateButton(
                box.transform, "Again", Strings.Again,
                new Vector2(0.05f, 0f), new Vector2(0.47f, 0f),
                new Vector2(0f, 24f), new Vector2(0f, 68f),
                new Color(0.20f, 0.34f, 0.22f, 0.98f), 18f);

            _againButton.onClick.AddListener((UnityEngine.Events.UnityAction)RestartGame);

            _backButton = CreateButton(
                box.transform, "Back", Strings.BackToSettings,
                new Vector2(0.53f, 0f), new Vector2(0.95f, 0f),
                new Vector2(0f, 24f), new Vector2(0f, 68f),
                new Color(0.26f, 0.24f, 0.20f, 0.98f), 18f);

            _backButton.onClick.AddListener((UnityEngine.Events.UnityAction)BackToSettings);

            _resultRoot.SetActive(false);
        }

        // ══════════════════ 生命周期 ══════════════════

        private void OnEnable()
        {
            _panelShown = true;

            if (!_built)
                return;

            if (_game == null)
                ShowSettings();
            else
                RefreshAll();
        }

        private void OnDisable()
        {
            _panelShown = false;

            if (_built)
                ShutdownGame("panel-hidden");
        }

        private void OnDestroy()
        {
            StopAi();
            _game = null;
        }

        internal void OnPanelShown()
        {
            _panelShown = true;

            if (!_built)
                return;

            if (_game == null)
                ShowSettings();
            else
                RefreshAll();
        }

        // ══════════════════ 设置 / 开始 / 销毁 ══════════════════

        private void OnToggleNoShuffle()
        {
            _noShuffle = !_noShuffle;
            UpdateNoShuffleLabel();
        }

        private void UpdateNoShuffleLabel()
        {
            if (_noShuffleLabel != null)
            {
                _noShuffleLabel.text = _noShuffle
                    ? Strings.NoShuffleOn
                    : Strings.NoShuffleOff;
            }
        }

        private void StartGame()
        {
            int seed;
            string raw = _seedField != null ? _seedField.text.Trim() : "";

            if (string.IsNullOrEmpty(raw))
            {
                seed = UnityEngine.Random.Range(1, int.MaxValue);
                _seedWasRandom = true;

                if (_seedField != null)
                    _seedField.text = seed.ToString(CultureInfo.InvariantCulture);
            }
            else if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture,
                         out seed) || seed <= 0)
            {
                SetSettingsStatus(Strings.ErrorSeed);
                return;
            }
            else
            {
                _seedWasRandom = false;
            }

            _lastConfig = new DdzConfig
            {
                Seed = seed,
                NoShuffle = _noShuffle,
                NoShuffleControlFactor = 50,
                AiDifficulty = AiDifficulty.Normal,
                MaxMultiplier = 0
            };

            BeginGame();
        }

        private void RestartGame()
        {
            if (_lastConfig == null)
            {
                StartGame();
                return;
            }

            if (_seedWasRandom)
            {
                _lastConfig.Seed = UnityEngine.Random.Range(1, int.MaxValue);

                if (_seedField != null)
                    _seedField.text = _lastConfig.Seed.ToString(CultureInfo.InvariantCulture);
            }

            BeginGame();
        }

        private void BeginGame()
        {
            StopAi();

            _game = new DdzGame(_lastConfig, 0);
            _game.Start();

            _lastDealNumber = 0;
            _selected.Clear();
            Array.Clear(_lastAction, 0, _lastAction.Length);
            ClearCardViews(_centerViews);
            ClearCardViews(_bottomViews);
            ClearCardViews(_handViews);

            _resultRoot.SetActive(false);
            _settingsRoot.SetActive(false);
            _gameRoot.SetActive(true);

            Canvas.ForceUpdateCanvases();
            RefreshAll();

            if (_game.Phase == GamePhase.Finished)
                ShowResult();
            else
                ScheduleAi();
        }

        internal void ShutdownGame(string reason)
        {
            StopAi();
            _game = null;
            _selected.Clear();
            Array.Clear(_lastAction, 0, _lastAction.Length);

            ClearCardViews(_centerViews);
            ClearCardViews(_bottomViews);
            ClearCardViews(_handViews);

            if (_built)
                ShowSettings();

            if (!string.IsNullOrEmpty(reason))
                Plugin.Log?.LogDebug($"[CardTarkov] 牌局已销毁：{reason}");
        }

        private void StopAi()
        {
            _aiEpoch++;
            _aiBusy = false;

            if (_aiRoutine != null)
            {
                StopCoroutine(_aiRoutine);
                _aiRoutine = null;
            }
        }

        private void ShowSettings()
        {
            _settingsRoot.SetActive(true);
            _gameRoot.SetActive(false);
            _resultRoot.SetActive(false);

            if (_settingsStatus != null && _game == null)
                _settingsStatus.text = "";
        }

        private void OnExitToSettings()
        {
            ShutdownGame("exit");
            ShowSettings();
        }

        private void BackToSettings()
        {
            ShutdownGame("back");
            ShowSettings();
        }

        // ══════════════════ 玩家操作 ══════════════════

        private void OnCardClicked(CardView view)
        {
            if (_game == null || _game.Phase != GamePhase.Playing ||
                _game.CurrentPlayer != 0 || view == null)
            {
                return;
            }

            if (_selected.Contains(view.Card))
            {
                _selected.Remove(view.Card);
                view.SetSelected(false);
            }
            else
            {
                _selected.Add(view.Card);
                view.SetSelected(true);
            }

            RefreshControlState();
        }

        private void HumanBid(bool call)
        {
            if (_game == null)
                return;

            ActionResult result = _game.Bid(0, call);

            if (!result.Success)
            {
                SetStatus(Strings.Error(result.Error));
                return;
            }

            _lastAction[0] = call ? Strings.CallLandlord : Strings.NoCall;
            AfterHumanAction();
        }

        private void HumanPlay()
        {
            if (_game == null)
                return;

            if (_selected.Count == 0)
            {
                SetStatus(Strings.SelectCards);
                return;
            }

            var cards = _selected.ToList();
            ActionResult result = _game.Play(0, cards);

            if (!result.Success)
            {
                SetStatus(Strings.Error(result.Error));
                return;
            }

            _selected.Clear();
            _lastAction[0] = Strings.Play;
            AfterHumanAction();
        }

        private void HumanPass()
        {
            if (_game == null)
                return;

            ActionResult result = _game.Pass(0);

            if (!result.Success)
            {
                SetStatus(Strings.Error(result.Error));
                return;
            }

            _lastAction[0] = Strings.Pass;
            AfterHumanAction();
        }

        private void HumanHint()
        {
            if (_game == null)
                return;

            HintResult hint = _game.GetHint(0);

            if (hint == null)
            {
                SetStatus(Strings.NoCardsToBeat);
                return;
            }

            _selected.Clear();

            foreach (PokerCard card in hint.Cards)
                _selected.Add(card);

            foreach (CardView view in _handViews)
                view.SetSelected(_selected.Contains(view.Card));

            SetStatus("提示：" + Strings.TypeName(hint.Type));
            RefreshControlState();
        }

        private void AfterHumanAction()
        {
            RefreshAll();

            if (_game.Phase == GamePhase.Finished)
            {
                ShowResult();
                return;
            }

            ScheduleAi();
        }

        // ══════════════════ AI ══════════════════

        private void ScheduleAi()
        {
            if (_game == null || !_panelShown || _aiBusy ||
                _game.Phase == GamePhase.Finished ||
                _game.CurrentPlayer == 0)
            {
                return;
            }

            _aiBusy = true;
            int epoch = ++_aiEpoch;
            _aiRoutine = StartCoroutine(AiTurn(epoch));
        }

        private IEnumerator AiTurn(int epoch)
        {
            float delay = UnityEngine.Random.Range(2f, 5f);
            yield return new WaitForSecondsRealtime(delay);

            if (_game == null || epoch != _aiEpoch || !_panelShown)
            {
                _aiBusy = false;
                _aiRoutine = null;
                yield break;
            }

            int player = _game.CurrentPlayer;

            if (player == 0 || _game.Phase == GamePhase.Finished)
            {
                _aiBusy = false;
                _aiRoutine = null;
                RefreshAll();
                yield break;
            }

            ActionResult result;

            if (_game.Phase == GamePhase.Bidding)
            {
                bool call = BasicAi.ShouldBid(_game.GetHand(player));
                _lastAction[player] = call ? Strings.CallLandlord : Strings.NoCall;
                result = _game.Bid(player, call);
            }
            else
            {
                AiMove move = BasicAi.ChooseMove(_game, player);
                _lastAction[player] = move.IsPass ? Strings.Pass : Strings.Play;
                result = move.IsPass
                    ? _game.Pass(player)
                    : _game.Play(player, move.Cards);
            }

            _aiBusy = false;
            _aiRoutine = null;

            if (!result.Success)
            {
                Plugin.Log?.LogWarning(
                    $"[CardTarkov] AI 动作失败：{result.Error}（玩家 {player}）");
                SetStatus(Strings.Error(result.Error));
                RefreshAll();
                yield break;
            }

            RefreshAll();

            if (_game.Phase == GamePhase.Finished)
                ShowResult();
            else
                ScheduleAi();
        }

        // ══════════════════ 刷新 ══════════════════

        private void RefreshAll()
        {
            if (!_built || _game == null)
                return;

            // 重发/新一局：清空旧的选牌与动作气泡
            if (_game.DealNumber != _lastDealNumber)
            {
                _lastDealNumber = _game.DealNumber;
                _selected.Clear();
                Array.Clear(_lastAction, 0, _lastAction.Length);
            }
            else if (_game.Phase == GamePhase.Playing &&
                     _game.LastType == PokerType.None)
            {
                // 新一轮：上一轮的“不要”气泡不再显示
                Array.Clear(_lastAction, 0, _lastAction.Length);
            }

            RefreshMeta();
            RefreshCounter();
            RefreshOpponents();
            RefreshBottomCards();
            RefreshCenter();
            RebuildHand();
            RefreshControlState();

            if (_game.Phase == GamePhase.Finished)
                ShowResult();
        }

        private void RefreshMeta()
        {
            if (_metaText == null)
                return;

            _metaText.text = string.Format(
                Strings.SeedLine, _game.Config.Seed, _game.DealNumber) +
                "　·　" + string.Format(Strings.MultiplierLine, _game.Multiplier);
        }

        private void RefreshCounter()
        {
            if (_counterText == null)
                return;

            _counterText.text = Strings.Counter(_game.GetRemainingCounts(0));
        }

        private void RefreshOpponents()
        {
            int landlord = _game.Landlord;

            SetOpponent(
                landlord, 1, _rightName, _rightCount, _rightAction, _rightBadge);
            SetOpponent(
                landlord, 2, _leftName, _leftCount, _leftAction, _leftBadge);

            if (_humanRole != null)
            {
                string role = landlord < 0
                    ? "身份未定"
                    : Strings.RoleName(0, landlord);

                _humanRole.text = "你 · " + role;
                _humanRole.color = landlord == 0
                    ? new Color(0.92f, 0.78f, 0.36f, 1f)
                    : new Color(0.82f, 0.82f, 0.76f, 1f);
            }

            RefreshStatus();
        }

        private void SetOpponent(
            int landlord, int player,
            TMP_Text name, TMP_Text count, TMP_Text action, GameObject badge)
        {
            bool isLandlord = landlord == player;

            if (name != null)
            {
                string role = landlord < 0
                    ? "身份未定"
                    : Strings.RoleName(player, landlord);

                name.text = role + " · " + Strings.SeatName(player);
                name.color = isLandlord
                    ? new Color(0.92f, 0.78f, 0.36f, 1f)
                    : new Color(0.82f, 0.82f, 0.76f, 1f);
            }

            if (count != null)
                count.text = "剩 " + _game.GetHandCount(player) + " 张";

            if (action != null)
            {
                action.text = _lastAction[player] ?? "";

                action.color = _lastAction[player] == Strings.Pass
                    ? new Color(0.62f, 0.60f, 0.52f, 1f)
                    : new Color(0.92f, 0.86f, 0.64f, 1f);
            }

            if (badge != null)
                badge.SetActive(isLandlord);
        }

        private void RefreshStatus()
        {
            if (_statusText == null || _game == null)
                return;

            if (_game.Phase == GamePhase.Finished)
            {
                _statusText.text = "";
                return;
            }

            if (_game.CurrentPlayer == 0)
            {
                if (_game.Phase == GamePhase.Bidding)
                {
                    _statusText.text = Strings.YourBid;
                }
                else if (_game.LastType == PokerType.None)
                {
                    _statusText.text = Strings.YourTurn + "　·　你是领出方";
                }
                else
                {
                    _statusText.text = Strings.YourTurn + "　·　上家：" +
                                       Strings.TypeName(_game.LastType);
                }

                _statusText.color = new Color(0.90f, 0.86f, 0.66f, 1f);
            }
            else
            {
                _statusText.text = Strings.SeatName(_game.CurrentPlayer) + " " +
                                   Strings.WaitingAi;
                _statusText.color = new Color(0.68f, 0.68f, 0.62f, 1f);
            }
        }

        private void RefreshBottomCards()
        {
            ClearCardViews(_bottomViews);

            bool faceUp = _game.Landlord >= 0;
            List<PokerCard> cards = _game.BottomCards.ToList();

            if (cards.Count == 0)
                return;

            const float cardWidth = 58f;
            const float cardHeight = 84f;
            float spacing = 64f;
            float total = (cards.Count - 1) * spacing + cardWidth;

            for (int i = 0; i < cards.Count; i++)
            {
                CardView view = CardView.Create(
                    _bottomRoot, _template, cardWidth, cardHeight, 16f, 0f, null);

                view.SetCard(cards[i], faceUp);
                view.SetBasePosition(new Vector2(
                    -total * 0.5f + cardWidth * 0.5f + i * spacing, 0f));
                _bottomViews.Add(view);
            }
        }

        private void RefreshCenter()
        {
            ClearCardViews(_centerViews);

            if (_game.LastType == PokerType.None)
            {
                if (_centerLabel != null)
                {
                    _centerLabel.text = _game.Landlord >= 0 && _game.Phase == GamePhase.Playing
                        ? string.Format(
                            Strings.NewTrick,
                            Strings.SeatName(_game.CurrentPlayer))
                        : "";
                }

                return;
            }

            if (_centerLabel != null)
            {
                _centerLabel.text = string.Format(
                    Strings.LastPlayBy, Strings.SeatName(_game.LastPlayer)) +
                    "　·　" + Strings.TypeName(_game.LastType);
            }

            List<PokerCard> cards = _game.LastCards.ToList();

            if (cards.Count == 0)
                return;

            const float cardWidth = 58f;
            const float cardHeight = 84f;
            const float gap = 4f;
            float available = ((RectTransform)_centerRoot).rect.width;

            if (available < 300f)
                available = 900f;

            float spacing = cards.Count > 1
                ? Mathf.Min(cardWidth + gap, (available - cardWidth) / (cards.Count - 1))
                : 0f;

            float total = (cards.Count - 1) * spacing + cardWidth;

            for (int i = 0; i < cards.Count; i++)
            {
                CardView view = CardView.Create(
                    _centerRoot, _template, cardWidth, cardHeight, 16f, 0f, null);

                view.SetCard(cards[i], true);
                view.SetBasePosition(new Vector2(
                    -total * 0.5f + cardWidth * 0.5f + i * spacing, 0f));
                _centerViews.Add(view);
            }
        }

        private void RebuildHand()
        {
            ClearCardViews(_handViews);

            List<PokerCard> cards = _game.GetHand(0).ToList();
            bool interactive = _game.Phase == GamePhase.Playing &&
                               _game.CurrentPlayer == 0;

            if (cards.Count == 0)
                return;

            float available = ((RectTransform)_handRoot).rect.width;

            if (available < 300f)
            {
                float contentWidth = _content != null ? _content.rect.width : 0f;
                available = contentWidth > 300f ? contentWidth * 0.94f : 1200f;
            }

            float cardWidth = Mathf.Clamp(available / 13.5f, 72f, 96f);
            float cardHeight = cardWidth * 1.42f;
            float rankSize = Mathf.Max(16f, cardWidth * 0.28f);

            float spacing = cards.Count > 1
                ? Mathf.Min(cardWidth + 6f, (available - cardWidth) / (cards.Count - 1))
                : 0f;

            float total = (cards.Count - 1) * spacing + cardWidth;

            for (int i = 0; i < cards.Count; i++)
            {
                PokerCard card = cards[i];

                CardView view = CardView.Create(
                    _handRoot, _template, cardWidth, cardHeight, rankSize, 22f,
                    OnCardClicked);

                view.SetCard(card, true);
                view.SetSelected(_selected.Contains(card));
                view.SetBasePosition(new Vector2(
                    -total * 0.5f + cardWidth * 0.5f + i * spacing, 0f));
                view.SetInteractable(interactive);

                _handViews.Add(view);
            }
        }

        private void RefreshControlState()
        {
            if (_game == null)
            {
                SetGroupActive(_bidGroup, false);
                SetGroupActive(_playGroup, false);
                SetGroupActive(_waitingGroup, false);
                return;
            }

            bool humanTurn = _game.CurrentPlayer == 0 &&
                             _game.Phase != GamePhase.Finished;

            bool bidding = _game.Phase == GamePhase.Bidding && humanTurn;
            bool playing = _game.Phase == GamePhase.Playing && humanTurn;

            SetGroupActive(_bidGroup, bidding);
            SetGroupActive(_playGroup, playing);
            SetGroupActive(
                _waitingGroup,
                _game.Phase != GamePhase.Finished && !humanTurn);

            if (_playButton != null)
                _playButton.interactable = playing && _selected.Count > 0;

            if (_passButton != null)
                _passButton.interactable = playing && _game.LastType != PokerType.None;

            bool interactive = playing;

            foreach (CardView view in _handViews)
                view.SetInteractable(interactive);
        }

        private void ShowResult()
        {
            if (_game == null || _game.Phase != GamePhase.Finished)
                return;

            StopAi();

            GameResult result = _game.BuildResult();

            _resultTitle.text = result.HumanWon ? Strings.Win : Strings.Lose;
            _resultTitle.color = result.HumanWon
                ? new Color(0.94f, 0.80f, 0.36f, 1f)
                : new Color(0.80f, 0.38f, 0.34f, 1f);

            _resultDetail.text =
                string.Format(
                    Strings.RoleLine,
                    result.HumanIsLandlord ? Strings.Landlord : Strings.Farmer,
                    result.Winner == WinnerTeam.Landlord ? "地主获胜" : "农民获胜") +
                "\n" +
                string.Format(Strings.SeedLine, result.Seed, result.DealNumber) +
                "\n" +
                string.Format(Strings.MultiplierLine, result.Multiplier);

            _resultRoot.SetActive(true);
            SetGroupActive(_bidGroup, false);
            SetGroupActive(_playGroup, false);
            SetGroupActive(_waitingGroup, false);
        }

        // ══════════════════ UI 工具 ══════════════════

        private TMP_Text CreateLabel(
            Transform parent, string name, string text, float fontSize,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax,
            TextAlignmentOptions alignment)
        {
            var labelObject = new GameObject(
                name, typeof(RectTransform), typeof(TextMeshProUGUI));

            labelObject.transform.SetParent(parent, false);

            var label = labelObject.GetComponent<TextMeshProUGUI>();

            if (_template != null)
            {
                label.font = _template.font;
                label.fontSharedMaterial = _template.fontSharedMaterial;
                label.fontStyle = _template.fontStyle;
            }

            label.text = text;
            label.fontSize = fontSize;
            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Max(8f, fontSize * 0.6f);
            label.fontSizeMax = fontSize;
            label.enableWordWrapping = false;
            label.alignment = alignment;
            label.color = new Color(0.82f, 0.82f, 0.76f, 1f);
            label.raycastTarget = false;

            RectTransform rect = label.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            return label;
        }

        private Button CreateButton(
            Transform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax,
            Color color, float fontSize = 18f)
        {
            var buttonObject = new GameObject(
                name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Button));

            buttonObject.transform.SetParent(parent, false);

            var background = buttonObject.GetComponent<Image>();
            background.color = color;
            background.raycastTarget = true;

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = background;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.55f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            RectTransform rect = (RectTransform)buttonObject.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            TMP_Text text = CreateLabel(
                buttonObject.transform, "Label", label, fontSize,
                Vector2.zero, Vector2.one,
                new Vector2(4f, 2f), new Vector2(-4f, -2f),
                TextAlignmentOptions.Center);

            text.color = new Color(0.90f, 0.90f, 0.84f, 1f);

            return button;
        }

        private static GameObject CreatePanel(
            Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax,
            Color color)
        {
            var panel = new GameObject(
                name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

            panel.transform.SetParent(parent, false);

            var image = panel.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;

            RectTransform rect = (RectTransform)panel.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            return panel;
        }

        private GameObject CreateBadge(
            Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject badge = CreatePanel(
                parent, name, anchorMin, anchorMax,
                Vector2.zero, Vector2.zero,
                new Color(0.62f, 0.48f, 0.16f, 0.95f));

            CreateLabel(
                badge.transform, "Label", Strings.Landlord, 15f,
                Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center);

            badge.SetActive(false);
            return badge;
        }

        private TMP_InputField CreateInput(
            Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax,
            string placeholderText, int characterLimit)
        {
            var inputObject = new GameObject(
                name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(TMP_InputField));

            inputObject.transform.SetParent(parent, false);

            var background = inputObject.GetComponent<Image>();
            background.color = new Color(0.07f, 0.08f, 0.08f, 0.95f);

            RectTransform rect = (RectTransform)inputObject.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            TMP_Text text = CreateInputText(
                inputObject.transform, "Text", "",
                new Color(0.90f, 0.90f, 0.84f, 1f));

            TMP_Text placeholder = CreateInputText(
                inputObject.transform, "Placeholder", placeholderText,
                new Color(0.58f, 0.58f, 0.54f, 0.7f));

            var input = inputObject.GetComponent<TMP_InputField>();
            input.targetGraphic = background;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.contentType = TMP_InputField.ContentType.Standard;
            input.characterLimit = characterLimit;

            return input;
        }

        private TMP_Text CreateInputText(
            Transform parent, string name, string text, Color color)
        {
            var labelObject = new GameObject(
                name, typeof(RectTransform), typeof(TextMeshProUGUI));

            labelObject.transform.SetParent(parent, false);

            var label = labelObject.GetComponent<TextMeshProUGUI>();

            if (_template != null)
            {
                label.font = _template.font;
                label.fontSharedMaterial = _template.fontSharedMaterial;
                label.fontStyle = _template.fontStyle;
            }

            label.text = text;
            label.fontSize = 18f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 10f;
            label.fontSizeMax = 18f;
            label.enableWordWrapping = false;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = color;
            label.raycastTarget = false;

            RectTransform rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(12f, 2f);
            rect.offsetMax = new Vector2(-8f, -2f);

            return label;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetGroupActive(GameObject group, bool active)
        {
            if (group != null)
                group.SetActive(active);
        }

        private void SetSettingsStatus(string text)
        {
            if (_settingsStatus != null)
                _settingsStatus.text = text ?? "";
        }

        private void SetStatus(string text)
        {
            if (_statusText == null)
                return;

            _statusText.text = text ?? "";
            _statusText.color = new Color(0.88f, 0.74f, 0.52f, 1f);
        }

        private static void ClearCardViews(List<CardView> views)
        {
            foreach (CardView view in views)
            {
                if (view != null)
                    view.DisposeView();
            }

            views.Clear();
        }
    }
}
