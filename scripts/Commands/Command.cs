using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Caretaker.Commands
{
    public class Command
    {
        public string name;
        public string desc;
        public string genre;
        public delegate void Run(SocketUserMessage msg, Dictionary<string, dynamic> p);
        public Run func;
        public Param[]? parameters;
        public Param? inf;
        public string[][]? limitedTo;
        public int timeout;
        public int currentTimeout;
        public Command(string name, string desc, string genre, Run func, Param[]? parameters = null, string[][]? limitedTo = null, int timeout = 0)
        {
            this.name = name;
            this.desc = desc;
            this.genre = genre;
            this.func = func;
            this.limitedTo = limitedTo;
            this.timeout = timeout;
            currentTimeout = 0;

            if (parameters != null) {
                List<Param> tempParameters = parameters.ToList();
                this.inf = tempParameters.Find(x => x.name == "inf");
                if (this.inf != null) {
                    tempParameters.Remove(this.inf);
                }
                this.parameters = parameters.ToArray();
            }
        }
    }

    public class Param
    {
        public string name;
        public string desc;
        public dynamic preset;
        public dynamic type;
        public Param(string name, string desc, dynamic preset, dynamic type)
        {
            this.name = name;
            this.desc = desc;
            this.preset = preset;
            this.type = type;
        }
    }

    // public class ParsedCommand
    // {
    //     public string name;
    //     public ParsedCommand(string name)
    //     {
    //         this.name = name;
    //     }
    // }
}
