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

    [JsonDerivedType(typeof(ConnectFour), typeDiscriminator: "ConnectFour")]
    public class ConnectFour : BoardGame
    {
        public int this[int x, int y] { get => board[x][y]; set => board[x][y] = value; }
        public struct Win(Player winningPlayer = Player.None, List<Vector2>? winPoints = null, bool tie = false) {
            public Player WinningPlayer = winningPlayer;
            public List<Vector2> winPoints = winPoints ?? [];
            public bool Tie = tie;
        }

        [JsonInclude] private readonly int[][] board; // list of a list of ints
        public const int W = 7; // width of the board
        public const int H = 6; // height of the board
        // public const int W = 6; // width of the board
        // public const int H = 8; // height of the board

        // public static bool IsValidMove(int column) => column is < W and >= 0;
        public bool AddToColumn(int column, Player player) => AddToColumn(column, (int)player);
        public bool AddToColumn(int column, int player)
        {
            if (column is < W and >= 0 && this[column, H - 1] == 0) {
                for (int i = 0; i < H; i++) {
                    if (this[column, i] == 0) {
                        this[column, i] = player;
                        return true;
                    }
                }
            }
            return false;
        }

        // public Win WinCheckFromPoint(Vector2 point, Player player)
        // {
            
        // }

        public Win WinCheck()
        {
            foreach (var player in new Player[] { Player.One, Player.Two }) {
                var win = WinCheck(player);
                if (win.WinningPlayer != Player.None) return win;
            }
            return new Win();
        }
        public Win WinCheck(Player player) => WinCheck((int)player);
        public Win WinCheck(int pl)
        {
            Player player = (Player)pl;

            // tie check
            if (!board[H - 1].Any(x => x == 0)) {
                return new Win(tie: true);
            }

            // horizontal check
            for (int x = 0; x < W; x++) {
                for (int y = 0; y < H - 3; y++) {
                    if (this[x, y] == 0) continue;
                    List<Vector2> checks = [ new(x, y), new(x, y + 1), new(x, y + 2), new(x, y + 3) ];
                    if (checks.All(vec => this[(int)vec.X, (int)vec.Y] == pl)) {
                        return new Win(player, checks);
                    }
                }
            }

            // vertical check
            for (int x = 0; x < W - 3; x++) {
                for (int y = 0; y < H; y++) {
                    if (this[x, y] == 0) continue;
                    List<Vector2> checks = [ new(x, y), new(x + 1, y), new(x + 2, y), new(x + 3, y) ];
                    if (checks.All(vec => this[(int)vec.X, (int)vec.Y] == pl)) {
                        return new Win(player, checks);
                    }
                }
            }

            // horizontal check (top left to bottom right, i.e bottom left to top right)
            for (int x = 0; x < W - 3; x++) {
                for (int y = 0; y < H - 3; y++) {
                    if (this[x, y] == 0) continue;
                    List<Vector2> checks = [ new(x, y), new(x + 1, y + 1), new(x + 2, y + 2), new(x + 3, y + 3) ];
                    if (checks.All(vec => this[(int)vec.X, (int)vec.Y] == pl)) {
                        return new Win(player, checks);
                    }
                }
            }

            // horizontal check (top left to bottom right, i.e bottom left to top right)
            for (int x = 3; x < W; x++) {
                for (int y = 0; y < H - 3; y++) {
                    if (this[x, y] == 0) continue;
                    List<Vector2> checks = [ new(x, y), new(x - 1, y + 1), new(x - 2, y + 2), new(x - 3, y + 3) ];
                    if (checks.All(vec => this[(int)vec.X, (int)vec.Y] == pl)) {
                        return new Win(player, checks);
                    }
                }
            }

            return new Win();
        }

        public string GetEmoji(ulong playerId) => GetEmoji(GetWhichPlayer(playerId));
        public string GetEmoji(Player player)
        {
            return player switch {
                Player.One => "🔴", // red circle (p1)
                Player.Two => "🟡", // yellow circle (p2)
                _ => "⬛" // black square (empty)
            };
        }

        public string DisplayBoard() => DisplayBoard(out _);

        public string DisplayBoard(out Win win)
        {   // useful if you want win data outside of the method call, like if you want to reset after a winning move
            win = WinCheck();
            return DisplayBoard(win);
        }
        public string DisplayBoard(Win win)
        {
            bool anyWin = win.WinningPlayer != Player.None;
            StringBuilder joinedRows = new();
            for (int i = H - 1; i >= 0; i--) { // display upside down i guess
                for (int j = 0; j < W; j++) {
                    bool isWin = anyWin && win.winPoints.Contains(new(j, i));
                    joinedRows.Append((Player)this[j, i] switch {
                        Player.One => isWin ? "❤" : "🔴",  // red circle/heart (p1)
                        Player.Two => isWin ? "💛" : "🟡", // yellow circle/heart (p2)
                        _ => "⬛" // black square (empty)
                    });
                }
                joinedRows.Append('\n');
            }
            joinedRows.AppendLine("1️⃣2️⃣3️⃣4️⃣5️⃣6️⃣7️⃣");
            return joinedRows.ToString();
        }

        public ConnectFour(ulong playingChannelId, params ulong[] players)
        {
            board = new int[W][];
            for (int i = 0; i < W; i++) {
                board[i] = new int[H];
            }

            PlayingChannelId = playingChannelId;
            Players = [ ..players ];
        }

        [JsonConstructor]
        public ConnectFour(List<ulong> Players, int[][] board)
        {
            this.Players = Players;
            this.board = board;
        }
    }
}
