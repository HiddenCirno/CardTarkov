using System;
using System.Collections.Generic;
using CardTarkov.Core.Ai;
using CardTarkov.Core.Game;
using PokerUtils;

namespace CardTarkov.Tests
{
    internal static class Program
    {
        private static int _failures;
        private static int _checks;

        private static int Main()
        {
            Run("牌型识别", TestCardTypes);
            Run("牌型比较", TestComparison);
            Run("种子发牌", TestSeededDeal);
            Run("叫地主", TestBidding);
            Run("出牌/过牌/新一轮", TestPlayFlow);
            Run("记牌器", TestCounter);
            Run("两家不要后领出", TestLeadAfterTwoPasses);
            Run("自对弈", TestSelfPlay);

            Console.WriteLine();
            Console.WriteLine(_failures == 0
                ? $"通过：{_checks} 项检查"
                : $"失败：{_failures} / {_checks} 项检查");

            return _failures == 0 ? 0 : 1;
        }

        private static void Run(string name, Action test)
        {
            Console.WriteLine($"== {name} ==");
            try
            {
                test();
            }
            catch (Exception ex)
            {
                Fail($"{name} 异常：{ex}");
            }
        }

        private static void Check(bool condition, string message)
        {
            _checks++;

            if (!condition)
            {
                _failures++;
                Console.WriteLine("  [FAIL] " + message);
            }
        }

        private static void Fail(string message)
        {
            _failures++;
            Console.WriteLine("  [FAIL] " + message);
        }

        // ══════════════════ 测试工具 ══════════════════

        private static PokerCard Card(int rank, int suit = 0) =>
            (PokerCard)((rank - 3) * 4 + suit);

        private static List<PokerCard> Cards(params PokerCard[] cards) =>
            new List<PokerCard>(cards);

        private static void CheckType(
            PokerType expected, string name, params PokerCard[] cards)
        {
            PokerType actual = DdzRules.GetPokerType(Cards(cards));
            Check(actual == expected, $"{name}: 期望 {expected}，实际 {actual}");
        }

        private static void CheckBeats(
            bool expected, string name,
            List<PokerCard> current, PokerType currentType,
            List<PokerCard> last, PokerType lastType)
        {
            MoveInfo c = DdzRules.Analyze(current);
            MoveInfo l = DdzRules.Analyze(last);

            Check(c.IsValid, $"{name}: 当前牌型无效");
            Check(l.IsValid, $"{name}: 上家牌型无效");
            Check(c.Type == currentType, $"{name}: 当前类型期望 {currentType}，实际 {c.Type}");
            Check(l.Type == lastType, $"{name}: 上家类型期望 {lastType}，实际 {l.Type}");

            bool actual = DdzRules.CanBeat(c, l);
            Check(actual == expected, $"{name}: 期望 {expected}，实际 {actual}");
        }

        // ══════════════════ 牌型 ══════════════════

