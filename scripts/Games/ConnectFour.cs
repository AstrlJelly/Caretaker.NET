using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CaretakerNET.Helper;
using Discord.WebSocket;

namespace CaretakerNET.Games
{
    /*// VISUAL GUIDE
    {0, 0, 0, 0, 0, 0}
    {1, 0, 0, 0, 0, 0}
    {2, 1, 2, 0, 0, 0}
    {1, 2, 0, 0, 0, 0}
    {1, 1, 0, 0, 0, 0}
    {2, 0, 0, 0, 0, 0}
    {0, 0, 0, 0, 0, 0}
    */// END VISUAL GUIDE

    public class ConnectFour
    {
        public enum Player
        {
            One = 1,
            Two,
        }
        private readonly List<List<int>> board; // array of a list of ints
        public const int MAXWIDTH = 7; // width of the board
        public const int MAXHEIGHT = 6; // height of the board
        public void SetColumn(int column, int player) => SetColumn(column, (Player)player);
        public void SetColumn(int column, Player player)
        {
            column = Math.Clamp(column, 0, MAXWIDTH - 1);
            board[column].Add((int)player);
        }
        public int GetElement(int x, int y)
        {
            return board[y].IsIndexValid(x) ? board[y][x] : 0;
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
