using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.Direct3D9;

namespace Realm
{
    public abstract class Entity
    {
        public Texture2D image;
        public int Width
        {
            get { return image == null ? 0 : image.Width; }
        }
        public int Height
        {
            get { return image == null ? 0 : image.Height; }
        }

        // The tint of the image. This will also allow us to change the transparency.
        protected Color color = Color.White;
        public Vector2 Position,
            Velocity;
        public float Orientation;
        public float Radius = 20; // used for circular collision detection
        public bool IsExpired; // true if the entity was destroyed and should be deleted.

        // General-purpose debuff system, usable by both Enemy and Player
        // (unlike Player's buff fields, which are Player-only). Adding a new
        // debuff type is just a new enum value plus a DebuffIcon() case
        // below — the tracking, countdown, and indicator drawing all work
        // automatically. What a given debuff actually *does* (e.g. blocking
        // movement) is left to whichever subclass checks HasDebuff() for it.
        public enum DebuffType
        {
            Paralyzed,
            Stunned,
        }

        private readonly Dictionary<DebuffType, int> activeDebuffs = new();

        public bool HasDebuff(DebuffType type) => activeDebuffs.ContainsKey(type);

        // Applies (or refreshes to, not stacks with) a debuff for
        // durationFrames. Call UpdateDebuffs()/DrawDebuffIndicators() once
        // per frame from a subclass's Update()/Draw() to tick it down and
        // show it on screen.
        public void ApplyDebuff(DebuffType type, int durationFrames)
        {
            activeDebuffs[type] = durationFrames;
        }

        protected void UpdateDebuffs()
        {
            if (activeDebuffs.Count == 0)
                return;

            List<DebuffType> expired = null;
            foreach (var type in new List<DebuffType>(activeDebuffs.Keys))
            {
                if (--activeDebuffs[type] <= 0)
                {
                    (expired ??= []).Add(type);
                }
            }

            if (expired != null)
            {
                foreach (var type in expired)
                    activeDebuffs.Remove(type);
            }
        }

        // One small icon above the sprite per active debuff, side by side if
        // more than one is active — mirrors Player's buff indicator ("+")
        // but as an image rather than text (each debuff type is visually
        // distinct on its own, so no extra color-coding is needed), drawn in
        // the row above the buff row so the two don't overlap when both are
        // active on the same entity.
        private const int DebuffIconSize = 16; // paralyzed.png/stunned.png are 16x16
        private const int DebuffIconSpacing = 2;

        protected void DrawDebuffIndicators(SpriteBatch spriteBatch)
        {
            if (activeDebuffs.Count == 0)
                return;

            List<Texture2D> activeIcons = [];
            foreach (var type in activeDebuffs.Keys)
                activeIcons.Add(DebuffIcon(type));

            int step = DebuffIconSize + DebuffIconSpacing;
            float totalWidth = (step * activeIcons.Count) - DebuffIconSpacing;
            float startX = Position.X - (totalWidth / 2f);
            float y = Position.Y - (Size.Y / 2f) - (DebuffIconSize * 2) - 8;

            for (int i = 0; i < activeIcons.Count; i++)
            {
                spriteBatch.Draw(
                    activeIcons[i],
                    new Vector2(startX + (i * step), y),
                    Microsoft.Xna.Framework.Color.White
                );
            }
        }

        private static Texture2D DebuffIcon(DebuffType type) =>
            type switch
            {
                DebuffType.Paralyzed => Art.Paralyzed,
                DebuffType.Stunned => Art.Stunned,
                _ => null,
            };

        public Rectangle Bounds
        {
            get { return new Rectangle((int)Position.X, (int)Position.Y, Width, Height); }
        }

        public Vector2 Size
        {
            get { return image == null ? Vector2.Zero : new Vector2(image.Width, image.Height); }
        }

        public abstract void Update();

        public virtual void Update(GameTime gameTime) { }

        public virtual void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(image, Position, null, color, Orientation, Size / 2f, 1f, 0, 0);
        }
    }
}
