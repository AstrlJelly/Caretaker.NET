using System;
using System.Diagnostics;
using System.Numerics;
using System.Text;

using CaretakerNET.Core;

namespace CaretakerNET.Games
{
    // // weird. but it works... maybe?
    // [JsonDerivedType(typeof(BoardGame), typeDiscriminator: "BoardGame")]
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
        public List<ulong>? Players { get; internal set; }
        public int Turns { get; internal set; } = 0;
        public ulong ForfeitPlayer { get; internal set; }
        public int EndAt { get; internal set; } = int.MaxValue;
        public const int FORFEIT_TURNS = 3;

        public void SwitchPlayers()
        {
            Turns++;
        }

        public Player GetWhichPlayer(ulong playerId)
        {
            if (/* allPlayers != null &&  */Players != null) {
                if (playerId == Players[0]) {
                    return Player.One;
                } else if (playerId == Players[1]) {
                    if (Turns == 0) {
                        Players.Reverse();
                        return Player.One;
                    }
                    return Player.Two;
                }
            }

            return Player.None;
        }

        public bool IsAnyPlayer(ulong id)
        {
            return Players?.Contains(id) ?? false;
        }

        public bool IsCurrentPlayer(Player player)
        {
            var whichPlayer = (Turns % 2) + 1;
            return Turns == 0 || (int)player == whichPlayer;
        }

        public bool StartForfeit(ulong playerId)
        {
            if (EndAt < int.MaxValue) return false;

            EndAt = Turns + 3;
            ForfeitPlayer = playerId;
            return true;
        }
    }
}
