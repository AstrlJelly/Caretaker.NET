using System;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;

namespace CaretakerNET.Games
{
    // weird. but it works!
    [JsonDerivedType(typeof(ConnectFour), typeDiscriminator: "ConnectFour")]
    [JsonDerivedType(typeof(Checkers), typeDiscriminator: "Checkers")]
    public abstract class BoardGame
    {
        public enum Player : int
        {
            None,
            One,
            Two,
        }

        public ulong PlayingChannelId;
        // internal ulong[]? allPlayers;
        public List<ulong> Players { get; internal set; } = [];
        
        public int Turns { get; internal set; } = 0;
        public ulong ForfeitPlayer { get; internal set; }
        public int EndAt { get; internal set; } = int.MaxValue;

        public void SwitchPlayers()
        {
            Turns++;
        }

        public Player GetWhichPlayer(ulong playerId)
        {
            if (Players != null) {
                var pIndex = Players.FindIndex(x => x == playerId);
                if (Turns == 0 && pIndex != 0) {
                    Players.Reverse();
                    return Player.One;
                } else {
                    return (Player)(pIndex + 1);
                }
            }

            return Player.None;
        }

        public ulong OtherPlayerId()
        {
            return Players[Turns % 2];
        }

        public (ulong, ulong) GetPlayerIds(ulong sentId)
        {
            if (Turns == 0) {
                int which = sentId == Players[1] ? 0 : 1;
                return (sentId, Players[which]);
            } else {
                return (Players[(Turns) % 2], Players[(Turns + 1) % 2]);
            }
        }

        public bool StartForfeit(ulong playerId)
        {
            if (EndAt < int.MaxValue) return false;

            EndAt = Turns + 3;
            ForfeitPlayer = playerId;
            return true;
        }

        #region Betting
        // public List<Bet> Betters { get; internal set; } = [];
        // public struct Bet(ulong betterId, long betAmount)
        // {
        //     public ulong BetterId = betterId;
        //     public long BetAmount = betAmount;
        // }

        // public bool TryAddBet(ulong playerId, long betAmount)
        // {
        //     if (!Betters.TryFindIndex(b => b.BetterId == playerId, out int index)) {
        //         Betters.Add(new Bet(playerId, betAmount));
        //         return true;
        //     }
        //     return false;
        // }
        public Dictionary<ulong, Bet> Betters { get; internal set; } = [];
        public class Bet(long betAmount, ulong winnerGuess)
        {
            public long betAmount = betAmount;
            public readonly ulong winnerGuess = winnerGuess;
        }

        public void AddBet(ulong better, long betAmount, ulong winnerGuess)
        {
            // if already contains playerId, set a new betAmount
            if (!Betters.TryAdd(better, new(betAmount, winnerGuess))) {
                Betters[better].betAmount = betAmount;
            }
        }
        #endregion
    }
}
