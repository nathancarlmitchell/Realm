using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Realm
{
    // Screen-space darkness overlay drawn on top of the entire gameplay
    // world each frame, replacing an earlier per-tile/per-entity approach
    // (RealmState.DrawBiomeRings()/DrawBeachFeature(), DungeonMap.Draw(),
    // Enemy.Draw() each individually deciding whether a given cell/entity
    // sat inside Game1.VisionRadius of the camera). That approach compared
    // the camera's own continuously-moving world position against a fixed
    // 32px tile grid, so boundary tiles/entities popped fully on/off from
    // one frame to the next as the player walked — visibly reshaping the
    // "circle" itself, not just revealing/hiding content at its edge.
    //
    // This overlay can't jitter the same way: Camera.GetTransformation()
    // always maps Camera.Pos to the exact center of the gameplay viewport
    // by construction (translate by -Camera.Pos, then by +viewport/2), so
    // drawing one static image at that fixed screen position, every frame,
    // regardless of where the camera actually is in world space, produces
    // an outline that never moves or reshapes relative to the screen. A
    // world-space fade (entry 357's attempt) was rejected -- still readable
    // as movement, would need to be recomputed as the world scrolled
    // beneath it -- so this instead darkens everything already drawn,
    // rather than trying to compute how lit each thing is.
    public static class VisionFog
    {
        // 1 texel == 1 world unit, so Game1.VisionRadius/VisionFeatherWidth
        // (both world px) can feed Game1.GetVisionAlpha() directly as texel
        // distances with no extra scale factor. Half-size (768px)
        // comfortably exceeds the gameplay viewport's own half-diagonal
        // (~608px, at the fixed 980x720 GameplayViewportWidth/Height), so
        // the texture's fully-opaque outer band covers all four screen
        // corners with margin regardless of where the circle itself sits
        // within it.
        private const int TextureSize = 1536;

        private static Texture2D texture;

        // Call from world-content drawing (RealmState.Draw(), after
        // entities/portals/loot are drawn) so it composites on top of all
        // of it. Screen-space draw -- call this from a spriteBatch.Begin()
        // pass with no camera transform (the default identity transform),
        // not the world-space pass entities/tiles draw through.
        public static void Draw(SpriteBatch spriteBatch)
        {
            EnsureTexture(spriteBatch.GraphicsDevice);

            Vector2 origin = new(TextureSize / 2f, TextureSize / 2f);
            Vector2 screenCenter = new(
                Game1.GameplayViewportWidth / 2f,
                Game1.GameplayViewportHeight / 2f
            );

            spriteBatch.Draw(
                texture,
                screenCenter,
                null,
                Color.White,
                0f,
                origin,
                1f,
                SpriteEffects.None,
                0f
            );
        }

        // Built once, lazily, on first Draw() -- a ~2.36M-pixel radial
        // gradient computed in a plain loop, not per frame.
        private static void EnsureTexture(GraphicsDevice device)
        {
            if (texture != null)
                return;

            var pixels = new Color[TextureSize * TextureSize];
            Vector2 center = new(TextureSize / 2f, TextureSize / 2f);

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);

                    // GetVisionAlpha() returns how LIT a point is (1 near
                    // the center, fading to 0 at VisionRadius) -- this
                    // texture instead needs how DARK to paint over it, the
                    // inverse of that same curve, so both stay driven by
                    // the exact same shared radius/feather constants.
                    float darkness = 1f - Game1.GetVisionAlpha(dist);
                    pixels[y * TextureSize + x] = new Color((byte)0, (byte)0, (byte)0, (byte)(darkness * 255f));
                }
            }

            texture = new Texture2D(device, TextureSize, TextureSize);
            texture.SetData(pixels);
        }
    }
}
