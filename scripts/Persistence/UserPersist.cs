using System;
using System.Text.Json.Serialization;

using Discord;
using Discord.WebSocket;

using CaretakerNET.Economy;

namespace CaretakerNET.Persistence
{
    public class UserPersist
    {
        [JsonInclude] bool IsInServer = true;
        public ulong UserId;
        public string Username = "";
        // economy
        public const decimal START_BAL = 50.00m;
        public decimal Balance = 0.00m;
        public bool HasStartedEconomy = false;
        public List<EconomyHandler.Item> Inventory = [];

        public bool StartEconomy(IUserMessage msg, bool fromCom = true)
        {
            if (HasStartedEconomy) return false;
            string[] startReplies = [
                "ohhhh you haven't used the economy before, have you?",
                "it's time for you to start gambling! :D",
                "ur CRAZY poor right now",
                "wow you somehow have no money. that's crazy.",
            ];
            string reply = startReplies.GetRandom()! + $"\nhere's {START_BAL} jells, you need it.";
            if (fromCom) reply += " (also, try that command again.)";

            _ = msg.Reply(reply);
            Balance = START_BAL;
            return true;
        }


        // gaming
        public List<string> Wins = []; // the name of the class of the game won or lost
        public List<string> Losses = [];

        public void AddWin(Type whichGame) => Wins.Add(whichGame.Name);
        public void AddLoss(Type whichGame) => Losses.Add(whichGame.Name);
        public float WinLossRatio()
        {
            return Wins.Count / Losses.Count;
        }

        // misc
        public long Timeout = 0;

        public enum Features
        {
            ItGo = 1,
        }
        public HashSet<Features> OptedOutFeatures = [];

        public void Init(DiscordSocketClient client, ulong userId)
        {
            UserId = userId;
            var user = client.GetUser(userId);
            if (user != null) {
                Username = user.Username;
                IsInServer = true;
            } else {
                IsInServer = false;
            }
            Update();
        }

        [JsonInclude] bool NeedsUpdating;
        private void Update()
        {
            if (!NeedsUpdating) return;
            NeedsUpdating = true;

            HasStartedEconomy = false;
        }
    }
}