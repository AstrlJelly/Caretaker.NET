using System;
using System.Diagnostics;
using System.Numerics;
using System.Text;

using Discord.WebSocket;



namespace CaretakerNET.Games
{
    public abstract class CardGame
    {
        public enum Player : int
        {
            None,
            One,
            Two,
            Three,
            Four,
        }

        public ulong PlayingChannelId;
        public required ulong[] allPlayers;
        public List<ulong>? Players;
        public int turns = 0;

        public void SwitchPlayers()
        {
            turns++;
        }

        public Player GetWhichPlayer(ulong playerId)
        {
            // might be working? hasn't been tested
            if (allPlayers != null && Players != null) {
                int playerIndex = Players.FindIndex(id => id == playerId);
                if (playerIndex == -1) {
                    Players.Add(playerId);
                    return (Player)(Players.Count + 1);
                } else {
                    return (Player)(playerIndex + 1);
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