        private static void TestCardTypes()
        {
            CheckType(PokerType.Single, "单张", Card(3));
            CheckType(PokerType.Double, "对子", Card(3, 0), Card(3, 1));
            CheckType(PokerType.ThreeWithNone, "三张", Card(3, 0), Card(3, 1), Card(3, 2));

            CheckType(PokerType.ThreeWithOne, "三带一",
                Card(4, 0), Card(4, 1), Card(4, 2), Card(5));

            CheckType(PokerType.ThreeWihtDouble, "三带二",
                Card(4, 0), Card(4, 1), Card(4, 2), Card(5, 0), Card(5, 1));

            CheckType(PokerType.ShunZi, "顺子",
                Card(3), Card(4), Card(5), Card(6), Card(7));

            CheckType(PokerType.LianDui, "连对",
                Card(3, 0), Card(3, 1), Card(4, 0), Card(4, 1),
                Card(5, 0), Card(5, 1));

            CheckType(PokerType.AeroplaneWithNone, "飞机",
                Card(3, 0), Card(3, 1), Card(3, 2),
                Card(4, 0), Card(4, 1), Card(4, 2));

            CheckType(PokerType.AeroplaneWithTwo, "飞机带两张",
                Card(3, 0), Card(3, 1), Card(3, 2),
                Card(4, 0), Card(4, 1), Card(4, 2),
                Card(5), Card(6));

            CheckType(PokerType.AeroplaneWithTwoDouble, "飞机带两对",
                Card(3, 0), Card(3, 1), Card(3, 2),
                Card(4, 0), Card(4, 1), Card(4, 2),
                Card(5, 0), Card(5, 1), Card(6, 0), Card(6, 1));

            CheckType(PokerType.Bomb, "炸弹",
                Card(3, 0), Card(3, 1), Card(3, 2), Card(3, 3));

            CheckType(PokerType.KingBomb, "王炸",
                PokerCard.Black_Joker, PokerCard.Red_Joker);

            CheckType(PokerType.FourWithTwo, "四带二",
                Card(3, 0), Card(3, 1), Card(3, 2), Card(3, 3),
                Card(4), Card(5));

            CheckType(PokerType.FourWithTwoDouble, "四带两对",
                Card(3, 0), Card(3, 1), Card(3, 2), Card(3, 3),
                Card(4, 0), Card(4, 1), Card(5, 0), Card(5, 1));

            CheckType(PokerType.AeroplaneWithNone, "三连飞机",
                Card(3, 0), Card(3, 1), Card(3, 2),
                Card(4, 0), Card(4, 1), Card(4, 2),
                Card(5, 0), Card(5, 1), Card(5, 2));

            CheckType(PokerType.AeroplaneWithThree, "飞机带三张",
                Card(3, 0), Card(3, 1), Card(3, 2),
                Card(4, 0), Card(4, 1), Card(4, 2),
                Card(5, 0), Card(5, 1), Card(5, 2),
                Card(8), Card(9), Card(10));

            CheckType(PokerType.AeroplaneWithThreeDouble, "飞机带三对",
                Card(3, 0), Card(3, 1), Card(3, 2),
                Card(4, 0), Card(4, 1), Card(4, 2),
                Card(5, 0), Card(5, 1), Card(5, 2),
                Card(8, 0), Card(8, 1), Card(9, 0),
                Card(9, 1), Card(10, 0), Card(10, 1));

            CheckType(PokerType.FourWithTwo, "四带一对也按四带二",
                Card(3, 0), Card(3, 1), Card(3, 2), Card(3, 3),
                Card(4, 0), Card(4, 1));

            CheckType(PokerType.None, "两张不同点数",
                Card(3), Card(4));

            CheckType(PokerType.None, "顺子不能带 2",
                Card(11), Card(12), Card(13), Card(14), Card(15));

            CheckType(PokerType.None, "四张加一张不是合法牌型",
                Card(3, 0), Card(3, 1), Card(3, 2), Card(3, 3), Card(4));
        }

        // ══════════════════ 比较 ══════════════════

