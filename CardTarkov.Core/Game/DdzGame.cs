using System;
using System.Collections.Generic;
using PokerUtils;

namespace CardTarkov.Core.Game
{
    /// <summary>
    /// 一局斗地主（v0.1：叫地主，无抢地主/加倍）。
    ///
    /// <para>
    /// 纯状态机，不依赖 Unity / 服务端。UI 通过公开状态渲染，
    /// AI 通过 <c>GetHandCopy</c> / <c>LastCards</c> 读取决策所需信息。
    /// </para>
    /// </summary>
    public sealed class DdzGame
    {
        private readonly DdzConfig _config;
        private readonly List<PokerCard>[] _hands =
            { new List<PokerCard>(20), new List<PokerCard>(20), new List<PokerCard>(20) };
        private readonly List<PokerCard> _bottom = new List<PokerCard>(3);
        private readonly List<PokerCard> _played = new List<PokerCard>(54);
        private readonly List<GameEvent> _events = new List<GameEvent>();

        private int _dealNumber;
        private int _bidsMade;
        private MoveInfo _lastInfo;

        public DdzGame(DdzConfig config, int humanPlayer = 0)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            if (humanPlayer < 0 || humanPlayer > 2)
                throw new ArgumentOutOfRangeException(nameof(humanPlayer));

            HumanPlayer = humanPlayer;
        }

        public DdzConfig Config => _config;
        public int HumanPlayer { get; }

        public GamePhase Phase { get; private set; }
        public int CurrentPlayer { get; private set; }
        public int FirstBidder { get; private set; }
        public int Landlord { get; private set; } = -1;
        public WinnerTeam Winner { get; private set; }
        public int Multiplier { get; private set; } = 1;
        public int DealNumber => _dealNumber;

        public IReadOnlyList<PokerCard> BottomCards => _bottom;
        public IReadOnlyList<PokerCard> LastCards { get; private set; } = Array.Empty<PokerCard>();
        public PokerType LastType { get; private set; } = PokerType.None;
        public MoveInfo LastMoveInfo => _lastInfo;
        public int LastPlayer { get; private set; } = -1;
        public int PassCount { get; private set; }
        public IReadOnlyList<PokerCard> PlayedCards => _played;
        public IReadOnlyList<GameEvent> Events => _events;

        public bool IsHuman(int player) => player == HumanPlayer;
        public int GetHandCount(int player) => _hands[player].Count;

        public IReadOnlyList<PokerCard> GetHand(int player) => _hands[player];

        public List<PokerCard> GetHandCopy(int player) => new List<PokerCard>(_hands[player]);

        public List<PokerCard> GetLastCardsCopy() => new List<PokerCard>(LastCards);

        public void Start()
        {
            BeginDeal(false);
        }

        private void BeginDeal(bool redeal)
        {
            _dealNumber++;

            DealResult deal = DdzDeck.Deal(_config, _dealNumber);

            for (int i = 0; i < 3; i++)
            {
                _hands[i].Clear();
                _hands[i].AddRange(deal.Hands[i]);
                Sort(_hands[i]);
            }

            _bottom.Clear();
            _bottom.AddRange(deal.Bottom);
            _played.Clear();
            _events.Clear();

            Phase = GamePhase.Bidding;
            CurrentPlayer = deal.FirstBidder;
            FirstBidder = deal.FirstBidder;
            Landlord = -1;
            Winner = WinnerTeam.None;
            Multiplier = 1;
            LastCards = Array.Empty<PokerCard>();
            LastType = PokerType.None;
            _lastInfo = null;
            LastPlayer = -1;
            PassCount = 0;
            _bidsMade = 0;

            _events.Add(new GameEvent(redeal ? GameEventKind.Redeal : GameEventKind.Deal));
        }

        /// <summary>叫地主 / 不叫。第一个叫的人直接成为地主（v0.1 不做抢地主）。</summary>
        public ActionResult Bid(int player, bool call)
        {
            if (Phase != GamePhase.Bidding)
                return ActionResult.Fail(GameErrors.NotBidding);

            if (player != CurrentPlayer)
                return ActionResult.Fail(GameErrors.NotYourTurn);

            if (call)
            {
                Landlord = player;
                _hands[player].AddRange(_bottom);
                Sort(_hands[player]);

                Phase = GamePhase.Playing;
                CurrentPlayer = player;
                LastCards = Array.Empty<PokerCard>();
                LastType = PokerType.None;
                _lastInfo = null;
                LastPlayer = -1;
                PassCount = 0;

                _events.Add(new GameEvent(
                    GameEventKind.BidCall, player));
                _events.Add(new GameEvent(
                    GameEventKind.LandlordSet, player, new List<PokerCard>(_bottom)));
                return ActionResult.Ok();
            }

            _bidsMade++;
            _events.Add(new GameEvent(GameEventKind.BidPass, player));

            if (_bidsMade >= 3)
            {
                BeginDeal(true);
                return ActionResult.Ok();
            }

            CurrentPlayer = NextPlayer(player);
            return ActionResult.Ok();
        }

