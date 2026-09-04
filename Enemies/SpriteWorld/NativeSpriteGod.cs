using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Realm.Projectiles;

namespace Realm
{
    // Sprite World (see Data/DungeonType_SpriteWorld.json) — sourced
    // directly from realmeye.com/wiki/native-sprite-god. Real art supplied
    // for all 5 elemental forms (Content/Dungeons/Sprite World/). A plain
    // Enemy subclass, not Boss — it fights inline in the dungeon with its
    // own small floating health bar, same "mini-boss, not a BossRealmState
    // encounter" precedent as SnakepitGuard.
    //
    // Not part of DungeonType_SpriteWorld.json's EnemyNames/
    // EnemySpawner.BasicEnemyPool — DungeonState's own constructor places
    // 1-2 of these directly into random rooms instead (see its own
    // comment), since the per-room spawner's uniform per-slot pick would
    // otherwise make a 3500 HP mini-boss absurdly common.
    //
    // The wiki's own raw attack table lists numbers without a clear
    // per-form column (a plain-text scrape strips the icons that would
    // normally disambiguate); the 2 attacks per form below are matched to
    // each form's own textual description as closely as the table allows,
    // not a guaranteed row-for-row mapping. "Silence" isn't a debuff this
    // engine has — every Silencing shot below uses DazesOnHit instead (see
    // Entity.DebuffType.Dazed's own doc comment), an explicit substitution,
    // not an oversight.
    class NativeSpriteGod : Enemy
    {
        private static readonly Random formRand = new();

        private enum Form
        {
            Darkness,
            Fire,
            Ice,
            Magic,
            Nature,
        }

        private Form currentForm;
        private bool hasTransformed = false;
        private const float TransformThreshold = 0.5f;
        private const int TransitionFrames = 60; // ~1s — the invulnerable window
        private const int SilenceDurationFrames = 240; // "Silenced for 4s" on the wiki
        private const int SilenceCooldown = 120;

        // Placeholder image passed to the base constructor — reassigned
        // (with Radius recomputed to match) the moment currentForm is
        // rolled just below, since which of the 5 forms to start in can't
        // be decided before base(...) runs. Every form's real image is the
        // same 94x94 size, so this never leaves Radius stale in practice,
        // but it's recomputed anyway rather than relying on that holding
        // forever.
        public NativeSpriteGod(Vector2 position)
            : base(Art.NativeSpriteGodDarkness, position)
        {
            currentForm = (Form)formRand.Next(5);
            image = GodImage(currentForm);
            Radius = image.Width / 2f;

            health = 3500;
            healthMax = 3500;
            Defense = 12;
            PointValue = 250;
            DropPool = SpriteWorldDropPool;
            DropChances = SpriteWorldDropChances;
            DropTierRanges = SpriteWorldDropTierRanges;

            // "may also drop Potions of Attack, at a noticeably increased
            // rate compared to regular Sprite Gods" — no exact number
            // published; 15% reads as "noticeably increased" against the
            // open-Realm Sprite God's own far rarer drop without being a
            // guarantee.
            GuaranteedPotionChances = new() { [Potions.Attack] = 0.15f };

            AddBehaviour(MoveTethered(wanderDistance: 96f));
            AddBehaviour(PhaseWatcher());
            AddAttackBehaviour(PrimaryAttack());
            AddAttackBehaviour(SilenceAttack());
        }

        private static Texture2D GodImage(Form form) =>
            form switch
            {
                Form.Darkness => Art.NativeSpriteGodDarkness,
                Form.Fire => Art.NativeSpriteGodFire,
                Form.Ice => Art.NativeSpriteGodIce,
                Form.Magic => Art.NativeSpriteGodMagic,
                _ => Art.NativeSpriteGodNature,
            };

        // "At 50% HP, the Native Sprite God will briefly go invulnerable
        // and transform into a random new form. It also permanently gains
        // the Armored status from this point on, giving it 18 DEF." — a
        // one-shot transform (hasTransformed), unlike SnakepitGuard's own
        // repeatable phase watcher, since the wiki only describes a single
        // transformation event.
        private IEnumerable<int> PhaseWatcher()
        {
            while (true)
            {
                if (!hasTransformed && HealthFraction <= TransformThreshold)
                {
                    hasTransformed = true;
                    FlashRed();
                    Invulnerable = true;

                    for (int i = 0; i < TransitionFrames; i++)
                        yield return 0;

                    Form newForm;
                    do
                    {
                        newForm = (Form)formRand.Next(5);
                    } while (newForm == currentForm);
                    currentForm = newForm;
                    image = GodImage(currentForm);
                    Radius = image.Width / 2f;
                    Defense = 18;

                    Invulnerable = false;
                }

                yield return 0;
            }
        }

