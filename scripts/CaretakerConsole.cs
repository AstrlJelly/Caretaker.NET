using System.Diagnostics;
using CaretakerNET.Audio;
using Discord;
using Discord.WebSocket;

namespace CaretakerNET
{
    public class CaretakerConsole
    {
        public enum States
        {
            Typing,
            SettingChannel,
        }
        public States CurrentState = States.Typing;
        public IDisposable? TypingState = null;
        public Stopwatch CancelTypingStopwatch = new();
        public ITextChannel?[] TalkingChannels = [];
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

        void AutoCancelTyping()
        {
            Task.Run(delegate {
                while (this != null) {
                    while (CancelTypingStopwatch.Elapsed.TotalSeconds > 8 && TypingState != null) {
                        DisposeTyping();
                    }
                }
            });
        }

        public void HistoryBack()
        {
            if (ConsoleLineHistory.Count > 0 && HistoryIndex > 0) {
                HistoryIndex--;
                ClearLine(false);
                ConsoleLine.AddRange(ConsoleLineHistory[HistoryIndex]);
                Console.Write(string.Join("", ConsoleLine));
            }
        }

        public void HistoryForward()
        {
            ClearLine(false);
            if (HistoryIndex < ConsoleLineHistory.Count) {
                HistoryIndex--;
                ConsoleLine.AddRange(ConsoleLineHistory[HistoryIndex]);
            }
            Console.Write(string.Join("", ConsoleLine));
        }
        public void StartReadingKeys()
        {
            AutoCancelTyping();

            Dictionary<ConsoleKey, Action> keyActions = new() {
                { ConsoleKey.Escape, MainHook.instance.Stop },
                { ConsoleKey.Backspace, delegate {
                    List<char> line = ConsoleLine;
                    if (line.Count > 0) {
                        line.RemoveAt(line.Count - 1);
                        // goes back a character, clears that character with space, then goes back again. i think
                        Console.Write("\b \b");
                    }
                    if (line.Count <= 0) {
                        DisposeTyping();
                    }
                }},
                { ConsoleKey.Enter, async delegate {
                    if (ConsoleLine.Count > 0) {
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

                                MainHook.instance.ts.UpdateTitle();

                                CurrentState = States.Typing;
                            } break;
                            default: { // or ConsoleState.Modes.Typing
                                DisposeTyping();

                                // with ConsoleKey.Escape, this is kinda redundant. keeping it anyways
                                if (line is "c" or "cancel" or "exit") {
                                    MainHook.instance.Stop();
                                    return;
                                }

                                var talkingChannel = CurrentTalkingChannel;
                                if (talkingChannel != null) {
                                    LogMessage(Client.CurrentUser, talkingChannel.Guild, talkingChannel, line);
                                    _ = MainHook.instance.MessageHandler(await talkingChannel.SendMessageAsync(line));
                                } else {
                                    LogWarning("that's null. ouch");
                                }
                            } break;
                        }
                    }
                }},
            };
            while (MainHook.instance.KeepRunning)
            {
                while (!Console.KeyAvailable);
                ConsoleKeyInfo key = Console.ReadKey(true);

                int keySfx = new Random().Next(6) + 1;
                string path = $"keyboard/key_press_{keySfx}";
                if (SoundDeck.ClipExists(path)) {
                    SoundDeck.PlayOneShotClip(path);
                } else {
                    LogError(path + " doesn't exist!!! i hope you die");
                }

                switch (key.Key)
                { // only use cases for ranges; for single keys just use keyActions
                    case >= ConsoleKey.F1 and <= ConsoleKey.F12: {
                        switch (key.Key)
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
                                int whichChannel = key.Key - ConsoleKey.F1;
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
                                MainHook.instance.ts.UpdateTitle();
                            } break;
                        }
                    } break;
                    case >= ConsoleKey.LeftArrow and <= ConsoleKey.DownArrow: {
                        // x from LEFT, y from TOP
                        (int left, int top) = Console.GetCursorPosition();
                        Action? action = key.Key switch {
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
    }
}