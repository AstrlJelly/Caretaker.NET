using System;
using System.Diagnostics;
using System.Numerics;
using System.Text;

using Discord.WebSocket;

using CaretakerNET.Core;

namespace CaretakerNET.Games
{
    public abstract class BoardGame
    {
        public enum Player : int
        {
            None,
            One,
            Two,
            // Three,
            // Four,
        }

        public ulong PlayingChannelId;
        public ulong Player1;
        public ulong Player2;
        // public Player CurrentPlayer = Player.None;
        public int turns = 0;

        public void SwitchPlayers(ulong player)
        {
            turns++;
        }

        public Player GetWhichPlayer(ulong playerId)
        {
            if (playerId == Player1) {
                return Player.One;
            } else if (playerId == Player2) {
                return Player.Two;
            } else {
                return Player.None;
            }
        }

        public bool IsCurrentPlayer(Player player)
        {
            var whichPlayer = (turns % 2) + 1;
            return turns == 0 || (int)player == whichPlayer;
        }
    }
}
