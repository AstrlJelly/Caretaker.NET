using System;
using NAudio.Wave;
using Microsoft.Extensions.ObjectPool;

namespace CaretakerNet.Audio
{
    public class AudioPlayer
    {
        public static readonly AudioPlayer instance = new();
        // public DefaultObjectPool<WaveOutEvent> PlayerPool { get; private set; } = new(
        //     new DefaultPooledObjectPolicy<WaveOutEvent> {

        //     }
        // );
        public AudioPoolHandler PlayerPool { get; private set; } = new();
        public class AudioPoolHandler() : ObjectPool<WaveOutEvent>
        {
            public override WaveOutEvent Get()
            {
                return new WaveOutEvent();
            }

            public override void Return(WaveOutEvent wo)
            {
                // PlayerPool.Return(wo);
            }
        }
        public static void PlayOneShot(string sfx)
        {
            var af = new AudioFileReader("./sfx/" + sfx);
            var wo = instance.PlayerPool.Get();
            wo.Init(af);
            wo.Play();
            wo.PlaybackStopped += delegate {
                af.Dispose();
                instance.PlayerPool.Return(wo);
            };
        }
    }
}