using System;
using System.Collections.Generic;
using PokerUtils;

namespace CardTarkov.Core.Game
{
    public enum GamePhase
    {
        Bidding,
        Playing,
        Finished
    }

    public enum WinnerTeam
    {
        None,
        Landlord,
        Farmers
    }

    public enum GameEventKind
    {
        Deal,
        Redeal,
        BidPass,
        BidCall,
        LandlordSet,
        Play,
        Pass,
        TrickReset,
        Finished
    }

    public sealed class GameEvent
    {
        public GameEventKind Kind { get; }
        public int Player { get; }
        public IReadOnlyList<PokerCard> Cards { get; }
        public PokerType Type { get; }

        public GameEvent(
            GameEventKind kind, int player = -1,
            IReadOnlyList<PokerCard> cards = null, PokerType type = PokerType.None)
        {
            Kind = kind;
            Player = player;
            Cards = cards ?? Array.Empty<PokerCard>();
            Type = type;
        }
    }

    public sealed class ActionResult
    {
        public bool Success { get; }
        public string Error { get; }

        private ActionResult(bool success, string error)
        {
            Success = success;
            Error = error;
        }

        public static ActionResult Ok() => new ActionResult(true, null);
        public static ActionResult Fail(string error) => new ActionResult(false, error);
    }

    public static class GameErrors
    {
        public const string NotBidding = "not_bidding";
        public const string NotPlaying = "not_playing";
        public const string NotYourTurn = "not_your_turn";
        public const string NoCards = "no_cards";
        public const string NotInHand = "not_in_hand";
        public const string InvalidType = "invalid_type";
        public const string MustBeat = "must_beat";
        public const string CannotPass = "cannot_pass";
        public const string GameOver = "game_over";
    }

    public sealed class GameResult
    {
        public int Seed { get; set; }
        public int DealNumber { get; set; }
        public int HumanPlayer { get; set; }
        public int Landlord { get; set; }
        public bool HumanIsLandlord => HumanPlayer == Landlord;
        public bool HumanWon { get; set; }
        public WinnerTeam Winner { get; set; }
        public int Multiplier { get; set; }
        public int MaxMultiplier { get; set; }
    }

    public sealed class HintResult
    {
        public List<PokerCard> Cards { get; }
        public PokerType Type { get; }

        public HintResult(List<PokerCard> cards, PokerType type)
        {
            Cards = cards ?? new List<PokerCard>();
            Type = type;
        }
    }

    public sealed class AiMove
    {
        public bool IsPass { get; }
        public List<PokerCard> Cards { get; }
        public PokerType Type { get; }

        private AiMove(bool pass, List<PokerCard> cards, PokerType type)
        {
            IsPass = pass;
            Cards = cards ?? new List<PokerCard>();
            Type = type;
        }

        public static AiMove Pass() => new AiMove(true, null, PokerType.None);

        public static AiMove Play(List<PokerCard> cards, PokerType type) =>
            new AiMove(false, cards, type);
    }
}