        // Each form's own "shotgun" — the main damage source. Every form's
        // underlying FanShot() enumerator is created exactly once, up
        // front (each with its own independent cooldown state that has to
        // persist tick-to-tick) — same "build the enumerator once outside
        // the loop, MoveNext() it conditionally inside" shape SnakepitGuard.
        // SnakeSpit()/SnakeSpinners() already use for their own phase gate.
        // Only the enumerator matching currentForm ever advances, so an
        // inactive form's own cooldown correctly stays frozen rather than
        // ticking down (or firing) while some other form is active.
        private IEnumerable<int> PrimaryAttack()
        {
            var darkness = FanShot(
                range: 14.12f * 32f,
                damage: 100,
                projectileSpeed: 8f * 32f / 60f,
                shots: 4,
                angleStep: 0.2f,
                projectileImage: Art.SpriteDarknessBolt,
                accelerationMagnitude: -20f * 32f / 3600f,
                minSpeed: 5f * 32f / 60f
            ).GetEnumerator();
            var magic = FanShot(
                range: 11f * 32f,
                damage: 90,
                projectileSpeed: 5.5f * 32f / 60f,
                shots: 4,
                angleStep: 0.2f,
                projectileImage: Art.SpriteMagicTwirl
            ).GetEnumerator();
            var ice = FanShot(
                range: 11f * 32f,
                damage: 10,
                projectileSpeed: 5.5f * 32f / 60f,
                shots: 4,
                angleStep: 0.2f,
                projectileImage: Art.SpriteIceBolt,
                slowsOnHit: true
            ).GetEnumerator();
            var fire = FanShot(
                range: 10.5f * 32f,
                damage: 50,
                projectileSpeed: 7f * 32f / 60f,
                shots: 4,
                angleStep: 0.2f,
                projectileImage: Art.SpriteFireBolt
            ).GetEnumerator();
            var nature = FanShot(
                range: 11.05f * 32f,
                damage: 120,
                projectileSpeed: 0f,
                shots: 4,
                angleStep: 0.2f,
                projectileImage: Art.SpriteNatureBolt,
                accelerationMagnitude: 20f * 32f / 3600f,
                maxSpeed: 9.5f * 32f / 60f
            ).GetEnumerator();

            while (true)
            {
                if (!Invulnerable)
                {
                    switch (currentForm)
                    {
                        case Form.Darkness:
                            darkness.MoveNext();
                            break;
                        case Form.Magic:
                            magic.MoveNext();
                            break;
                        case Form.Ice:
                            ice.MoveNext();
                            break;
                        case Form.Fire:
                            fire.MoveNext();
                            break;
                        default:
                            nature.MoveNext();
                            break;
                    }
                }
                yield return 0;
            }
        }

        // Each form's own Silence attack — 0 damage on the wiki's own
        // table, DazesOnHit substituting for Silence throughout (see this
        // class's own header comment). Magic's is a boomerang (matching
        // "aimed cyan boomerangs that Silence" specifically), hand-rolled
        // since FanShot/ShootIfInRange don't spawn BoomerangProjectile.
        private int magicSilenceCooldownRemaining = 0;

        private IEnumerable<int> SilenceAttack()
        {
            // Same "build every form's own enumerator once outside the
            // loop, MoveNext() only the active one" shape as PrimaryAttack()
            // above — see its own comment for why.
            var darkness = ShootIfInRange(
                range: 7.19f * 32f,
                damage: 0,
                projectileSpeed: 9f * 32f / 60f,
                projectileImage: Art.SpriteDarknessTwirl,
                cooldownFrames: SilenceCooldown,
                dazesOnHit: true,
                dazeDurationFrames: SilenceDurationFrames
            ).GetEnumerator();
            var ice = FanShot(
                range: 26f * 32f,
                damage: 0,
                projectileSpeed: 6.5f * 32f / 60f,
                shots: 3,
                angleStep: 0.3f,
                projectileImage: Art.SpriteIceTwirl,
                cooldownFrames: SilenceCooldown,
                dazesOnHit: true,
                dazeDurationFrames: SilenceDurationFrames
            ).GetEnumerator();
            var fire = FanShot(
                range: 7.875f * 32f,
                damage: 0,
                projectileSpeed: 4.5f * 32f / 60f,
                shots: 3,
                angleStep: 0.3f,
                projectileImage: Art.SpriteFireTwirl,
                cooldownFrames: SilenceCooldown,
                dazesOnHit: true,
                dazeDurationFrames: SilenceDurationFrames
            ).GetEnumerator();
            var nature = ShootIfInRange(
                range: 12f * 32f,
                damage: 0,
                projectileSpeed: 2f * 32f / 60f,
                projectileImage: Art.SpriteNatureTwirl,
                cooldownFrames: SilenceCooldown,
                accelerationMagnitude: 40f * 32f / 3600f,
                maxSpeed: 11f * 32f / 60f,
                dazesOnHit: true,
                dazeDurationFrames: SilenceDurationFrames
            ).GetEnumerator();

            while (true)
            {
                if (Invulnerable)
                {
                    yield return 0;
                    continue;
                }

                if (currentForm == Form.Magic)
                {
                    if (magicSilenceCooldownRemaining <= 0)
                    {
                        Vector2 aim = Player.Instance.Position - Position;
                        float range = 19.5f * 32f;
                        if (aim.LengthSquared() > 0 && aim.LengthSquared() <= range * range)
                        {
                            magicSilenceCooldownRemaining = SilenceCooldown;
                            Vector2 vel = Extensions.FromPolar(aim.ToAngle(), 6.5f * 32f / 60f);
                            EntityManager.Add(
                                new BoomerangProjectile(Position, vel, 40, Art.SpriteMagicTwirl)
                                {
                                    Damage = 0,
                                    DazesOnHit = true,
                                    DazeDurationFrames = SilenceDurationFrames,
                                }
                            );
                        }
                    }
                    if (magicSilenceCooldownRemaining > 0)
                        magicSilenceCooldownRemaining--;

                    yield return 0;
                    continue;
                }

                switch (currentForm)
                {
                    case Form.Darkness:
                        darkness.MoveNext();
                        break;
                    case Form.Ice:
                        ice.MoveNext();
                        break;
                    case Form.Fire:
                        fire.MoveNext();
                        break;
                    default:
                        nature.MoveNext();
                        break;
                }

                yield return 0;
            }
        }
    }
}
