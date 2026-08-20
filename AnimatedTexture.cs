using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Realm;

/// <summary>
/// A helper class for handling animated textures.
/// </summary>
namespace Realm
{
    public class AnimatedTexture
    {
        // Number of frames in the animation.
        private int frameCount;

        // Frames per row in the spritesheet — lets a sheet wrap onto
        // multiple rows (e.g. a 7-frame animation laid out 5-wide, 2 rows)
        // instead of requiring one long horizontal strip. Defaults to
        // frameCount in Load() below, which reproduces the old single-row
        // behavior exactly for every existing caller.
        private int columns;

        // The animation spritesheet.
        private Texture2D myTexture;

        // The number of frames to draw per second.
        private float timePerFrame;

        // The current frame being drawn.
        private int frame;

        // Total amount of time the animation has been running.
        private float totalElapsed;

        // Is the animation currently running?
        private bool isPaused;

        // Whether the animation wraps back to frame 0 after the last frame
        // (the original, still-default behavior) or holds on the last
        // frame forever instead — e.g. an "opening" animation that should
        // play once and stay open, rather than replaying "closed" on a
        // loop.
        private bool loop;

        // The current rotation, scale and draw depth for the animation.
        public float Rotation,
            Scale,
            Depth,
            Opacity;

        // The origin point of the animated texture.
        public Vector2 Origin;

        public AnimatedTexture(Vector2 origin, float rotation, float scale, float depth)
        {
            this.Origin = origin;
            this.Rotation = rotation;
            this.Scale = scale;
            this.Depth = depth;
            this.Opacity = 1f;
        }

        public void Load(
            ContentManager content,
            string asset,
            int frameCount,
            int framesPerSec,
            int columns = 0,
            bool loop = true
        )
        {
            this.frameCount = frameCount;
            this.columns = columns > 0 ? columns : frameCount;
            this.loop = loop;
            myTexture = content.Load<Texture2D>(asset);
            timePerFrame = (float)1 / framesPerSec;
            frame = 0;
            totalElapsed = 0;
            isPaused = false;
        }

        public void UpdateFrame(float elapsed)
        {
            if (isPaused)
                return;

            // Already holding on the last frame — nothing left to advance.
            if (!loop && frame >= frameCount - 1)
                return;

            totalElapsed += elapsed;
            if (totalElapsed > timePerFrame)
            {
                frame++;
                if (loop)
                {
                    // Keep the Frame between 0 and the total frames, minus one.
                    frame %= frameCount;
                }
                else if (frame >= frameCount - 1)
                {
                    frame = frameCount - 1;
                }
                totalElapsed -= timePerFrame;
            }
        }

        public void DrawFrame(SpriteBatch batch, Vector2 screenPos)
        {
            DrawFrame(batch, frame, screenPos);
        }

        public void DrawFrame(SpriteBatch batch, int frame, Vector2 screenPos)
        {
            int rows = (frameCount + columns - 1) / columns;
            int FrameWidth = myTexture.Width / columns;
            int FrameHeight = myTexture.Height / rows;
            Rectangle sourcerect = new Rectangle(
                FrameWidth * (frame % columns),
                FrameHeight * (frame / columns),
                FrameWidth,
                FrameHeight
            );
            batch.Draw(
                myTexture,
                screenPos,
                sourcerect,
                Color.White * Opacity,
                Rotation,
                Origin,
                Scale,
                SpriteEffects.None,
                Depth
            );
        }

        public bool IsPaused
        {
            get { return isPaused; }
        }

        // Native (unscaled) size of a single frame — lets callers that draw
        // this texture at its own Scale (e.g. Portal's label centering)
        // work correctly regardless of the sheet's frame/column layout.
        public int FrameWidth => myTexture.Width / columns;
        public int FrameHeight => myTexture.Height / ((frameCount + columns - 1) / columns);

        public void Reset()
        {
            frame = 0;
            totalElapsed = 0f;
        }

        public void Stop()
        {
            Pause();
            Reset();
        }

        public void Play()
        {
            isPaused = false;
        }

        public void Pause()
        {
            isPaused = true;
        }
    }
}
