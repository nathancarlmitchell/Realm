using System;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

namespace Realm
{
    static class Sound
    {
        public static SoundEffect Jump,
            Coin,
            Button,
            Locked,
            LevelUp,
            Blip,
            Song;

        public static Song mp3;

        public static SoundEffectInstance songInstance;

        private static readonly Random rand = new();

        public static void Load(ContentManager content)
        {
            SoundEffect.MasterVolume = 0.75f;

            //Jump = content.Load<SoundEffect>("Sounds/jump");
            //Coin = content.Load<SoundEffect>("Sounds/coin");
            Button = content.Load<SoundEffect>("Sounds/button");
            //Locked = content.Load<SoundEffect>("Sounds/locked");
            LevelUp = content.Load<SoundEffect>("Sounds/level_up");
            Blip = content.Load<SoundEffect>("Sounds/blip");

            Song = content.Load<SoundEffect>("Sounds/Music/snd_game");

            songInstance = Song.CreateInstance();
            songInstance.IsLooped = true;
            songInstance.Volume = 0.3f;
            if (!Game1.Mute)
                songInstance.Play();

            //mp3 = content.Load<Song>("Sounds/Music/8bit bossa");
            //MediaPlayer.Play(mp3);
            //MediaPlayer.IsRepeating = true;
            //MediaPlayer.MediaStateChanged += MediaPlayer_MediaStateChanged;
        }

        public static void ToggleMute()
        {
            Game1.Mute = !Game1.Mute;
            //Overlay.ToggleAudio();
            songInstance.Volume = 0.0f;
            if (!Game1.Mute)
            {
                songInstance.Volume = 0.5f;
            }
        }

        public static void SongVolume(float volume)
        {
            volume = songInstance.Volume + volume;

            if (volume > 1.0f)
            {
                volume = 1.0f;
            }
            if (volume < 0.0f)
            {
                volume = 0.0f;
            }

            songInstance.Volume = volume;
        }

        public static void Play(SoundEffect sound, float volume)
        {
            if (!Game1.Mute)
            {
                sound.Play(volume, 0.0f, 0.0f);
            }
        }

        public static void Play(SoundEffect sound, float volume, float pitchVariance)
        {
            if (!Game1.Mute)
            {
                float pitch = (float)(
                    rand.NextDouble() * (pitchVariance - -pitchVariance) + -pitchVariance
                );

                sound.Play(volume, pitch, 0.0f);
            }
        }
    }
}
