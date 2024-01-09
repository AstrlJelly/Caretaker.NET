using System;
using CaretakerNET.Core;
using Discord;
using Discord.WebSocket;

namespace CaretakerNET.Commands
{
    public class Command
    {
        public readonly string name;
        public readonly string desc;
        public readonly string genre;
        public delegate Task RunAsync(IUserMessage msg, Dictionary<string, dynamic> p);
        // public delegate void Run(SocketUserMessage msg, Dictionary<string, dynamic> p);
        public readonly RunAsync func;
        public readonly Param[] parameters;
        public readonly Param? inf;
        public string[][] limitedTo;
        public int timeout;
        public int currentTimeout;
        public Command(string name, string desc, string genre, RunAsync func, List<Param>? parameters = null, string[][]? limitedTo = null, int timeout = 0)
        {
            this.name = name;
            this.desc = desc;
            this.genre = genre;
            this.func = func;
            this.limitedTo = limitedTo ?? [];
            this.timeout = timeout;
            currentTimeout = 0;

            if (parameters != null) {
                this.inf = parameters.Find(x => x.name == "params");
                if (this.inf != null) {
                    parameters.Remove(this.inf);
                    Caretaker.Log(this.inf.name);
                }
                this.parameters = [.. parameters];
            } else {
                this.parameters = [];
            }
        }
    }

    public class Param
    {
        // public static readonly string[] customTypes = [
        //     "user", "channel"
        // ];

        public string name;
        public string desc;
        public dynamic preset;
        public string type;
        public Param(string name, string desc, dynamic preset, string? type = null)
        {
            this.name = name;
            this.desc = desc;
            this.preset = preset;
            
            type ??= preset.GetType().Name;
            this.type = type.ToLower();
        }
    }
}
