using System;
using System.Diagnostics;
using System.Numerics;
using System.Text;

using Discord.WebSocket;

using CaretakerNET.Core;

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
        public ulong[] allPlayers;
        public List<ulong>? Players;
        public int turns = 0;

        public void SwitchPlayers()
        {
            turns++;
        }

        public Player GetWhichPlayer(ulong playerId)
        {
            // not working rn
            if (allPlayers != null && Players != null) {
                int max = Math.Min(allPlayers.Length, Players.Count + 1);
                Log("max : " + max);
                for (int i = 0; i < max; i++) {
                    Log("i : " + i);
                    if (i == Players.Count + 1) {
                        Log("ADD");
                        Players.Add(playerId);
                    }
                    if (i == max) {
                        Log("RETURN");
                        return (Player)(i + 1);
                    }
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
