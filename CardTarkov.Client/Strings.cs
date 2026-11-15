using System.Collections.Generic;
using System.Text;
using CardTarkov.Core.Game;
using PokerUtils;

namespace CardTarkov.Client
{
    internal static class Strings
    {
        internal const string TabName = "棋牌室";
        internal const string PanelTitle = "塔科夫棋牌室";
        internal const string GameTitle = "斗地主";

        internal const string Settings = "牌局设置";
        internal const string NoShuffle = "不洗牌";
        internal const string NoShuffleOn = "开（炸弹更多）";
        internal const string NoShuffleOff = "关（完全随机）";
        internal const string AiStrength = "AI 强度";
        internal const string AiNormal = "普通（暂定）";
        internal const string Seed = "随机种子";
        internal const string SeedPlaceholder = "留空则随机";
        internal const string MaxMultiplier = "最大倍数";
        internal const string MaxMultiplierDisabled = "暂未启用";
        internal const string StartGame = "开始牌局";
        internal const string RulesHint =
            "首版规则：叫地主、出牌/不要。\n" +
            "炸弹/王炸可以出，但暂不翻倍；抢地主、加倍后续开放。\n" +
            "切换背包页签会立即结束当前牌局。";

        internal const string CallLandlord = "叫地主";
        internal const string NoCall = "不叫";
        internal const string Play = "出牌";
        internal const string Pass = "不要";
        internal const string Hint = "提示";
        internal const string ExitGame = "退出牌局";
        internal const string Again = "再来一局";
        internal const string BackToSettings = "返回设置";

        internal const string You = "你";
        internal const string RightSeat = "下家";
        internal const string LeftSeat = "上家";
        internal const string Landlord = "地主";
        internal const string Farmer = "农民";
        internal const string Computer = "电脑";

        internal const string WaitingAi = "电脑思考中…";
        internal const string YourBid = "轮到你叫地主";
        internal const string YourTurn = "轮到你出牌";
        internal const string NewTrick = "新一轮，由 {0} 领出";
        internal const string LastPlayBy = "{0} 出牌";
        internal const string NoCardsToBeat = "没有能管上的牌";
        internal const string SelectCards = "请先选牌";
        internal const string Win = "你赢了！";
        internal const string Lose = "你输了";
        internal const string RoleLine = "你：{0}　·　{1}";
        internal const string SeedLine = "种子 {0}　·　第 {1} 局";
        internal const string MultiplierLine = "倍数 {0}（首版固定）";
        internal const string DealLine = "第 {0} 局";

        internal const string ErrorNotYourTurn = "还没轮到你";
        internal const string ErrorInvalidType = "这不是合法牌型";
        internal const string ErrorMustBeat = "管不上上家的牌";
        internal const string ErrorCannotPass = "你是领出方，不能不要";
        internal const string ErrorNotInHand = "选中的牌不在手里";
        internal const string ErrorNoCards = "请先选牌";
        internal const string ErrorGameOver = "牌局已结束";
        internal const string ErrorPhase = "当前阶段不能这样操作";
        internal const string ErrorSeed = "种子必须是整数";

        internal static string Error(string code)
        {
            switch (code)
            {
                case GameErrors.NotYourTurn: return ErrorNotYourTurn;
                case GameErrors.InvalidType: return ErrorInvalidType;
                case GameErrors.MustBeat: return ErrorMustBeat;
                case GameErrors.CannotPass: return ErrorCannotPass;
                case GameErrors.NotInHand: return ErrorNotInHand;
                case GameErrors.NoCards: return ErrorNoCards;
                case GameErrors.GameOver: return ErrorGameOver;
                case GameErrors.NotBidding:
                case GameErrors.NotPlaying:
                    return ErrorPhase;
                default: return "操作失败";
            }
        }

        internal static int RankCode(PokerCard card) => (int)card / 4 + 3;

        internal static string RankName(PokerCard card) => RankName(RankCode(card));

        internal static string RankName(int rank)
        {
            switch (rank)
            {
                case 11: return "J";
                case 12: return "Q";
                case 13: return "K";
                case 14: return "A";
                case 15: return "2";
                case 16: return "小王";
                case 17: return "大王";
                default: return rank.ToString();
            }
        }

        internal static bool IsJoker(PokerCard card) => RankCode(card) >= 16;

        internal static bool IsRed(PokerCard card)
        {
            int rank = RankCode(card);
            if (rank == 17) return true;
            if (rank == 16) return false;
            int suit = (int)card % 4; // 0 黑桃 1 红桃 2 梅花 3 方块
            return suit == 1 || suit == 3;
        }

        /// <summary>0 黑桃 1 红桃 2 梅花 3 方块</summary>
        internal static int Suit(PokerCard card) => (int)card % 4;

        internal static string SeatName(int player)
        {
            if (player == 1) return RightSeat;
            if (player == 2) return LeftSeat;
            return You;
        }

        internal static string RoleName(int player, int landlord) =>
            player == landlord ? Landlord : Farmer;

        internal static string TypeName(PokerType type)
        {
            switch (type)
            {
                case PokerType.Single: return "单张";
                case PokerType.Double: return "对子";
                case PokerType.ThreeWithNone: return "三张";
                case PokerType.ThreeWithOne: return "三带一";
                case PokerType.ThreeWihtDouble: return "三带二";
                case PokerType.ShunZi: return "顺子";
                case PokerType.LianDui: return "连对";
                case PokerType.AeroplaneWithNone: return "飞机";
                case PokerType.AeroplaneWithTwo: return "飞机带两张";
                case PokerType.AeroplaneWithThree: return "飞机带三张";
                case PokerType.AeroplaneWithFour: return "飞机带四张";
                case PokerType.AeroplaneWithFive: return "飞机带五张";
                case PokerType.AeroplaneWithTwoDouble: return "飞机带两对";
                case PokerType.AeroplaneWithThreeDouble: return "飞机带三对";
                case PokerType.AeroplaneWithFourDouble: return "飞机带四对";
                case PokerType.FourWithTwo: return "四带二";
                case PokerType.FourWithTwoDouble: return "四带两对";
                case PokerType.Bomb: return "炸弹";
                case PokerType.KingBomb: return "王炸";
                default: return "未知牌型";
            }
        }

        /// <summary>记牌器文本：3:4  4:3 … 小王:1  大王:0</summary>
        internal static string Counter(int[] counts)
        {
            var sb = new StringBuilder();
            sb.Append("记牌器　");

            for (int i = 0; i < 13; i++)
            {
                if (i > 0) sb.Append("  ");
                sb.Append(RankName(i + 3));
                sb.Append(':');
                sb.Append(counts[i]);
            }

            sb.Append("  小王:");
            sb.Append(counts[13]);
            sb.Append("  大王:");
            sb.Append(counts[14]);
            return sb.ToString();
        }

        internal static string CardsText(IReadOnlyList<PokerCard> cards)
        {
            var parts = new List<string>();

            foreach (PokerCard card in cards)
                parts.Add(RankName(card));

            return string.Join(" ", parts);
        }
    }
}
