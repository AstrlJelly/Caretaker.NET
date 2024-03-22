using System;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;



namespace CaretakerNET.Games
{
    // weird. but it works... maybe?
    // [JsonDerivedType(typeof(BoardGame), typeDiscriminator: "BoardGame")]
    [JsonDerivedType(typeof(ConnectFour), typeDiscriminator: "ConnectFour")]
    [JsonDerivedType(typeof(Checkers), typeDiscriminator: "Checkers")]
    public abstract class BoardGame
    {
        public enum Player : int
        {
            None,
            One,
            Two,
        }

        public ulong PlayingChannelId;
        // internal ulong[]? allPlayers;
        public List<ulong> Players { get; internal set; } = [];
        
        // public int PlayerTurns { get; internal set; } = 0;
        public int Turns { get; internal set; } = 0;
        public ulong ForfeitPlayer { get; internal set; }
        public int EndAt { get; internal set; } = int.MaxValue;

        public void SwitchPlayers()
        {
            Turns++;
        }

        public Player GetWhichPlayer(ulong playerId)
        {
            if (/* allPlayers != null &&  */Players != null) {
                if (playerId == Players[0]) {
                    return Player.One;
                } else if (playerId == Players[1]) {
                    if (Turns == 0) {
                        Players.Reverse();
                        return Player.One;
                    }
                    return Player.Two;
                }
            }

            return Player.None;
        }

        public ulong OtherPlayerId()
        {
            return Players[Turns % 2];
        }

        public (ulong, ulong) GetPlayerIds(ulong sentId)
        {
            if (Turns == 0) {
                int which = sentId == Players[1] ? 0 : 1;
                return (sentId, Players[which]);
            } else {
                return (Players[(Turns) % 2], Players[(Turns + 1) % 2]);
            }
        }

        public bool IsAnyPlayer(ulong id)
        {
            return Players.Contains(id);
        }

        public bool IsCurrentPlayer(Player player)
        {
            var whichPlayer = (Turns % 2) + 1;
            return Turns == 0 || (int)player == whichPlayer;
        }

        public bool StartForfeit(ulong playerId)
        {
            if (EndAt < int.MaxValue) return false;

            EndAt = Turns + 3;
            ForfeitPlayer = playerId;
            return true;
        }
    }
}
