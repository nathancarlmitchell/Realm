using System;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

namespace Realm
{
    static class Sound
    {
        public static SoundEffect Button,
            LevelUp,
            NoMana,
            WizardHit,
            MagicShoot,
            LootAppears,
            UsePotion,
            Blip,
            Song,
            DefaultHit,
            EnterRealm;

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
            LevelUp = content.Load<SoundEffect>("Sounds/Player/level_up");
            NoMana = content.Load<SoundEffect>("Sounds/Player/no_mana");
            WizardHit = content.Load<SoundEffect>("Sounds/Player/wizard_hit");
            MagicShoot = content.Load<SoundEffect>("Sounds/Player/magic_shoot");
            LootAppears = content.Load<SoundEffect>("Sounds/Player/loot_appears");
            UsePotion = content.Load<SoundEffect>("Sounds/Player/use_potion");
            Blip = content.Load<SoundEffect>("Sounds/blip");
            DefaultHit = content.Load<SoundEffect>("Sounds/Enemy/default_hit");
            EnterRealm = content.Load<SoundEffect>("Sounds/enter_realm");

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
