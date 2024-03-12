using System;
using System.Diagnostics;
using System.Numerics;
using System.Text;

using Discord.WebSocket;

using CaretakerNET.Core;

namespace CaretakerNET.Games
{
    public abstract class Game
    {
        public enum Player : int
        {
            None,
            One,
            Two,
            // Three,
            // Four,
        }

        public ulong player1;
        public ulong player2;
    }
}
