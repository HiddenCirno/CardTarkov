using System;
using System.Collections.Generic;
using CardTarkov.Core.Game;
using PokerUtils;

namespace CardTarkov.Core.Ai
{
    /// <summary>
    /// v0.1 基础 AI：
    /// <list type="bullet">
    ///   <item>叫地主按手牌强度评分</item>
    ///   <item>跟牌用 ProkerUtils 的提示，但会留炸弹</item>
    ///   <item>农民不压队友；对手快走完时才交炸弹</item>
    ///   <item>领出优先走低点数单/对/三，不主动拆炸弹</item>
    /// </list>
    /// </summary>
    public static class BasicAi
    {
        private const int BidThreshold = 5;

        public static bool ShouldBid(IReadOnlyList<PokerCard> hand)
        {
            int score = 0;
            var counts = new int[18];

            foreach (PokerCard card in hand)
            {
                int rank = DdzGame.RankCode(card);
                counts[rank]++;

                if (rank == 17) score += 4;      // 大王
                else if (rank == 16) score += 3; // 小王
                else if (rank == 15) score += 2; // 2
                else if (rank == 14) score += 1; // A
            }

            if (counts[16] > 0 && counts[17] > 0)
                score += 6; // 王炸

            for (int rank = 3; rank <= 15; rank++)
            {
                if (counts[rank] == 4)
                    score += 5;
            }

            return score >= BidThreshold;
        }

        public static AiMove ChooseMove(DdzGame game, int player)
        {
            if (game == null || game.Phase != GamePhase.Playing || game.CurrentPlayer != player)
                return AiMove.Pass();

            return game.LastType == PokerType.None
                ? ChooseLead(game.GetHandCopy(player))
                : ChooseFollow(game, player);
        }

        private static AiMove ChooseFollow(DdzGame game, int player)
        {
            List<PokerCard> hand = game.GetHandCopy(player);

            // 农民不压队友（地主出牌时才需要跟）
            bool isFarmer = player != game.Landlord;
            bool lastIsTeammate = isFarmer &&
                                  game.LastPlayer >= 0 &&
                                  game.LastPlayer != game.Landlord &&
                                  game.LastPlayer != player;

            if (lastIsTeammate)
                return AiMove.Pass();

            var tip = new List<PokerCard>();
            PokerHelper.GetTipCards(
                hand, game.GetLastCardsCopy(), game.LastType, tip, out PokerType type);

            if (type == PokerType.None || tip.Count == 0)
                return AiMove.Pass();

            // 上游提示结果用本地规则再校验一次（飞机等类型）
            MoveInfo info = DdzRules.Analyze(tip);
            if (!info.IsValid || !DdzRules.CanBeat(info, game.LastMoveInfo))
                return AiMove.Pass();

            bool isBomb = DdzRules.IsBomb(info);

            if (isBomb && !ShouldUseBomb(game, player, tip.Count))
                return AiMove.Pass();

            tip.Sort((a, b) => ((int)a).CompareTo((int)b));
            return AiMove.Play(tip, info.Type);
        }

        private static bool ShouldUseBomb(DdzGame game, int player, int playCount)
        {
            // 一手炸弹能直接把牌走完，直接用
            if (game.GetHandCount(player) == playCount)
                return true;

            bool isLandlord = player == game.Landlord;

            if (isLandlord)
            {
                // 任意农民快走完
                for (int p = 0; p < 3; p++)
                {
                    if (p != player && game.GetHandCount(p) <= 2)
                        return true;
                }
            }
            else
            {
                // 地主快走完
                if (game.GetHandCount(game.Landlord) <= 2)
                    return true;
            }

            // 刚出牌的人快走完
            if (game.LastPlayer >= 0 &&
                game.LastPlayer != player &&
                game.GetHandCount(game.LastPlayer) <= 2)
            {
                return true;
            }

            return false;
        }

        private static AiMove ChooseLead(List<PokerCard> hand)
        {
            var groups = new Dictionary<int, List<PokerCard>>();
            var order = new List<int>();

            foreach (PokerCard card in hand)
            {
                int rank = DdzGame.RankCode(card);

                if (!groups.TryGetValue(rank, out List<PokerCard> list))
                {
                    list = new List<PokerCard>(4);
                    groups[rank] = list;
                    order.Add(rank);
                }

                list.Add(card);
            }

            order.Sort();

            // 优先走最低的非炸弹、非王炸组
            for (int i = 0; i < order.Count; i++)
            {
                int rank = order[i];
                int count = groups[rank].Count;

                if (rank >= 16 || count == 4)
                    continue;

                if (count == 1 || count == 2 || count == 3)
                    return AiMove.Play(SortedCopy(groups[rank]), TypeOfGroup(count));
            }

            // 只剩炸弹/王炸：先走最小的炸弹，最后才是王炸
            for (int i = 0; i < order.Count; i++)
            {
                int rank = order[i];
                if (groups[rank].Count == 4)
                    return AiMove.Play(SortedCopy(groups[rank]), PokerType.Bomb);
            }

            if (groups.ContainsKey(16) && groups.ContainsKey(17))
            {
                var rocket = new List<PokerCard> { PokerCard.Black_Joker, PokerCard.Red_Joker };
                return AiMove.Play(rocket, PokerType.KingBomb);
            }

            // 理论上不会走到这里；兜底出最小单张
            var fallback = new List<PokerCard> { hand[0] };
            return AiMove.Play(fallback, PokerType.Single);
        }

        private static PokerType TypeOfGroup(int count)
        {
            if (count == 1) return PokerType.Single;
            if (count == 2) return PokerType.Double;
            return PokerType.ThreeWithNone;
        }

        private static List<PokerCard> SortedCopy(List<PokerCard> cards)
        {
            var copy = new List<PokerCard>(cards);
            copy.Sort((a, b) => ((int)a).CompareTo((int)b));
            return copy;
        }
    }
}