        private static void TestComparison()
        {
            var pair3 = Cards(Card(3, 0), Card(3, 1));
            var pair4 = Cards(Card(4, 0), Card(4, 1));

            CheckBeats(true, "对 4 压对 3",
                pair4, PokerType.Double, pair3, PokerType.Double);

            CheckBeats(false, "对 3 压不了对 4",
                pair3, PokerType.Double, pair4, PokerType.Double);

            var t31 = Cards(Card(4, 0), Card(4, 1), Card(4, 2), Card(9));
            var t32 = Cards(Card(3, 0), Card(3, 1), Card(3, 2), Card(15));

            CheckBeats(true, "三带一比三张部分，不看带牌",
                t31, PokerType.ThreeWithOne, t32, PokerType.ThreeWithOne);

            var straight37 = Cards(Card(3), Card(4), Card(5), Card(6), Card(7));
            var straight48 = Cards(Card(4), Card(5), Card(6), Card(7), Card(8));
            var straight49 = Cards(Card(4), Card(5), Card(6), Card(7), Card(8), Card(9));

            CheckBeats(true, "顺子 4-8 压 3-7",
                straight48, PokerType.ShunZi, straight37, PokerType.ShunZi);

            CheckBeats(false, "长度不同的顺子不能压",
                straight49, PokerType.ShunZi, straight37, PokerType.ShunZi);

            var bomb3 = Cards(Card(3, 0), Card(3, 1), Card(3, 2), Card(3, 3));
            var bomb4 = Cards(Card(4, 0), Card(4, 1), Card(4, 2), Card(4, 3));
            var rocket = Cards(PokerCard.Black_Joker, PokerCard.Red_Joker);

            CheckBeats(true, "炸弹压顺子",
                bomb3, PokerType.Bomb, straight37, PokerType.ShunZi);

            CheckBeats(true, "大炸弹压小炸弹",
                bomb4, PokerType.Bomb, bomb3, PokerType.Bomb);

            CheckBeats(true, "王炸压炸弹",
                rocket, PokerType.KingBomb, bomb4, PokerType.Bomb);

            CheckBeats(false, "炸弹压不了王炸",
                bomb4, PokerType.Bomb, rocket, PokerType.KingBomb);

            var plane3344 = Cards(
                Card(3, 0), Card(3, 1), Card(3, 2),
                Card(4, 0), Card(4, 1), Card(4, 2));
            var plane4455 = Cards(
                Card(4, 0), Card(4, 1), Card(4, 2),
                Card(5, 0), Card(5, 1), Card(5, 2));

            CheckBeats(true, "飞机 444555 压 333444",
                plane4455, PokerType.AeroplaneWithNone,
                plane3344, PokerType.AeroplaneWithNone);

            var plane3344w = Cards(
                Card(3, 0), Card(3, 1), Card(3, 2),
                Card(4, 0), Card(4, 1), Card(4, 2),
                Card(6), Card(7));
            var plane4455w = Cards(
                Card(4, 0), Card(4, 1), Card(4, 2),
                Card(5, 0), Card(5, 1), Card(5, 2),
                Card(8), Card(9));

            CheckBeats(true, "飞机带单 444555 压 333444",
                plane4455w, PokerType.AeroplaneWithTwo,
                plane3344w, PokerType.AeroplaneWithTwo);

            CheckBeats(false, "纯飞机压不了带翅膀飞机",
                plane4455, PokerType.AeroplaneWithNone,
                plane3344w, PokerType.AeroplaneWithTwo);

            var plane3 = Cards(
                Card(3, 0), Card(3, 1), Card(3, 2),
                Card(4, 0), Card(4, 1), Card(4, 2),
                Card(5, 0), Card(5, 1), Card(5, 2));
            var plane4 = Cards(
                Card(4, 0), Card(4, 1), Card(4, 2),
                Card(5, 0), Card(5, 1), Card(5, 2),
                Card(6, 0), Card(6, 1), Card(6, 2));

            CheckBeats(true, "三连飞机 444555666 压 333444555",
                plane4, PokerType.AeroplaneWithNone,
                plane3, PokerType.AeroplaneWithNone);
        }

        // ══════════════════ 发牌 ══════════════════

        private static void TestSeededDeal()
        {
            var cfg = new DdzConfig { Seed = 12345 };
            DealResult a = DdzDeck.Deal(cfg, 1);
            DealResult b = DdzDeck.Deal(cfg, 1);

            Check(Seq(a.Hands[0]).Equals(Seq(b.Hands[0])), "同种子第一手一致");
            Check(Seq(a.Hands[1]).Equals(Seq(b.Hands[1])), "同种子第二手一致");
            Check(Seq(a.Hands[2]).Equals(Seq(b.Hands[2])), "同种子第三手一致");
            Check(Seq(a.Bottom).Equals(Seq(b.Bottom)), "同种子底牌一致");
            Check(a.FirstBidder == b.FirstBidder, "同种子首叫一致");

            var cfg2 = new DdzConfig { Seed = 12346 };
            DealResult c = DdzDeck.Deal(cfg2, 1);
            Check(!Seq(a.Hands[0]).Equals(Seq(c.Hands[0])), "不同种子第一手不同");

            CheckDealInvariant(a, "随机洗牌");

            var noShuffle = new DdzConfig { Seed = 777, NoShuffle = true };
            DealResult d1 = DdzDeck.Deal(noShuffle, 1);
            DealResult d2 = DdzDeck.Deal(noShuffle, 1);
            Check(Seq(d1.Hands[0]).Equals(Seq(d2.Hands[0])), "不洗牌同种子一致");
            CheckDealInvariant(d1, "不洗牌");
        }

