using System;
using System.Numerics;
using System.Text;

namespace CaretakerNET.Games
{
    /*
    {0, 0, 0, 0, 0, 0}
    {1, 0, 0, 0, 0, 0}
    {2, 1, 2, 0, 0, 0}
    {1, 2, 0, 0, 0, 0}
    {1, 1, 0, 0, 0, 0}
    {2, 0, 0, 0, 0, 0}
    {0, 0, 0, 0, 0, 0}
    */

    public class ConnectFour : Game
    {
        // this[MAXWIDTH, MAXHEIGHT]
        public int this[int index1, int index2] => board[index1].IsIndexValid(index2) ? board[index1][index2] : 0;
        public struct Win(Player winningPlayer, Vector2[] winPoints) {
            public Player winningPlayer = winningPlayer;
            public Vector2[] winPoints = winPoints;
        }

        private readonly List<List<int>> board; // list of a list of ints
        public const int MAXWIDTH = 7; // width of the board
        public const int MAXHEIGHT = 6; // height of the board

        public bool AddToColumn(int column, Player player) => AddToColumn(column, (int)player);
        public bool AddToColumn(int column, int player)
        {   // technically don't need the else but i think it makes it look nicer
            if (column > MAXWIDTH -1 || board[column].Count > MAXHEIGHT - 1) {
                return false;
            } else {
                board[column].Add(player);
                return true;
            }
        }
        
        public int ElementAt(int x, int y)
        {
            return board[y].IsIndexValid(x) ? board[y][x] : 0;
        }

        // private void IterateBoard(Action<int, int> action)
        // {
        //     for (int i = MAXHEIGHT - 1; i >= 0; i--) {
        //         for (int j = 0; j < MAXWIDTH; j++) {
        //             action.Invoke(j, i);
        //         }
        //     }
        // }

        // CURRENTLY THIS IS WRITTEN BY BING AI, REPLACE SOON :3 
        public Win WinCheck()
        {
            // // Check horizontal lines
            // for (int i = 0; i < MAXHEIGHT; i++) {
            //     for (int j = 0; j < MAXWIDTH; j++) {
            //         int p = ElementAt(i, j);
            //         if (p != 0 && p == ElementAt(i, j + 1) && p == ElementAt(i, j + 2) && p == ElementAt(i, j + 3))
            //         {
            //             return new Win((Player)p, [ new(i, j), new(i, j + 1), new(i, j + 2), new(i, j + 3) ]);
            //         }
            //     }
            // }

            // // Check vertical lines
            // for (int j = 0; j < 7; j++) {
            //     for (int i = 0; i < 3; i++) {
            //         int p = ElementAt(i, j);
            //         if (p != 0 && p == ElementAt(i + 1, j) && p == ElementAt(i + 2, j) && p == ElementAt(i + 3, j))
            //         {
            //             return new Win((Player)p, [ new(i, j), new(i + 1, j), new(i + 2, j), new(i + 3, j) ]);
            //         }
            //     }
            // }

            // // Check diagonal lines (top-left to bottom-right)
            // for (int i = 0; i < 3; i++) {
            //     for (int j = 0; j < 4; j++) {
            //         int p = ElementAt(i, j);
            //         if (p != 0 && p == ElementAt(i + 1, j + 1) && p == ElementAt(i + 2, j + 2) && p == ElementAt(i + 3, j + 3))
            //         {
            //             return new Win((Player)p, [ new(i, j), new(i + 1, j + 1), new(i + 2, j + 2), new(i + 3, j + 3) ]);
            //         }
            //     }
            // }

            // // Check diagonal lines (bottom-left to top-right)
            // for (int i = 3; i < 6; i++) {
            //     for (int j = 0; j < 4; j++) {
            //         int p = ElementAt(i, j);
            //         if (p != 0 && p == ElementAt(i - 1, j + 1) && p == ElementAt(i - 2, j + 2) && p == ElementAt(i - 3, j + 3))
            //         {
            //             return new Win((Player)p, [ new(i, j), new(i - 1, j + 1), new(i - 2, j + 2), new(i - 3, j + 3) ]);
            //         }
            //     }
            // }

            bool isP1 = false;
            int i = 0;
            for (int x = 0; x < MAXWIDTH; x++)
            {
                i = 0;
                for (int y = 0; y < MAXHEIGHT; y++)
                {
                    i++;
                    Log(this[x, y]);
                    // if (i == 4) return new Win(isP1 ? Player.One : Player.Two, []);
                }
            }

            return new Win(Player.None, []);
        }

        public string DisplayBoard() => DisplayBoard(out _);

        public string DisplayBoard(out Win win)
        {
            // useful if you want win data outside of the method call, like if you want to reset after a winning move
            win = WinCheck();
            bool anyWin = win.winningPlayer != Player.None;
            // List<string> joinedRows = [];
            StringBuilder joinedRows = new();
            for (int i = MAXHEIGHT - 1; i >= 0; i--) {
                // List<string> joinedChars = [];
                StringBuilder joinedChars = new();
                for (int j = 0; j < MAXWIDTH; j++) {
                    bool isWin = anyWin && win.winPoints.Contains(new(i, j)); // the vector2 in this uses x, y, while this displays using y, x
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
        public ConnectFour(ulong player1 = 0, ulong player2 = 0)
        {
            board = new List<List<int>>(MAXWIDTH);
            for (int i = 0; i < MAXWIDTH; i++) { // make sure all the lists are initialized
                board.Add([]);
            }
            this.player1 = player1;
            this.player2 = player2;
        }
    }
}
