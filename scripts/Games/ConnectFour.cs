using System;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CaretakerNET.Core;
using Discord.WebSocket;

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

    public class ConnectFour
    {
        public enum Player
        {
            None,
            One,
            Two,
        }

        public struct Win(Player winningPlayer, Vector2[] winPoints) {
            public Player winningPlayer = winningPlayer;
            public Vector2[] winPoints = winPoints;
        }

        private readonly List<List<int>> board; // array of a list of ints
        public const int MAXWIDTH = 7; // width of the board
        public const int MAXHEIGHT = 6; // height of the board

        public ulong player1;
        public ulong player2;

        public bool AddToColumn(int column, int player) => AddToColumn(column, (Player)player);
        public bool AddToColumn(int column, Player player)
        {
            // technically don't need the else but i think it makes it look nicer
            if (column > MAXWIDTH -1 || board[column].Count > MAXWIDTH - 1) {
                return false;
            } else {
                board[column].Add((int)player);
                return true;
            }
        }
        
        public int GetElement(int x, int y)
        {
            return board[y].IsIndexValid(x) ? board[y][x] : 0;
        }

        // this is one of the most complicated things ive done in code so far so
        // if you're some random person looking at this, sorry if it's atrocious
        // CURRENTLY THIS IS WRITTEN BY BING AI, LOL (it does suck rn though, i wanna see how optimized i can get it)
        public Win WinCheck()
        {
            // // Check horizontal lines
            // for (int i = 0; i < 6; i++)
            // {
            //     for (int j = 0; j < 4; j++)
            //     {
            //         if (GetElement(i, j) != 0 && GetElement(i, j) == GetElement(i, j + 1) && GetElement(i, j) == GetElement(i, j + 2) && GetElement(i, j) == GetElement(i, j + 3))
            //         {
            //             return new Win((Player)GetElement(i, j), []);
            //         }
            //     }
            // }

            // // Check vertical lines
            // for (int j = 0; j < 7; j++)
            // {
            //     for (int i = 0; i < 3; i++)
            //     {
            //         if (GetElement(i, j) != 0 && GetElement(i, j) == GetElement(i + 1, j) && GetElement(i, j) == GetElement(i + 2, j) && GetElement(i, j) == GetElement(i + 3, j))
            //         {
            //             return new Win((Player)GetElement(i, j), []);
            //         }
            //     }
            // }

            // // Check diagonal lines (top-left to bottom-right)
            // for (int i = 0; i < 3; i++)
            // {
            //     for (int j = 0; j < 4; j++)
            //     {
            //         if (GetElement(i, j) != 0 && GetElement(i, j) == GetElement(i + 1, j + 1) && GetElement(i, j) == GetElement(i + 2, j + 2) && GetElement(i, j) == GetElement(i + 3, j + 3))
            //         {
            //             return new Win((Player)GetElement(i, j), []);
            //         }
            //     }
            // }

            // // Check diagonal lines (bottom-left to top-right)
            // for (int i = 3; i < 6; i++)
            // {
            //     for (int j = 0; j < 4; j++)
            //     {
            //         if (GetElement(i, j) != 0 && GetElement(i, j) == GetElement(i - 1, j + 1) && GetElement(i, j) == GetElement(i - 2, j + 2) && GetElement(i, j) == GetElement(i - 3, j + 3))
            //         {
            //             return new Win((Player)GetElement(i, j), []);
            //         }
            //     }
            // }



            return new Win(Player.None, []);
        }

        public string DisplayBoard()
        {
            List<string> joinedRows = [];
            for (int i = MAXHEIGHT - 1; i >= 0; i--) {
                List<string> joinedChars = [];
                for (int j = 0; j < MAXWIDTH; j++) {
                    int player = board[j].IsIndexValid(i) ? board[j][i] : 0;
                    joinedChars.Add((Player)player switch {
                        Player.One => "🔴", // red circle (p1)
                        Player.Two => "🟡", // yellow circle (p2)
                        _ => "⬛" // black square (empty)
                    });
                }
                joinedRows.Add(string.Join("", joinedChars) + "\n");
            }
            return string.Join("", joinedRows);
        }
        public ConnectFour()
        {
            board = new List<List<int>>(MAXWIDTH);
            for (int i = 0; i < MAXWIDTH; i++) { // make sure all the lists are initialized
                board.Add([]);
            }
            // Array.Fill(board, new List<int>(MAXHEIGHT)); // make sure all the lists are initialized
        }
    }
}
