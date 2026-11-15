using System;
using System.Collections.Generic;
using PokerUtils;

namespace CardTarkov.Core.Game
{
    public sealed class MoveInfo
    {
        public bool IsValid { get; }
        public PokerType Type { get; }
        public int MainRank { get; }
        public int Length { get; }
        public int CardCount { get; }

        private MoveInfo(bool valid, PokerType type, int mainRank, int length, int cardCount)
        {
            IsValid = valid;
            Type = type;
            MainRank = mainRank;
            Length = length;
            CardCount = cardCount;
        }

        public static MoveInfo Invalid =>
            new MoveInfo(false, PokerType.None, 0, 0, 0);

        public static MoveInfo Valid(
            PokerType type, int mainRank, int length, int cardCount) =>
            new MoveInfo(true, type, mainRank, length, cardCount);
    }

    /// <summary>
    /// 斗地主牌型识别与比较。
    ///
    /// <para>
    /// 上游 ProkerUtils 的 GetPokerType/Check 对飞机（尤其 6 张纯飞机和带翅膀）
    /// 支持不完整，因此这里用独立的规则实现；PokerCard / PokerType 仍复用上游枚举，
    /// 提示仍复用 GetTipCards（结果会经过本类校验）。
    /// </para>
    /// </summary>
    public static class DdzRules
    {
        public static PokerType GetPokerType(IList<PokerCard> cards) =>
            Analyze(cards).Type;

        public static MoveInfo Analyze(IList<PokerCard> cards)
        {
            if (cards == null || cards.Count == 0)
                return MoveInfo.Invalid;

            int count = cards.Count;
            var counts = new int[18];

            foreach (PokerCard card in cards)
            {
                int value = (int)card;

                if (!((value >= 0 && value <= 52) || value == 56))
                    return MoveInfo.Invalid;

                int rank = value / 4 + 3;
                counts[rank]++;
            }

            // 每种点数最多 4 张；大小王各 1 张
            for (int rank = 3; rank <= 15; rank++)
            {
                if (counts[rank] > 4)
                    return MoveInfo.Invalid;
            }

            if (counts[16] > 1 || counts[17] > 1)
                return MoveInfo.Invalid;

            if (count == 2 && counts[16] == 1 && counts[17] == 1)
                return MoveInfo.Valid(PokerType.KingBomb, 17, 1, count);

            int quad = FindRank(counts, 4);
            if (count == 4 && quad > 0)
                return MoveInfo.Valid(PokerType.Bomb, quad, 1, count);

            if (count == 1)
                return MoveInfo.Valid(PokerType.Single, FindRank(counts, 1), 1, count);

            if (count == 2)
            {
                int pair = FindRank(counts, 2);
                if (pair > 0)
                    return MoveInfo.Valid(PokerType.Double, pair, 1, count);
            }

            if (count == 3)
            {
                int triple = FindRank(counts, 3);
                if (triple > 0)
                    return MoveInfo.Valid(PokerType.ThreeWithNone, triple, 1, count);
            }

            if (count == 4)
            {
                int triple = FindRank(counts, 3);
                if (triple > 0 && HasSingleOutside(counts, triple))
                    return MoveInfo.Valid(PokerType.ThreeWithOne, triple, 1, count);
            }

            if (count == 5)
            {
                int triple = FindRank(counts, 3);
                if (triple > 0 && HasPairOutside(counts, triple))
                    return MoveInfo.Valid(PokerType.ThreeWihtDouble, triple, 1, count);
            }

            if (count >= 5 && IsStraight(counts, count))
                return MoveInfo.Valid(PokerType.ShunZi, HighestRank(counts), count, count);

            if (count >= 6 && count % 2 == 0 && IsDoubleStraight(counts, count))
            {
                return MoveInfo.Valid(
                    PokerType.LianDui, HighestRank(counts), count / 2, count);
            }

            if (count >= 6)
            {
                MoveInfo airplane = TryAirplane(counts, count);
                if (airplane.IsValid)
                    return airplane;
            }

            if (count == 6 && quad > 0)
                return MoveInfo.Valid(PokerType.FourWithTwo, quad, 1, count);

            if (count == 8 && quad > 0 && IsTwoPairsOutside(counts, quad))
                return MoveInfo.Valid(PokerType.FourWithTwoDouble, quad, 1, count);

            return MoveInfo.Invalid;
        }

        public static bool CanBeat(MoveInfo current, MoveInfo last)
        {
            if (current == null || !current.IsValid)
                return false;

            if (last == null || !last.IsValid)
                return true;

            if (current.Type == PokerType.KingBomb)
                return last.Type != PokerType.KingBomb;

            if (last.Type == PokerType.KingBomb)
                return false;

            if (current.Type == PokerType.Bomb)
            {
                return last.Type != PokerType.Bomb ||
                       current.MainRank > last.MainRank;
            }

            if (last.Type == PokerType.Bomb)
                return false;

            if (current.Type != last.Type)
                return false;

            if (current.CardCount != last.CardCount)
                return false;

            if (IsSequence(current.Type) && current.Length != last.Length)
                return false;

            return current.MainRank > last.MainRank;
        }

        public static bool IsBomb(MoveInfo move) =>
            move != null && move.IsValid &&
            (move.Type == PokerType.Bomb || move.Type == PokerType.KingBomb);

        // ══════════════════ 牌型辅助 ══════════════════

