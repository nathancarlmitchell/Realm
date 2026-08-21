using System;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace Realm
{
    static class Sound
    {
        public static SoundEffect Button,
            LevelUp,
            NoMana,
            PlayerHit,
            MagicShoot,
            LootAppears,
            UsePotion,
            Blip,
            Song,
            DefaultHit,
            EnterRealm,
            Death,
            InventoryMoveItem,
            SpriteGodDeath,
            SpriteGodHit,
            SnakesHit,
            SnakesDeath,
            Error;

        public static SoundEffectInstance SongInstance;

        private static readonly Random rand = new();

        public static void Load(ContentManager content)
        {
            SoundEffect.MasterVolume = 0.75f;

            Button = content.Load<SoundEffect>("Sounds/button");
            Error = content.Load<SoundEffect>("Sounds/error");
            LevelUp = content.Load<SoundEffect>("Sounds/Player/level_up");
            NoMana = content.Load<SoundEffect>("Sounds/Player/no_mana");
            PlayerHit = content.Load<SoundEffect>("Sounds/Player/wizard_hit");
            MagicShoot = content.Load<SoundEffect>("Sounds/Player/magic_shoot");
            LootAppears = content.Load<SoundEffect>("Sounds/Player/loot_appears");
            UsePotion = content.Load<SoundEffect>("Sounds/Player/use_potion");
            Blip = content.Load<SoundEffect>("Sounds/blip");
            DefaultHit = content.Load<SoundEffect>("Sounds/Enemy/default_hit");
            EnterRealm = content.Load<SoundEffect>("Sounds/enter_realm");
            Death = content.Load<SoundEffect>("Sounds/Player/death");
            InventoryMoveItem = content.Load<SoundEffect>("Sounds/Player/inventory_move_item");
            SpriteGodDeath = content.Load<SoundEffect>("Sounds/Enemy/sprite_god_death");
            SpriteGodHit = content.Load<SoundEffect>("Sounds/Enemy/sprite_god_hit");
            SnakesHit = content.Load<SoundEffect>("Sounds/Enemy/snakes_hit");
            SnakesDeath = content.Load<SoundEffect>("Sounds/Enemy/snakes_death");

            Song = content.Load<SoundEffect>("Sounds/Music/snd_game");

            SongInstance = Song.CreateInstance();
            SongInstance.IsLooped = true;
            SongInstance.Volume = 0.25f;
        }

        public static void ToggleMute()
        {
            Game1.Mute = !Game1.Mute;
            Overlay.ToggleAudio();
            RefreshMusicState();
        }

        // Called once per dungeon entry (RealmState's constructor, which
        // BossRealmState inherits) to start the track, and again whenever
        // any of Player.Instance's Music* settings change (via
        // SettingsState.cs's Audio tab) or the master Game1.Mute toggles —
        // the single place that reconciles "should the song be playing at
        // all" and "at what volume" from every source that can affect
        // either, so none of those call sites need to duplicate this
        // logic or risk drifting out of sync with each other.
        public static void RefreshMusicState()
        {
            bool shouldPlay = !Game1.Mute && Player.Instance.MusicEnabled;
            bool audible = shouldPlay && !Player.Instance.MusicMuted;

            SongInstance.Volume = audible ? Player.Instance.MusicVolumePercent / 100f : 0f;

            if (shouldPlay)
            {
                if (SongInstance.State != SoundState.Playing)
                    SongInstance.Play();
            }
            else if (SongInstance.State == SoundState.Playing)
            {
                SongInstance.Pause();
            }
        }

        public static void PlaySong() => RefreshMusicState();

        public static void SongVolume(float volume)
        {
            volume = SongInstance.Volume + volume;

            if (volume > 1.0f)
            {
                volume = 1.0f;
            }
            if (volume < 0.0f)
            {
                volume = 0.0f;
            }

            SongInstance.Volume = volume;
        }

        // Every non-music sound effect routes through here (or the
        // pitchVariance overload below) — Game1.Mute is still the master
        // override (unchanged from before these settings existed), then
        // Player.Instance.SfxMuted/SfxVolumePercent apply on top, and
        // MagicShoot specifically also respects WeaponShotsMuted, since
        // that's the one sound Weapon.Shoot() plays for every class's
        // basic attack.
        private static bool ShouldPlaySfx(SoundEffect sound) =>
            !Game1.Mute
            && !Player.Instance.SfxMuted
            && !(Player.Instance.WeaponShotsMuted && sound == MagicShoot);

        public static void Play(SoundEffect sound, float volume)
        {
            if (ShouldPlaySfx(sound))
            {
                sound.Play(volume * (Player.Instance.SfxVolumePercent / 100f), 0.0f, 0.0f);
            }
        }

        public static void Play(SoundEffect sound, float volume, float pitchVariance)
        {
            if (ShouldPlaySfx(sound))
            {
                float pitch = (float)(
                    rand.NextDouble() * (pitchVariance - -pitchVariance) + -pitchVariance
                );

                sound.Play(volume * (Player.Instance.SfxVolumePercent / 100f), pitch, 0.0f);
            }
        }
    }
}
