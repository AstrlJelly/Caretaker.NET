using System;
using NAudio.Wave;

namespace CaretakerNET.Audio
{
    public class SoundDeck
    {
        public static readonly SoundDeck instance = new();

        public const string SFX_PATH = "./sfx/";
        
        private readonly List<WaveOutEvent> returnPool = new();
        private readonly List<WaveOutEvent> usingPool = new();

        private WaveOutEvent Get(string sfx) => Get(new AudioFileReader(SFX_PATH + sfx + ".wav"));
        private WaveOutEvent Get(AudioFileReader af)
        {
            WaveOutEvent wo;
            if (returnPool.Count > 0) {
                wo = returnPool[^1];
                returnPool.RemoveAt(returnPool.Count - 1);
            } else {
                wo = new();
                wo.PlaybackStopped += delegate {
                    instance.Return(wo);
                };
            }
            wo.Stop();
            wo.Init(af);
            usingPool.Add(wo);
            
            return wo;
        }

        private void Return(WaveOutEvent waveOutEvent)
        {
            waveOutEvent.Stop();
            usingPool.Remove(waveOutEvent);
            returnPool.Add(waveOutEvent);
        }

        public static void PlayOneShotClip(string sfx)
        {
            try
            {
                var af = new AudioFileReader(SFX_PATH + sfx + ".wav");
                var wo = instance.Get(af);
                // Log($"returnPool.Count : {instance.returnPool.Count}, usingPool.Count : {instance.usingPool.Count}");
                wo.Play();

                void Test(object? sender, StoppedEventArgs e) {
                    af.Dispose();
                    wo.PlaybackStopped -= Test;
                }
                wo.PlaybackStopped += Test;
            }
            catch (Exception err)
            {
                LogError(err);
                throw;
            }
        }

        public static bool ClipExists(string path)
        {
            return File.Exists(SFX_PATH + path + ".wav");
        }
    }
}