        private static void CheckDealInvariant(DealResult deal, string name)
        {
            var all = new HashSet<PokerCard>();

            for (int i = 0; i < 3; i++)
                Check(deal.Hands[i].Count == 17, $"{name}: 第 {i} 家 17 张");

            Check(deal.Bottom.Count == 3, $"{name}: 底牌 3 张");

            foreach (List<PokerCard> hand in deal.Hands)
                foreach (PokerCard card in hand)
                    Check(all.Add(card), $"{name}: 重复牌 {card}");

            foreach (PokerCard card in deal.Bottom)
                Check(all.Add(card), $"{name}: 底牌重复 {card}");

            Check(all.Count == 54, $"{name}: 共 54 张");
        }

        private static string Seq(IEnumerable<PokerCard> cards)
        {
            var parts = new List<string>();
            foreach (PokerCard card in cards)
                parts.Add(((int)card).ToString());
            return string.Join(",", parts);
        }

        // ══════════════════ 叫地主 ══════════════════

        private static void TestBidding()
        {
            var game = new DdzGame(new DdzConfig { Seed = 1 }, 0);
            game.Start();

            Check(game.Phase == GamePhase.Bidding, "开始时处于叫地主阶段");
            Check(game.CurrentPlayer == game.FirstBidder, "从首个叫牌人开始");
            Check(game.GetHandCount(0) == 17, "叫地主前 17 张");

            int bidder = game.CurrentPlayer;
            ActionResult r = game.Bid(bidder, true);

            Check(r.Success, "叫地主成功");
            Check(game.Landlord == bidder, "叫牌人成为地主");
            Check(game.Phase == GamePhase.Playing, "进入出牌阶段");
            Check(game.CurrentPlayer == bidder, "地主先出");
            Check(game.GetHandCount(bidder) == 20, "地主 20 张");
            Check(game.BottomCards.Count == 3, "底牌保留 3 张");
            Check(game.LastType == PokerType.None, "地主领出");

            var game2 = new DdzGame(new DdzConfig { Seed = 2 }, 0);
            game2.Start();

            int dealBefore = game2.DealNumber;
            int first = game2.CurrentPlayer;
            Check(game2.Bid(first, false).Success, "第一家不叫");
            Check(game2.Bid(game2.CurrentPlayer, false).Success, "第二家不叫");
            Check(game2.Bid(game2.CurrentPlayer, false).Success, "第三家不叫");

            Check(game2.DealNumber == dealBefore + 1, "三家不叫后重新发牌");
            Check(game2.Phase == GamePhase.Bidding, "重新发牌后回到叫地主");
            Check(game2.GetHandCount(0) == 17, "重整后仍是 17 张");

            Check(!game2.Bid(0, true).Success || game2.CurrentPlayer == 0, "非当前玩家不能叫");
        }

        // ══════════════════ 出牌流程 ══════════════════

        private static void TestPlayFlow()
        {
            var game = new DdzGame(new DdzConfig { Seed = 3 }, 0);
            game.Start();

            int landlord = game.CurrentPlayer;
            game.Bid(landlord, true);

            Check(!game.Pass(landlord).Success, "领出时不能不要");

            PokerCard first = game.GetHand(landlord)[0];
            ActionResult lead = game.Play(landlord, Cards(first));

            Check(lead.Success, "地主领出单张");
            Check(game.LastType == PokerType.Single, "记录最后牌型");
            Check(game.LastPlayer == landlord, "记录最后出牌人");
            Check(game.GetHandCount(landlord) == 19, "地主少一张");

            int p1 = game.CurrentPlayer;
            Check(game.Pass(p1).Success, "下家不要");
            int p2 = game.CurrentPlayer;
            Check(game.Pass(p2).Success, "对家不要");

            Check(game.CurrentPlayer == landlord, "两家不要后回到最后出牌人");
            Check(game.LastType == PokerType.None, "新一轮清空上家牌");
            Check(game.PassCount == 0, "过牌计数清零");

            Check(!game.Play(0, Cards(first)).Success || landlord == 0, "非当前玩家不能出牌");

            // 选牌不在手里
            PokerCard notInHand = FindNotInHand(game.GetHand(game.CurrentPlayer));
            ActionResult invalid = game.Play(game.CurrentPlayer, Cards(notInHand));
            Check(!invalid.Success && invalid.Error == GameErrors.NotInHand,
                "不能用别人的牌出牌");
        }

