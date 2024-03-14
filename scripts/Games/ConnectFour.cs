using System;
using System.Numerics;
using System.Text;

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

    public class ConnectFour : BoardGame
    {
        public int this[int x, int y] { get => board[x, y]; set => board[x, y] = value; }
        public struct Win(Player winningPlayer = Player.None, List<Vector2>? winPoints = null) {
            public Player winningPlayer = winningPlayer;
            public List<Vector2> winPoints = winPoints ?? [];
        }

        private readonly int[,] board; // list of a list of ints
        public const int W = 7; // width of the board
        public const int H = 6; // height of the board

        public bool AddToColumn(int column, Player player) => AddToColumn(column, (int)player);
        public bool AddToColumn(int column, int player)
        {
            if (column < W) {
                for (int i = 0; i < H; i++) {
                    if (this[column, i] == 0) {
                        this[column, i] = player;
                        return true;
                    }
                }
            }

            return false;
        }

        public Win WinCheck()
        {
            foreach (var player in new Player[] { Player.One, Player.Two }) {
                var win = WinCheck(player);
                if (win.winningPlayer != Player.None) return win;
            }
            return new Win();
        }
        public Win WinCheck(Player player) => WinCheck((int)player);
        public Win WinCheck(int pl)
        {
            Player player = (Player)pl;
            Win? win = null;

            Log(1);
            // horizontal check
            Parallel.For(0, W, (x, state) => {
                Parallel.For(0, H - 4, y => {
                    List<Vector2> checks = [ new(x, y), new(x, y + 1), new(x, y + 2), new(x, y + 3) ];
                    if (checks.All(vec => board[(int)vec.X, (int)vec.Y] == pl)) {
                        win = new Win(player, checks);
                        state.Stop();
                    }
                });
            });
            if (win != null) return (Win)win;

            Log(2);
            // vertical check
            Parallel.For(0, W - 4, (x, state) => {
                Parallel.For(0, H, y => {
                    List<Vector2> checks = [ new(x, y), new(x + 1, y), new(x + 2, y), new(x + 3, y) ];
                    if (checks.All(vec => board[(int)vec.X, (int)vec.Y] == pl)) {
                        win = new Win(player, checks);
                        state.Stop();
                        return;
                    }
                });
            });
            if (win != null) return (Win)win;

            Log(3);
            // horizontal check (top left to bottom right, i.e bottom left to top right)
            Parallel.For(0, W - 4, (x, state) => {
                Parallel.For(0, H - 4, y => {
                    List<Vector2> checks = [ new(x, y), new(x + 1, y + 1), new(x + 2, y + 2), new(x + 3, y + 3) ];
                    if (checks.All(vec => board[(int)vec.X, (int)vec.Y] == pl)) {
                        win = new Win(player, checks);
                        state.Stop();
                        return;
                    }
                });
            });
            if (win != null) return (Win)win;

            Log(4);
            // horizontal check (top left to bottom right, i.e bottom left to top right)
            Parallel.For(3, W, (x, state) => {
                Parallel.For(0, H - 4, y => {
                    List<Vector2> checks = [ new(x, y), new(x - 1, y + 1), new(x - 2, y + 2), new(x - 3, y + 3) ];
                    if (checks.All(vec => board[(int)vec.X, (int)vec.Y] == pl)) {
                        win = new Win(player, checks);
                        state.Stop();
                        return;
                    }
                });
            });
            if (win != null) return (Win)win;

            Log(5);
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
            bool anyWin = win.winningPlayer != Player.None;
            // List<string> joinedRows = [];
            StringBuilder joinedRows = new();
            for (int i = H; i >= 0; i--) {
                // Log(i);
                // List<string> joinedChars = [];
                StringBuilder joinedChars = new();
                for (int j = 0; j < W; j++) {
                    bool isWin = anyWin && win.winPoints.Contains(new(j, i)); // the vector2 in this uses x, y, while this displays using y, x
                    joinedChars.Append((Player)this[j, i] switch {
                        Player.One => isWin ? "❤" : "🔴",  // red circle/heart (p1)
                        Player.Two => isWin ? "💛" : "🟡", // yellow circle/heart (p2)
                        _ => "⬛" // black square (empty)
                    });
                }
                joinedRows.AppendLine(joinedChars.ToString());
            }
            return joinedRows.ToString();
        }
        public ConnectFour(ulong channelId, ulong player1 = 0, ulong player2 = 0)
        {
            board = new int[W, H];
            // for (int i = 0; i < W; i++) { // make sure all the lists are initialized
            //     board.Add([]);
            // }
            // Log(board[0, 0]);
            // Log(board[W - 1, H - 1]);
            this.playingChannelId = channelId;
            this.player1 = player1;
            this.player2 = player2;
        }
    }
}
