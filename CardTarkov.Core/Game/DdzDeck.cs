using System;
using System.Collections.Generic;
using PokerUtils;

namespace CardTarkov.Core.Game
{
    public sealed class DealResult
    {
        public List<PokerCard>[] Hands { get; } =
            { new List<PokerCard>(20), new List<PokerCard>(20), new List<PokerCard>(20) };

        public List<PokerCard> Bottom { get; } = new List<PokerCard>(3);

        public int FirstBidder { get; set; }
    }

    /// <summary>
    /// 发牌。洗牌使用固定种子，保证同一 (seed, dealNumber) 的牌局可复现。
    /// 不洗牌模式沿用上游思路：先交换整组炸弹，再做少量单牌交换。
    /// </summary>
    public static class DdzDeck
    {
        public static List<PokerCard> CreateOrdered()
        {
            var deck = new List<PokerCard>(54);
            for (int i = 0; i < 52; i++)
                deck.Add((PokerCard)i);

            deck.Add(PokerCard.Black_Joker);
            deck.Add(PokerCard.Red_Joker);
            return deck;
        }

        public static DealResult Deal(DdzConfig config, int dealNumber)
        {
            var rng = new SeededRandom(MixSeed(config.Seed, dealNumber));
            List<PokerCard> deck = config.NoShuffle
                ? NoShuffle(rng, config.NoShuffleControlFactor)
                : Shuffle(rng);

            var result = new DealResult();

            // 上游：前 3 张为底牌，其余按 17 张一组切给三家。
            for (int i = 0; i < 3; i++)
                result.Bottom.Add(deck[i]);

            if (config.NoShuffle)
            {
                for (int i = 3; i < deck.Count; i++)
                    result.Hands[(i - 3) / 17].Add(deck[i]);
            }
            else
            {
                for (int i = 3; i < deck.Count; i++)
                    result.Hands[i % 3].Add(deck[i]);
            }

            result.FirstBidder = rng.Next(3);
            return result;
        }

        private static List<PokerCard> Shuffle(SeededRandom rng)
        {
            List<PokerCard> deck = CreateOrdered();

            for (int i = deck.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                PokerCard tmp = deck[i];
                deck[i] = deck[j];
                deck[j] = tmp;
            }

            return deck;
        }

        private static List<PokerCard> NoShuffle(SeededRandom rng, int controlFactor)
        {
            List<PokerCard> deck = CreateOrdered();

            // 1. 整组炸弹（4 张同点数）随机交换，炸弹结构得以保留。
            int groupSwaps = rng.Next(5, 20);
            for (int i = 0; i < groupSwaps; i++)
            {
                int rankA = rng.Next(3, 16);
                int rankB = rng.Next(3, 16);
                SwapGroups(deck, rankA, rankB);
            }

            // 2. 少量单牌交换，控制炸弹被拆散的概率。
            int singleSwaps = rng.Next(0, Math.Max(0, controlFactor) + 1);
            for (int i = 0; i < singleSwaps; i++)
            {
                int a = rng.Next(deck.Count);
                int b = rng.Next(deck.Count);
                PokerCard tmp = deck[a];
                deck[a] = deck[b];
                deck[b] = tmp;
            }

            return deck;
        }

        private static void SwapGroups(List<PokerCard> deck, int rankA, int rankB)
        {
            int a = (rankA - 3) * 4;
            int b = (rankB - 3) * 4;

            for (int i = 0; i < 4; i++)
            {
                PokerCard tmp = deck[a + i];
                deck[a + i] = deck[b + i];
                deck[b + i] = tmp;
            }
        }

        private static ulong MixSeed(int seed, int dealNumber)
        {
            ulong x = (uint)seed;
            x ^= (ulong)(uint)dealNumber * 0x9E3779B97F4A7C15UL;
            x += 0x9E3779B97F4A7C15UL;
            x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
            x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
            return x ^ (x >> 31);
        }
    }
}
