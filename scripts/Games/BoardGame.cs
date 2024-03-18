using System;
using System.Diagnostics;
using System.Numerics;
using System.Text;

using Discord.WebSocket;

using CaretakerNET.Core;
using System.Text.Json.Serialization;

namespace CaretakerNET.Games
{
    // weird. but it works
    [JsonDerivedType(typeof(ConnectFour), typeDiscriminator: "connect4")]
    [JsonDerivedType(typeof(Checkers), typeDiscriminator: "checkers")]
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
        public List<ulong>? Players;
        public int turns = 0;

        public void SwitchPlayers()
        {
            turns++;
        }

        public Player GetWhichPlayer(ulong playerId)
        {
            if (/* allPlayers != null &&  */Players != null) {
                if (playerId == Players[0]) {
                    return Player.One;
                } else if (playerId == Players[1]) {
                    if (turns == 0) {
                        Players.Reverse();
                        return Player.One;
                    }
                    return Player.Two;
                }
            }

            return Player.None;
        }

        public bool IsCurrentPlayer(Player player)
        {
            var whichPlayer = (turns % 2) + 1;
            return turns == 0 || (int)player == whichPlayer;
        }
    }
}
