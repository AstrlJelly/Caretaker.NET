using System.Diagnostics;
using CaretakerNET.Audio;
using Discord;
using Discord.WebSocket;

namespace CaretakerNET
{
    public class CaretakerConsole
    {
        public static CaretakerConsole Instance {get; private set;} = new();

        public static void Init(List<ITextChannel?> TalkingChannels)
        {
            Instance.TalkingChannels = [ ..TalkingChannels ];
            Instance.CurrentTalkingChannel = Instance.TalkingChannels[0];
            Instance.CurrentTitleState.UpdateTitle(); 
            Instance.StartReadingKeys();
        }

        public enum States
        {
            Typing,
            SettingChannel,
        }
        
        public class TitleState
        {
            private string status = "Starting...";
            public string Status { get => status; set {
                status = value;
                UpdateTitle();
            }}

            // public void UpdateTitle(string status)
            // {
            //     Status = status;
            //     // UpdateTitle();
            // }

            public void UpdateTitle()
            {
                var ch = Instance.CurrentTalkingChannel;
                Console.Title = $"CaretakerNET : {Status} | {ch?.Guild.Name}, {ch?.Name}";
            }
        }
        public readonly TitleState CurrentTitleState = new();
        public States CurrentState = States.Typing;
        public IDisposable? TypingState = null;
        public Stopwatch CancelTypingStopwatch = new();
        private ITextChannel?[] TalkingChannels = [];
        public ITextChannel? CurrentTalkingChannel = null;
        public Stopwatch PlayKeyPressStopwatch = new();
        public int CursorPos = 0;
        public int HistoryIndex = 0;
        public readonly List<char[]> ConsoleLineHistory = [];
        public readonly List<char> ConsoleLine = [];

        public void ClearLine(bool history = false)
        {
            CursorPos = 0;
            ClearConsoleLine();
            if (history) {
                ConsoleLineHistory.Add(ConsoleLine.ToArray());
            }
            ConsoleLine.Clear();
        }

        public void TypeKey(ConsoleKeyInfo key)
        {
            if (CurrentState == States.Typing) {
                TypingState ??= CurrentTalkingChannel?.EnterTypingState();
                CancelTypingStopwatch.Restart();
            }
            Console.Write(key.KeyChar);
            ConsoleLine.Add(key.KeyChar);
            CursorPos = ConsoleLine.Count - 1;
        }

        public void DisposeTyping()
        {
            TypingState?.Dispose();
            TypingState = null;
        }

        private void AutoCancelTyping()
        {
            Task.Run(delegate {
                while (this != null) {
                    while (CancelTypingStopwatch.Elapsed.TotalSeconds > 8 && TypingState != null) {
                        DisposeTyping();
                    }
                }
            });
        }

        private void HistoryBack()
        {
            if (ConsoleLineHistory.Count > 0 && HistoryIndex > 0) {
                HistoryIndex--;
                ClearLine(false);
                ConsoleLine.AddRange(ConsoleLineHistory[HistoryIndex]);
                Console.Write(string.Join("", ConsoleLine));
            }
        }

        private void HistoryForward()
        {
            ClearLine(false);
            if (HistoryIndex < ConsoleLineHistory.Count) {
                HistoryIndex--;
                ConsoleLine.AddRange(ConsoleLineHistory[HistoryIndex]);
            }
            Console.Write(string.Join("", ConsoleLine));
        }

        public bool TrySetTalkingChannelAtIndex(int index, ITextChannel talkingChannel)
        {
            if (index < 0 && index > TalkingChannels.Length - 1) {
                return false;
            }
            TalkingChannels[index] = talkingChannel;
            return true;
        }

        public bool IsChannelTalkingChannel(ITextChannel channel)
        {
            return IsChannelTalkingChannel(channel.Id);
        }

        public bool IsChannelTalkingChannel(ulong channelId)
        {
            return TalkingChannels.Any(c => c?.Id == channelId);
        }

#region Keybinds
        private void OnFunctionKey(ConsoleKey key)
        {
            switch (key)
            {
                case ConsoleKey.F6: {
                    if (CurrentState != States.SettingChannel) {
                        ClearLine(true);
                        Console.Write("\"channel|guild\", please : ");
                        CurrentState = States.SettingChannel;
                    } else {
                        ClearLine(false);
                        CurrentState = States.Typing;
                    }
                } break;
                case ConsoleKey.F12: {
                    // gwahahaha! this is where i run any kind of code i want
                    ClearLine(false);
                } break;
                default: {
                    int whichChannel = key - ConsoleKey.F1;
                    ITextChannel? ch = null;
                    if (whichChannel >= 0 && whichChannel < TalkingChannels.Length) {
                        ch = TalkingChannels[whichChannel];
                    }
                    
                    if (ch != null) {
                        CurrentTalkingChannel = ch;
                        LogInfo($"switched to channel \"{ch.Name}\" in guild \"{ch.Guild.Name}\"");
                    } else {
                        string[] logs = [
                            $"ahh sorry the channel at {whichChannel} is null",
                            $"talkingChannel at {whichChannel} was null!",
                            $"LOOOOOSERR... {whichChannel} doesn't exist.",
                            $"you know the drill. {whichChannel}",
                        ];
                        LogWarning(logs.GetRandom());
                    }
                    CurrentTitleState.UpdateTitle();
                } break;
            }
        }

