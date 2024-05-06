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
        private ulong this[Player key] => Players[key];
        private ulong this[int index] => Players[(Player)(index + 1)];

        public enum Player : int
        {
            None,
            One,
            Two,
        }

        public ulong PlayingChannelId;
        // public List<ulong> Players { get; internal set; } = [];
        public Dictionary<Player, ulong> Players { get; internal set; } = [];
        
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
                Player plr = Players.FirstOrDefault(x => x.Value == playerId).Key;
                if (Turns == 0 && plr == Player.Two) {
                    Players = (Dictionary<Player, ulong>)Players.Reverse();
                    return Player.One;
                } else {
                    return plr;
                }
            }

            return Player.None;
        }

        public ulong OtherPlayerId()
        {
            return this[Turns % 2];
        }

        public (ulong, ulong) GetPlayerIds(ulong sentId)
        {
            if (Turns == 0) {
                int which = sentId == this[1] ? 0 : 1;
                return (sentId, this[which]);
            } else {
                return (this[(Turns) % 2], this[(Turns + 1) % 2]);
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
        // public struct Bet(ulong betterId, decimal betAmount)
        // {
        //     public ulong BetterId = betterId;
        //     public decimal BetAmount = betAmount;
        // }

        // public bool TryAddBet(ulong playerId, decimal betAmount)
        // {
        //     if (!Betters.TryFindIndex(b => b.BetterId == playerId, out int index)) {
        //         Betters.Add(new Bet(playerId, betAmount));
        //         return true;
        //     }
        //     return false;
        // }
        public Dictionary<ulong, Bet> Betters { get; internal set; } = [];
        public class Bet(decimal betAmount, ulong winnerGuess)
        {
            public decimal betAmount = betAmount;
            public readonly ulong winnerGuess = winnerGuess;
        }

        public void AddBet(ulong better, decimal betAmount, ulong winnerGuess)
        {
            // if already contains playerId, set a new betAmount
            if (!Betters.TryAdd(better, new(betAmount, winnerGuess))) {
                Betters[better].betAmount = betAmount;
            }
        }
        #endregion
    }
}
