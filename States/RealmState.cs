using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Realm.States
{
    public class RealmState : State
    {
        Rectangle targetRectangle;

        // Ground textures for each biome ring (Data/BiomeData.json),
        // resolved once here rather than re-resolving Content.Load() every
        // Draw() call — sorted ascending by MaxDistance so DrawBiomeRings()
        // can just walk it back-to-front (farthest/largest ring painted
        // first, nearest/smallest painted last on top), the same "just
        // overdraw the next ring on top" trick that turns a handful of
        // plain filled squares into concentric rings with no actual
        // ring/donut geometry needed. Left empty for BossRealmState (see
        // SpawnsRegularEnemies below) — Draw() falls back to the single
        // flat Art.Tile background when this is empty, same as before
        // biomes existed.
        private readonly List<(Data.BiomeData biome, Texture2D texture)> biomeRings = [];

        // Extension points for BossRealmState (a bounded arena instance
        // instead of the open Realm world, and no regular EnemySpawner
        // traffic) — pure constants, safe to read from this base
        // constructor since C# virtual dispatch during construction already
        // resolves to the most-derived override.
        protected virtual bool SpawnsRegularEnemies => true;
        protected virtual int InstanceWorldWidth => Game1.WorldWidth;
        protected virtual int InstanceWorldHeight => Game1.WorldHeight;

        // Boss-arena-specific HUD (name+health bar, appearance announcement)
        // — empty here since the open Realm/regular dungeons never have a
        // Boss; BossRealmState overrides it. Called from the same
        // screen-space spriteBatch.Begin()/End() pair as the rest of the HUD
        // below, so it can use plain screen coordinates like Overlay.cs does.
        protected virtual void DrawBossHud(SpriteBatch spriteBatch) { }

        // Extension point for any additional on-screen directional
        // indicator an instance wants beyond the Beach Beacon compass arrow
        // above (e.g. DungeonState's own arrow toward its boss room's
        // portal) — same shape/placement as DrawBossHud() just above it.
        // Empty here; the open Realm/boss arenas have nothing else to point
        // at.
        protected virtual void DrawQuestIndicator(SpriteBatch spriteBatch) { }

        // Background extension point for DungeonState (real wall/tile-atlas
        // rendering via DungeonMap.Draw()) — same shape as DrawBossHud()
        // above. Owns its own spriteBatch.Begin()/End() pair (rather than
        // being called inside one Draw() already opened) specifically so a
        // subclass can pick its own SamplerState — DungeonState overrides it
        // with SamplerState.PointClamp instead of this default LinearWrap,
        // since linear-filtering a shared tile atlas bleeds a sliver of the
        // adjacent packed tile in at every seam (worse with camera scroll
        // landing on sub-pixel positions — reported as "flashing colors" at
        // tile seams while moving). Default body here is exactly what Draw()
        // always drew before this was extracted: biome rings if this
        // instance has any, the flat Art.Tile background otherwise — zero
        // behavior change for the open Realm/boss arenas, which have no
        // shared-atlas seams to bleed across in the first place (each biome
        // ring draws its own single texture, not sub-rectangles of one
        // packed atlas).
        protected virtual void DrawBackground(SpriteBatch spriteBatch)
        {
            spriteBatch.Begin(
                SpriteSortMode.FrontToBack,
                BlendState.AlphaBlend,
                SamplerState.LinearWrap,
                DepthStencilState.Default,
                RasterizerState.CullNone,
                null,
                Game1.Camera.GetTransformation()
            );

            if (biomeRings.Count > 0)
                DrawBiomeRings(spriteBatch);
            else
                spriteBatch.Draw(Art.Tile, new Vector2(32, 32), targetRectangle, Color.White);

            spriteBatch.End();
        }

        public static Guid HealthPotionGuid = Guid.NewGuid();
        public static Guid ManaPotionGuid = Guid.NewGuid();
        public static Guid AttackPotionGuid = Guid.NewGuid();
        public static Guid DefensePotionGuid = Guid.NewGuid();
        public static Guid DexterityPotionGuid = Guid.NewGuid();
        public static Guid LifePotionGuid = Guid.NewGuid();
        public static Guid ManaGuid = Guid.NewGuid();
        public static Guid SpeedPotionGuid = Guid.NewGuid();
        public static Guid VitalityPotionGuid = Guid.NewGuid();
        public static Guid WisdomPotionGuid = Guid.NewGuid();

        // Set (via ??=, only if not already set) by BossRealmState/
        // DungeonState's own constructor, right before it overwrites Player.
        // Instance.Position with a spawn point that only makes sense in that
        // bounded instance's own tiny coordinate space (2000x2000 for a boss
        // arena, 3200x3200 for a dungeon) — the value captured is wherever
        // the player actually was, in whatever shared-coordinate-space state
        // they were in (the open Realm, or Nexus — both use Game1.WorldWidth/
        // Height and never reinterpret coordinates, unlike a bounded
        // instance), immediately before entering it. The ??= (rather than a
        // plain assignment) matters for a dungeon's own boss room: entering
        // the dungeon already captured the true pre-dungeon position;
        // entering its boss arena from inside the dungeon must NOT overwrite
        // that with the dungeon's own (already bounded, already wrong to
        // restore to) coordinate.
        //
        // Consumed once — by RealmState's own constructor below (the
        // instance's Realm-bound exit, e.g. Boss.OnDeath()'s dropped
        // portal) OR by NexusState's constructor (the instance's OTHER
        // exit, its own separately-dropped Nexus portal, taken instead of
        // killing the boss/finishing the dungeon) — whichever the player
        // actually walks into. Both restore it the same way: before their
        // own Camera is constructed (its initial _pos is captured from
        // Player.Instance.Position at that moment), so the player reappears
        // exactly where they left off instead of at that small-instance
        // coordinate reinterpreted in the far larger (500,000px) shared
        // world — which, being close to (0,0), used to land them right at
        // the world's true edge and visibly stick there against Camera.
        // Pos's own edge-barrier clamp (Camera.cs), exactly the "camera
        // stopped tracking the player" symptom reported after leaving a
        // boss room (originally fixed for the direct Boss/Dungeon -> Realm
        // path only — entry fd2035a; NexusState initially just discarded
        // this instead of restoring it, so the identical symptom kept
        // happening via a bounded instance's other exit until fixed here
        // too).
        //
        // internal rather than protected — NexusState (a sibling State, not
        // a RealmState subclass) needs to consume this too.
        //
        // Null (the default, and its state after being consumed/cleared)
        // means nothing pending to restore — the normal Realm <-> Nexus walk
        // with no bounded instance involved, which already carries a
        // perfectly valid shared-world position over as-is and must not be
        // touched.
        internal static Vector2? PendingReturnPosition;

        public RealmState(Game1 game, GraphicsDevice graphicsDevice, ContentManager content)
            : base()
        {
            Debug.WriteLine("New RealmState created.");

            // Only for a real open-world instance (not the bounded
            // BossRealmState/DungeonState currently setting this) — see
            // PendingReturnPosition's own doc comment above. Runs before
            // Camera is constructed just below, so the fresh Camera's own
            // initial _pos (set from Player.Instance.Position in its
            // constructor) already reflects the restored position instead
            // of needing a second fixup call afterward.
            if (SpawnsRegularEnemies && PendingReturnPosition.HasValue)
            {
                Player.Instance.Position = PendingReturnPosition.Value;
                PendingReturnPosition = null;
            }

            Sound.PlaySong();

            Game1.Camera = new Camera(
                Game1.GameplayViewportWidth,
                Game1.GameplayViewportHeight,
                InstanceWorldWidth,
                InstanceWorldHeight,
                1f
            );

            // Both — every other save site in the game pairs these (StateManager,
            // GameOverState). Saving PlayerData alone here left InventoryData stale
            // on disk if equipment was dragged in/out in the nexus right before
            // entering a dungeon, e.g. an unequipped item's slot showing as empty on
            // disk while the item that should be in the inventory file wasn't there
            // yet — a real desync risk, even if not the exact one reported.
            Util.SavePlayerData();
            Util.SaveInventoryData();
            Util.SaveBankData();
            Util.SaveFameData();

            ItemSpawner.Reset();
            Portal.Reset();

            // Distance-based enemy spawn density (EnemySpawner.Update())
            // measures from wherever the player entered this Realm instance
            // — only meaningful for a regular dungeon that actually runs
            // EnemySpawner, not the boss arena.
            if (SpawnsRegularEnemies)
            {
                EnemySpawner.SetEntryPosition(Player.Instance.Position);

                // Sorted ascending by MaxDistance regardless of the JSON's
                // own authoring order — DrawBiomeRings() relies on this
                // exact order (drawn back-to-front, so it can't assume the
                // catalog file itself stays sorted).
                foreach (var biome in Game1.Instance.Biomes.OrderBy(b => b.MaxDistance))
                {
                    biomeRings.Add((biome, content.Load<Texture2D>(biome.GroundTileImageName)));
                }

                // Beach Beacon: one per Realm instance, at a random point
                // within the Beach ring (always the innermost — Data/
                // BiomeData.json's own MinDistance 0 — so every regular
                // dungeon entry gets exactly one). Uniform over the ring's
                // AREA, not just its radius — sampling radius directly
                // would bunch points near the center, since a thin band
                // near the middle covers far less area than an
                // equally-thin band near the outer edge.
                var beachBiome = Game1.Instance.Biomes.FirstOrDefault(b => b.Name == "Beach");
                if (beachBiome != null)
                {
                    var beaconRand = new Random();
                    float angle = (float)(beaconRand.NextDouble() * MathHelper.TwoPi);
                    float radius =
                        beachBiome.MaxDistance * MathF.Sqrt((float)beaconRand.NextDouble());
                    Vector2 beaconPosition =
                        EnemySpawner.EntryPosition + Extensions.FromPolar(angle, radius);
                    EntityManager.Add(new BeachBeacon(beaconPosition));
                }
            }

            // Leaving the Nexus — its fixed portal set no longer applies to
            // the minimap until the player returns (NexusState's
            // constructor re-sets this).
            Portal.NexusPortals = null;

            // Define a drawing rectangle based on the number of tiles wide and high, using the texture dimensions.
            targetRectangle = new Rectangle(0, 0, InstanceWorldWidth, InstanceWorldHeight);
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            DrawBackground(spriteBatch);

            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                null,
                null,
                null,
                null,
                null,
                Game1.Camera.GetTransformation()
            );

            // Drawn before portals below — a Beacon is a background
            // landmark and should sit beneath any portal that happens to
            // overlap it on screen. A no-op outside a Beach-containing
            // Realm instance.
            EntityManager.DrawBeachBeacon(spriteBatch);

            // Draw portals dropped in the world (e.g. by a defeated
            // SpriteGod, or a boss arena's own exit portal).
            foreach (Portal portal in Portal.DroppedPortals)
            {
                portal.Draw(spriteBatch, gameTime);
            }

            // Draw entities (player, enemies, projectiles).
            EntityManager.Draw(spriteBatch);

            if (Game1._Debug || Player.Instance.ShowHitboxesEnabled)
            {
                EntityManager.DrawHitboxes(spriteBatch, Portal.DroppedPortals);
            }

            spriteBatch.End();

            spriteBatch.Begin();

            // Draw audio.
            Overlay.DrawAudio(spriteBatch);

            // Draw loot. Iterate a snapshot — DrawLoot() can remove the bag from
            // ItemSpawner.LootBags (when the last item is picked up), which would
            // otherwise throw for mutating the list mid-enumeration.
            foreach (LootBag bag in ItemSpawner.LootBags.ToList())
            {
                bag.DrawLoot(spriteBatch);
            }

            // Draw the HUD sidebar (stats, XP, health, mana, ability,
            // equipment, inventory, in that order).
            Overlay.DrawSidebar(spriteBatch);

            // Draw the portal-entry confirmation prompt (sidebar, below the
            // inventory grid), if the player is currently standing in one —
            // no-op otherwise.
            Portal.DrawConfirmationPrompt(gameTime, spriteBatch);

            Player.Instance.Inventory.DrawDragGhost(spriteBatch);

            // Draw Fame, top-left — same corner Score/Hi Score used to occupy.
            Overlay.DrawFame(spriteBatch);

            // Compass arrow around the player pointing at the Beach
            // Beacon — a no-op via BeachBeacon.ActiveInstance whenever
            // none exists (also runs harmlessly in a boss arena, since
            // BossRealmState inherits this same Draw() and its own
            // constructor's EntityManager.Reset() already expires any
            // Beacon from before entering the fight).
            Overlay.DrawBeaconIndicator(spriteBatch);

            DrawBossHud(spriteBatch);

            DrawQuestIndicator(spriteBatch);

            if (Game1._Debug)
            {
                Overlay.DrawDebug(spriteBatch);
            }

            spriteBatch.End();
        }

        // Concentric biome rings, centered on EnemySpawner.EntryPosition —
        // the exact same point EnemySpawner itself measures distance from
        // to pick which biome's enemy pool applies (GetCurrentBiome()), so
        // the ground the player sees always lines up with what can spawn
        // on it. No real ring/donut geometry: biomeRings is sorted
        // ascending by MaxDistance, so painting largest-to-smallest just
        // lets each nearer ring's opaque square overdraw the farther one
        // underneath it, leaving only the band between two consecutive
        // MaxDistance values visible for each biome — the same trick as
        // painting concentric squares in any raster editor.
        private void DrawBiomeRings(SpriteBatch spriteBatch)
        {
            Vector2 entryPos = EnemySpawner.EntryPosition;

            for (int i = biomeRings.Count - 1; i >= 0; i--)
            {
                var (biome, texture) = biomeRings[i];
                float half = biome.MaxDistance;
                Vector2 topLeft = entryPos - new Vector2(half, half);
                Rectangle ringRect = new(0, 0, (int)(half * 2f), (int)(half * 2f));
                Color tint = new(biome.TintR, biome.TintG, biome.TintB);

                spriteBatch.Draw(texture, topLeft, ringRect, tint);
            }
        }

        public override void PostUpdate(GameTime gameTime) { }

        public override void Update(GameTime gameTime)
        {
            EntityManager.Update();

            if (SpawnsRegularEnemies)
                EnemySpawner.Update();

            foreach (Portal portal in Portal.DroppedPortals.ToList())
            {
                portal.Update(gameTime);
            }

            // Update high score.
            if (Player.Instance.ExperienceTotal > Player.Instance.HighScore)
            {
                int starsBefore = Player.ComputeStars(Player.Instance.HighScore);

                Player.Instance.HighScore = Player.Instance.ExperienceTotal;

                // Keeps the permanent per-class star record current on
                // every new high, not just star-threshold crossings — a
                // class's stars (CharacterCreationState.cs) are read from
                // this record now, not from any one character's own save
                // file (a class can have zero, one, or many characters), so
                // it needs to track the true best-ever value between
                // thresholds too, not just jump at each one.
                ClassRecordSystem.RecordHighScore(Player.PlayerClass, Player.Instance.HighScore);

                int starsAfter = Player.ComputeStars(Player.Instance.HighScore);

                // Persisted immediately when crossing a star threshold —
                // same reasoning as Player.LevelUp()'s Star 1 save, so a
                // newly-earned star doesn't depend on the player dying or
                // otherwise hitting a save checkpoint first. Gated on
                // starsAfter > starsBefore rather than saving on every
                // HighScore increment — HighScore can climb every frame
                // during active play, and only a threshold crossing is
                // actually worth a disk write.
                if (starsAfter > starsBefore)
                {
                    Util.SavePlayerData();
                    Util.SaveInventoryData();
                    Util.SaveBankData();
                    Util.SaveFameData();
                    Util.SaveClassRecordsData();
                }
            }
        }
    }
}