        private static PokerCard FindNotInHand(IReadOnlyList<PokerCard> hand)
        {
            var set = new HashSet<PokerCard>(hand);

            for (int i = 0; i < 52; i++)
            {
                PokerCard card = (PokerCard)i;
                if (!set.Contains(card))
                    return card;
            }

            return PokerCard.Red_Joker;
        }

        // ══════════════════ 记牌器 ══════════════════

        private static void TestCounter()
        {
            var game = new DdzGame(new DdzConfig { Seed = 4 }, 0);
            game.Start();

            int[] before = game.GetRemainingCounts(0);
            Check(Sum(before) == 54 - game.GetHandCount(0), "开局未出现牌 = 54 - 自己手牌");

            foreach (int count in before)
                Check(count >= 0, "记牌器没有负数");

            int landlord = game.CurrentPlayer;
            game.Bid(landlord, true);
            Check(Sum(game.GetRemainingCounts(0)) ==
                  54 - game.GetHandCount(0) - game.PlayedCards.Count,
                "叫地主后记牌器仍然守恒");
        }

        // ══════════════════ 两家不要后重新领出 ══════════════════

        private static void TestLeadAfterTwoPasses()
        {
            for (int seed = 1; seed <= 300; seed++)
            {
                var game = new DdzGame(new DdzConfig { Seed = seed }, 0);
                game.Start();

                int landlord = game.CurrentPlayer;
                game.Bid(landlord, true);

                List<PokerCard> hand = game.GetHandCopy(landlord);
                var groups = new Dictionary<int, List<PokerCard>>();

                foreach (PokerCard card in hand)
                {
                    int rank = DdzGame.RankCode(card);
                    if (!groups.TryGetValue(rank, out List<PokerCard> list))
                    {
                        list = new List<PokerCard>();
                        groups[rank] = list;
                    }
                    list.Add(card);
                }

                int pairRank = 0;
                foreach (KeyValuePair<int, List<PokerCard>> entry in groups)
                {
                    if (entry.Value.Count >= 2)
                    {
                        pairRank = entry.Key;
                        break;
                    }
                }

                if (pairRank == 0)
                    continue;

                var pair = new List<PokerCard>
                {
                    groups[pairRank][0],
                    groups[pairRank][1]
                };

                ActionResult pairResult = game.Play(landlord, pair);
                Check(pairResult.Success, $"seed {seed} 领出对子应该成功");
                if (!pairResult.Success)
                    return;

                Check(game.Pass(game.CurrentPlayer).Success, "第一家不要");
                Check(game.Pass(game.CurrentPlayer).Success, "第二家不要");

                Check(game.CurrentPlayer == landlord, "两家不要后回到最后出牌人");
                Check(game.LastMoveInfo == null, "两家不要后清空上家牌型");
                Check(game.LastType == PokerType.None, "两家不要后可以重新领出");

                List<PokerCard> remaining = game.GetHandCopy(landlord);
                if (remaining.Count == 0)
                    continue;

                var single = new List<PokerCard> { remaining[0] };
                ActionResult lead = game.Play(landlord, single);

                Check(lead.Success, $"seed {seed} 两家不要后领出单张应该成功，实际 {lead.Error}");
                return;
            }

            Fail("没有找到可用于测试的牌局");
        }

        private static int Sum(int[] values)
        {
            int total = 0;
            foreach (int value in values)
                total += value;
            return total;
        }

        // ══════════════════ 无头自对弈 ══════════════════