        private static bool IsSequence(PokerType type)
        {
            return type == PokerType.ShunZi ||
                   type == PokerType.LianDui ||
                   type == PokerType.AeroplaneWithNone ||
                   type == PokerType.AeroplaneWithTwo ||
                   type == PokerType.AeroplaneWithThree ||
                   type == PokerType.AeroplaneWithFour ||
                   type == PokerType.AeroplaneWithFive ||
                   type == PokerType.AeroplaneWithTwoDouble ||
                   type == PokerType.AeroplaneWithThreeDouble ||
                   type == PokerType.AeroplaneWithFourDouble;
        }

        private static int FindRank(int[] counts, int expected)
        {
            for (int rank = 3; rank <= 17; rank++)
            {
                if (counts[rank] == expected)
                    return rank;
            }

            return 0;
        }

        private static bool HasSingleOutside(int[] counts, int except)
        {
            for (int rank = 3; rank <= 17; rank++)
            {
                if (rank != except && counts[rank] == 1)
                    return true;
            }

            return false;
        }

        private static bool HasPairOutside(int[] counts, int except)
        {
            for (int rank = 3; rank <= 17; rank++)
            {
                if (rank != except && counts[rank] == 2)
                    return true;
            }

            return false;
        }

        private static bool IsTwoPairsOutside(int[] counts, int quadRank)
        {
            int pairs = 0;

            for (int rank = 3; rank <= 17; rank++)
            {
                if (rank == quadRank)
                    continue;

                if (counts[rank] == 0)
                    continue;

                if (counts[rank] != 2)
                    return false;

                pairs++;
            }

            return pairs == 2;
        }

        private static bool IsStraight(int[] counts, int total)
        {
            var ranks = new List<int>();

            for (int rank = 3; rank <= 17; rank++)
            {
                if (counts[rank] == 0)
                    continue;

                if (counts[rank] != 1)
                    return false;

                ranks.Add(rank);
            }

            if (ranks.Count != total || ranks.Count < 5)
                return false;

            if (ranks[ranks.Count - 1] > 14)
                return false;

            for (int i = 1; i < ranks.Count; i++)
            {
                if (ranks[i] != ranks[i - 1] + 1)
                    return false;
            }

            return true;
        }

        private static bool IsDoubleStraight(int[] counts, int total)
        {
            var ranks = new List<int>();

            for (int rank = 3; rank <= 17; rank++)
            {
                if (counts[rank] == 0)
                    continue;

                if (counts[rank] != 2)
                    return false;

                ranks.Add(rank);
            }

            if (ranks.Count != total / 2 || ranks.Count < 3)
                return false;

            if (ranks[ranks.Count - 1] > 14)
                return false;

            for (int i = 1; i < ranks.Count; i++)
            {
                if (ranks[i] != ranks[i - 1] + 1)
                    return false;
            }

            return true;
        }

        private static int HighestRank(int[] counts)
        {
            for (int rank = 17; rank >= 3; rank--)
            {
                if (counts[rank] > 0)
                    return rank;
            }

            return 0;
        }

        private static MoveInfo TryAirplane(int[] counts, int total)
        {
            var candidates = new List<int>();

            for (int rank = 3; rank <= 14; rank++)
            {
                if (counts[rank] == 3)
                    candidates.Add(rank);
            }

            // 拆成连续段
            int index = 0;

            while (index < candidates.Count)
            {
                int runStart = index;

                while (index + 1 < candidates.Count &&
                       candidates[index + 1] == candidates[index] + 1)
                {
                    index++;
                }

                int runEnd = index;
                index++;

                int runLength = runEnd - runStart + 1;

                // 从最长的主体开始尝试
                for (int length = runLength; length >= 2; length--)
                {
                    for (int start = runStart; start + length - 1 <= runEnd; start++)
                    {
                        MoveInfo result = TryAirplaneBody(
                            counts, total, candidates, start, length);

                        if (result.IsValid)
                            return result;
                    }
                }
            }

            return MoveInfo.Invalid;
        }

        private static MoveInfo TryAirplaneBody(
            int[] counts, int total, List<int> candidates, int start, int length)
        {
            var remaining = (int[])counts.Clone();

            for (int i = 0; i < length; i++)
                remaining[candidates[start + i]] -= 3;

            int wingCount = total - 3 * length;

            int mainRank = candidates[start + length - 1];

            if (wingCount == 0)
                return MoveInfo.Valid(PokerType.AeroplaneWithNone, mainRank, length, total);

            if (wingCount == length)
            {
                PokerType singleType = SingleWingType(length);
                if (singleType != PokerType.None)
                    return MoveInfo.Valid(singleType, mainRank, length, total);
            }

            if (wingCount == 2 * length && IsPairWings(remaining, length))
            {
                PokerType pairType = PairWingType(length);
                if (pairType != PokerType.None)
                    return MoveInfo.Valid(pairType, mainRank, length, total);
            }

            return MoveInfo.Invalid;
        }

        private static bool IsPairWings(int[] remaining, int length)
        {
            int pairs = 0;

            for (int rank = 3; rank <= 17; rank++)
            {
                if (remaining[rank] == 0)
                    continue;

                if (remaining[rank] != 2)
                    return false;

                pairs++;
            }

            return pairs == length;
        }

        private static PokerType SingleWingType(int length)
        {
            switch (length)
            {
                case 2: return PokerType.AeroplaneWithTwo;
                case 3: return PokerType.AeroplaneWithThree;
                case 4: return PokerType.AeroplaneWithFour;
                case 5: return PokerType.AeroplaneWithFive;
                default: return PokerType.None;
            }
        }

        private static PokerType PairWingType(int length)
        {
            switch (length)
            {
                case 2: return PokerType.AeroplaneWithTwoDouble;
                case 3: return PokerType.AeroplaneWithThreeDouble;
                case 4: return PokerType.AeroplaneWithFourDouble;
                default: return PokerType.None;
            }
        }
    }
}
