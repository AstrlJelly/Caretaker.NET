using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Caretaker.Helper;
using Discord.WebSocket;

namespace Caretaker.Games
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
        public List<int>[] board; // array of a list of ints
        public const int MAXWIDTH = 7; // width of the board
        public const int MAXHEIGHT = 6; // height of the board
        public void SetColumn(int column, Player character)
        {
            column = Math.Clamp(column, 0, MAXWIDTH);
            board[column].Add((int)character);
            Console.WriteLine(board.IsIndexValid(column));
        }
        public string DisplayBoard()
        {
            Console.WriteLine(board);
            List<string> joinedRows = [];
            for (int i = 0; i < board.Length; i++) {
                for (int j = MAXHEIGHT - 1; j >= 0; j--) {
                    bool valid = board[i].IsIndexValid(j);
                    joinedRows.Add(valid ? board[i][j].ToString() : "0");
                }
                joinedRows.Add("\n");
            }
            return string.Join("", joinedRows);
        }
        public ConnectFour()
        {
            board = new List<int>[MAXWIDTH];
            Array.Fill(board, new List<int>(MAXHEIGHT)); // make sure all the lists are initialized
        }
    }
}