        /// <summary>出牌。调用方负责把选中的牌传进来（不要求顺序）。</summary>
        public ActionResult Play(int player, IList<PokerCard> selection)
        {
            if (Phase != GamePhase.Playing)
                return ActionResult.Fail(GameErrors.GameOver);

            if (player != CurrentPlayer)
                return ActionResult.Fail(GameErrors.NotYourTurn);

            if (selection == null || selection.Count == 0)
                return ActionResult.Fail(GameErrors.NoCards);

            var cards = new List<PokerCard>(selection);
            cards.Sort(CompareCard);

            if (!ContainsAll(_hands[player], cards))
                return ActionResult.Fail(GameErrors.NotInHand);

            MoveInfo info = DdzRules.Analyze(cards);
            if (!info.IsValid)
                return ActionResult.Fail(GameErrors.InvalidType);

            if (_lastInfo != null && !DdzRules.CanBeat(info, _lastInfo))
                return ActionResult.Fail(GameErrors.MustBeat);

            RemoveAll(_hands[player], cards);
            _played.AddRange(cards);

            LastCards = cards;
            LastType = info.Type;
            _lastInfo = info;
            LastPlayer = player;
            PassCount = 0;

            _events.Add(new GameEvent(GameEventKind.Play, player, cards, info.Type));

            if (_hands[player].Count == 0)
            {
                Phase = GamePhase.Finished;
                Winner = player == Landlord ? WinnerTeam.Landlord : WinnerTeam.Farmers;
                _events.Add(new GameEvent(GameEventKind.Finished, player));
                return ActionResult.Ok();
            }

            CurrentPlayer = NextPlayer(player);
            return ActionResult.Ok();
        }

        /// <summary>不要。领出时不能不要。</summary>
        public ActionResult Pass(int player)
        {
            if (Phase != GamePhase.Playing)
                return ActionResult.Fail(GameErrors.GameOver);

            if (player != CurrentPlayer)
                return ActionResult.Fail(GameErrors.NotYourTurn);

            if (LastType == PokerType.None)
                return ActionResult.Fail(GameErrors.CannotPass);

            PassCount++;
            _events.Add(new GameEvent(GameEventKind.Pass, player));

            if (PassCount >= 2)
            {
                // 两家都不要：由最后出牌的人重新领出
                if (LastPlayer >= 0)
                    CurrentPlayer = LastPlayer;

                LastCards = Array.Empty<PokerCard>();
                LastType = PokerType.None;
                _lastInfo = null;
                LastPlayer = -1;
                PassCount = 0;
                _events.Add(new GameEvent(GameEventKind.TrickReset, CurrentPlayer));
            }
            else
            {
                CurrentPlayer = NextPlayer(player);
            }

            return ActionResult.Ok();
        }

        /// <summary>给玩家/AI 的一手提示；没有可出牌时返回 null。</summary>
        public HintResult GetHint(int player)
        {
            if (Phase != GamePhase.Playing)
                return null;

            var hand = new List<PokerCard>(_hands[player]);
            var tip = new List<PokerCard>();

            PokerType type;

            if (LastType == PokerType.None)
            {
                type = PokerHelper.GetTipCards(hand, tip);
            }
            else
            {
                PokerHelper.GetTipCards(
                    hand, new List<PokerCard>(LastCards), LastType, tip, out type);
            }

            if (type == PokerType.None || tip.Count == 0)
                return null;

            MoveInfo info = DdzRules.Analyze(tip);
            if (!info.IsValid)
                return null;

            if (_lastInfo != null && !DdzRules.CanBeat(info, _lastInfo))
                return null;

            return new HintResult(tip, info.Type);
        }

        /// <summary>
        /// 记牌器：按牌码 3..17 统计「未出现在自己手里、也未被任何人打出」的张数。
        /// 返回长度 15 的数组（索引 0 = 3，12 = 2，13 = 小王，14 = 大王）。
        /// </summary>
        public int[] GetRemainingCounts(int viewer = 0)
        {
            var remaining = new int[15];
            for (int i = 0; i < 13; i++)
                remaining[i] = 4;

            remaining[13] = 1;
            remaining[14] = 1;

            foreach (PokerCard card in _hands[viewer])
                remaining[RankCode(card) - 3]--;

            foreach (PokerCard card in _played)
                remaining[RankCode(card) - 3]--;

            return remaining;
        }

        public GameResult BuildResult()
        {
            bool humanLandlord = HumanPlayer == Landlord;

            bool humanWon =
                Winner == WinnerTeam.Landlord ? humanLandlord :
                Winner == WinnerTeam.Farmers ? !humanLandlord :
                false;

            return new GameResult
            {
                Seed = _config.Seed,
                DealNumber = _dealNumber,
                HumanPlayer = HumanPlayer,
                Landlord = Landlord,
                HumanWon = humanWon,
                Winner = Winner,
                Multiplier = Multiplier,
                MaxMultiplier = _config.MaxMultiplier
            };
        }

        public static int RankCode(PokerCard card) => (int)card / 4 + 3;

        public static int NextPlayer(int player) => (player + 1) % 3;

        private static int CompareCard(PokerCard a, PokerCard b) =>
            ((int)a).CompareTo((int)b);

        private static void Sort(List<PokerCard> cards) => cards.Sort(CompareCard);

        private static bool ContainsAll(List<PokerCard> hand, List<PokerCard> selection)
        {
            var counts = new Dictionary<PokerCard, int>();

            foreach (PokerCard card in hand)
            {
                counts.TryGetValue(card, out int count);
                counts[card] = count + 1;
            }

            foreach (PokerCard card in selection)
            {
                if (!counts.TryGetValue(card, out int count) || count <= 0)
                    return false;

                counts[card] = count - 1;
            }

            return true;
        }

        private static void RemoveAll(List<PokerCard> hand, List<PokerCard> cards)
        {
            foreach (PokerCard card in cards)
                hand.Remove(card);
        }
    }
}