        private static void TestSelfPlay()
        {
            int games = 300;
            int landlordWins = 0;
            int farmerWins = 0;
            int redeals = 0;
            int totalPlies = 0;

            for (int seed = 1; seed <= games; seed++)
            {
                GameResult result = PlayOne(seed, false, out int plies, out int deals);
                if (result == null)
                {
                    Fail($"自对弈 seed={seed} 未正常结束");
                    continue;
                }

                totalPlies += plies;
                redeals += deals - 1;

                if (result.Winner == WinnerTeam.Landlord)
                    landlordWins++;
                else if (result.Winner == WinnerTeam.Farmers)
                    farmerWins++;
                else
                    Fail($"自对弈 seed={seed} 没有胜者");
            }

            Console.WriteLine(
                $"  随机洗牌：{games} 局，地主 {landlordWins} 胜，农民 {farmerWins} 胜，" +
                $"平均 {totalPlies / (double)games:F1} 手，重发 {redeals} 次");

            Check(landlordWins + farmerWins == games, "每局都有胜负");
            Check(landlordWins > 0 && farmerWins > 0, "地主/农民都有胜局");

            int noShuffleGames = 100;
            int nsLandlord = 0;
            int nsFarmer = 0;

            for (int seed = 1; seed <= noShuffleGames; seed++)
            {
                GameResult result = PlayOne(seed + 10000, true, out _, out _);
                if (result == null)
                {
                    Fail($"不洗牌自对弈 seed={seed} 未正常结束");
                    continue;
                }

                if (result.Winner == WinnerTeam.Landlord)
                    nsLandlord++;
                else if (result.Winner == WinnerTeam.Farmers)
                    nsFarmer++;
            }

            Console.WriteLine(
                $"  不洗牌：{noShuffleGames} 局，地主 {nsLandlord} 胜，农民 {nsFarmer} 胜");

            Check(nsLandlord + nsFarmer == noShuffleGames, "不洗牌每局都有胜负");
        }

        private static GameResult PlayOne(
            int seed, bool noShuffle, out int plies, out int deals)
        {
            plies = 0;
            int redealCount = 0;

            var config = new DdzConfig
            {
                Seed = seed,
                NoShuffle = noShuffle
            };

            var game = new DdzGame(config, 0);
            game.Start();
            int firstDeal = game.DealNumber;

            int guard = 0;

            while (game.Phase == GamePhase.Bidding)
            {
                if (++guard > 200)
                {
                    Fail($"[seed {seed}] 叫地主阶段死循环");
                    deals = redealCount + 1;
                    return null;
                }

                if (game.DealNumber != firstDeal)
                {
                    redealCount = game.DealNumber - firstDeal;
                }

                int player = game.CurrentPlayer;
                bool call = BasicAi.ShouldBid(game.GetHand(player));
                ActionResult result = game.Bid(player, call);

                if (!result.Success)
                {
                    Fail($"[seed {seed}] 叫地主失败：{result.Error}");
                    deals = redealCount + 1;
                    return null;
                }
            }

            guard = 0;

            while (game.Phase == GamePhase.Playing)
            {
                if (++guard > 3000)
                {
                    Fail($"[seed {seed}] 出牌阶段死循环");
                    deals = redealCount + 1;
                    return null;
                }

                int player = game.CurrentPlayer;
                AiMove move = BasicAi.ChooseMove(game, player);

                ActionResult result;
                if (move.IsPass)
                {
                    result = game.Pass(player);
                }
                else
                {
                    result = game.Play(player, move.Cards);
                }

                if (!result.Success)
                {
                    Fail(
                        $"[seed {seed}] 玩家 {player} 动作失败：{result.Error}，" +
                        $"牌型 {move.Type}，手牌 {Seq(game.GetHand(player))}，" +
                        $"上家 {game.LastType} {Seq(game.LastCards)}");
                    deals = redealCount + 1;
                    return null;
                }

                plies++;

                if (TotalCards(game) != 54)
                {
                    Fail($"[seed {seed}] 牌数不守恒：{TotalCards(game)}");
                    deals = redealCount + 1;
                    return null;
                }
            }

            deals = redealCount + 1;

            if (game.Phase != GamePhase.Finished)
            {
                Fail($"[seed {seed}] 未进入结束状态");
                return null;
            }

            return game.BuildResult();
        }

        private static int TotalCards(DdzGame game)
        {
            int total = game.PlayedCards.Count;

            for (int i = 0; i < 3; i++)
                total += game.GetHandCount(i);

            return total;
        }
    }
}