        private void OnArrowKey(ConsoleKey key)
        {
            // x from LEFT, y from TOP
            (int left, int top) = Console.GetCursorPosition();
            Action? action = key switch {
                ConsoleKey.LeftArrow => delegate { // 37
                    if (CursorPos >= 0) {
                        CursorPos--;
                        Console.SetCursorPosition(left - 1, top);
                    }
                },
                ConsoleKey.RightArrow => delegate { // 39
                    if (CursorPos < ConsoleLine.Count - 1) {
                        CursorPos++;
                        Console.SetCursorPosition(left + 1, top);
                    }
                },
                ConsoleKey.UpArrow => HistoryBack,
                ConsoleKey.DownArrow => HistoryForward,
                _ => null,
            };
            
            action?.Invoke();
        }

        private void OnBackspace()
        {
            List<char> line = ConsoleLine;
            if (line.Count > 0) {
                line.RemoveAt(line.Count - 1);
                // goes back a character, clears that character with space, then goes back again. i think
                Console.Write("\b \b");
            }
            if (line.Count <= 0) {
                DisposeTyping();
            }
        }
        private async void OnEnter()
        {
            if (ConsoleLine.Count <= 0) return;

            string line = string.Join("", ConsoleLine);
            ClearLine(true);
            switch (CurrentState)
            {
                case States.SettingChannel: {
                    (string cId, string gId) = line.SplitByFirstChar('|');

                    SocketGuild? guild = (SocketGuild?)(string.IsNullOrEmpty(gId) ? CurrentTalkingChannel?.Guild : Client.ParseGuild(gId));
                    var ch = guild?.ParseChannel(cId);
                    if (ch != null) {
                        CurrentTalkingChannel = ch;
                    } else {
                        LogError($"\nthat was null. is channel \"{cId}\" and guild \"{gId}\" correct?");
                    }

                    CurrentTitleState.UpdateTitle();

                    CurrentState = States.Typing;
                } break;
                default: { // or ConsoleState.Modes.Typing
                    DisposeTyping();

                    // with ConsoleKey.Escape, this is kinda redundant. keeping it anyways
                    if (line is "c" or "cancel" or "exit") {
                        MainHook.Instance.Stop();
                        return;
                    }

                    var talkingChannel = CurrentTalkingChannel;
                    if (talkingChannel != null) {
                        LogMessage(Client.CurrentUser, talkingChannel.Guild, talkingChannel, line);
                        _ = MainHook.Instance.MessageHandler(await talkingChannel.SendMessageAsync(line));
                    } else {
                        LogWarning("that's null. ouch");
                    }
                } break;
            }
        }
#endregion

        public async void StartReadingKeys()
        {
            PlayKeyPressStopwatch.Start();
            AutoCancelTyping();

            Dictionary<ConsoleKey, Action> keyActions = new() {
                { ConsoleKey.Escape, MainHook.Instance.Stop },
                { ConsoleKey.Backspace, OnBackspace },
                { ConsoleKey.Enter, OnEnter },
            };
            while (MainHook.Instance.KeepRunning)
            {
                while (!Console.KeyAvailable) await Task.Delay(10);
                ConsoleKeyInfo key = Console.ReadKey(true);

                int keySfx = new Random().Next(6) + 1;
                string path = $"keyboard/key_press_{keySfx}";
                if (SoundDeck.ClipExists(path)) {
                    if (PlayKeyPressStopwatch.Elapsed.Milliseconds > 10) {
                        SoundDeck.PlayOneShotClip(path);
                        PlayKeyPressStopwatch.Restart();
                    }
                } else {
                    LogError(path + " doesn't exist!!! i hope you die");
                }

                switch (key.Key)
                { // only use cases for ranges; for single keys just use keyActions
                    case >= ConsoleKey.F1 and <= ConsoleKey.F12: {
                        OnFunctionKey(key.Key);
                    } break;
                    case >= ConsoleKey.LeftArrow and <= ConsoleKey.DownArrow: {
                        OnArrowKey(key.Key);
                    } break;
                    default: {
                        if (keyActions.TryGetValue(key.Key, out var action)) {
                            action.Invoke();
                        } else {
                            TypeKey(key);
                        }
                    } break;
                }
            }
        }

        // public CaretakerConsole(List<ITextChannel?> TalkingChannels)
        // {
        //     this.TalkingChannels = [ ..TalkingChannels ];
        //     this.CurrentTalkingChannel = TalkingChannels[0];
        // }
    }
}