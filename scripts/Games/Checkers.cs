using System;
using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;

namespace CaretakerNET.Games
{
    /*
    {0, 0, 0, 0, 0, 0, 0}
    {0, 0, 0, 0, 0, 0, 0}
    {0, 0, 0, 0, 0, 0, 0}
    {0, 0, 0, 2, 0, 0, 0}
    {0, 0, 0, 1, 1, 0, 0}
    {2, 2, 0, 1, 2, 1, 0}
    */

    [JsonDerivedType(typeof(Checkers), typeDiscriminator: "Checkers")]
    public class Checkers : BoardGame
    {
        public int this[int x, int y] { get => board[x, y]; set => board[x, y] = value; }
        public struct Win(Player winningPlayer = Player.None, bool tie = false) {
            public Player WinningPlayer = winningPlayer;
            // public List<Vector2> winPoints = winPoints ?? [];
            public bool Tie = tie;
        }

        private readonly int[,] board; // list of a list of ints
        public const int W = 8; // width of the board
        public const int H = 8; // height of the board
        public const int MAX_PIECES = 12; // max amount of pieces a user can have

        // public static string GetEmoji(Player player)
        // {
        //     return player switch {
        //         Player.One => "🔴", // red circle (p1)
        //         Player.Two => "🟡", // yellow circle (p2)
        //         _ => "⬛" // black square (empty)
        //     };
        // }

        public Win WinCheck()
        {
            return new Win();
        }

        public string DisplayBoard() => DisplayBoard(out _);

        public string DisplayBoard(out Win win)
        {   // useful if you want win data outside of the method call, like if you want to reset after a winning move
            win = WinCheck();
            return DisplayBoard(win);
        }
        public string DisplayBoard(Win win)
        {
            // bool anyWin = win.WinningPlayer != Player.None;
            StringBuilder joinedRows = new();
            for (int i = 0; i < H; i++) {
                for (int j = 0; j < W; j++) {
                    joinedRows.Append((Player)this[j, i] switch {
                        Player.One => "🔴", // red circle (p1)
                        Player.Two => "🟡", // yellow circle (p2)
                        _ => (j + i) % 2 == 1 ? "⬜" : "⬛" // checkerboard base (empty)
                    });
                }
                joinedRows.Append('\n');
            }
            // joinedRows.AppendLine("1️⃣2️⃣3️⃣4️⃣5️⃣6️⃣7️⃣");
            return joinedRows.ToString();
        }
        public Checkers(ulong playingChannelId, params ulong[] players)
        {
            board = new int[W, H];

            PlayingChannelId = playingChannelId;
            int j = 0;
            Players = players.ToDictionary(_ => (Player)(++j));
        }
    }
}
