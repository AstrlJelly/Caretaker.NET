﻿using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
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
        public readonly Param[] parameters;
        public readonly Param? inf;
        public string[][]? limitedTo;
        public int timeout;
        public int currentTimeout;
        public Command(string name, string desc, string genre, Run func, List<Param>? parameters = null, string[][]? limitedTo = null, int timeout = 0)
        {
            this.name = name;
            this.desc = desc;
            this.genre = genre;
            this.func = func;
            this.limitedTo = limitedTo;
            this.timeout = timeout;
            currentTimeout = 0;

            if (parameters != null) {
                this.inf = parameters.Find(x => x.name == "inf");
                if (this.inf != null) {
                    parameters.Remove(this.inf);
                }
                this.parameters = [.. parameters];
            } else {
                this.parameters = [];
            }
        }
    }

    public class Param
    {
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
            
            Console.WriteLine(this.type);
        }
    }
}
