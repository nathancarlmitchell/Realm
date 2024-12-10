using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input.Touch;

namespace FlappyBox
{
    public static class TouchCollectionExtensions
    {
        private static bool _debug = false;

        public static bool AnyTouch(this TouchCollection touchState)
        {
            foreach (TouchLocation location in touchState)
            {
                if (
                    location.State == TouchLocationState.Pressed
                    || location.State == TouchLocationState.Moved
                    || location.State == TouchLocationState.Released
                )
                {
                    if (_debug)
                    {
                        Console.WriteLine("AnyTouch:" + location.Position);
                    }
                    return true;
                }
            }
            return false;
        }

        public static bool Pressed(this TouchCollection touchState)
        {
            foreach (TouchLocation location in touchState)
            {
                if (location.State == TouchLocationState.Pressed)
                {
                    if (_debug)
                    {
                        Console.WriteLine("Pressed:" + location.Position);
                    }
                    return true;
                }
            }
            return false;
        }

        public static bool Released(this TouchCollection touchState)
        {
            foreach (TouchLocation location in touchState)
            {
                if (location.State == TouchLocationState.Released)
                {
                    if (_debug)
                    {
                        Console.WriteLine("Released:" + location.Position);
                    }
                    return true;
                }
            }
            return false;
        }

        public static bool Moved(this TouchCollection touchState)
        {
            foreach (TouchLocation location in touchState)
            {
                if (location.State == TouchLocationState.Moved)
                {
                    if (_debug)
                    {
                        Console.WriteLine("Moved:" + location.Position);
                    }
                    return true;
                }
            }
            return false;
        }

        public static Vector2 GetPosition(this TouchCollection touchState)
        {
            int x = 0;
            int y = 0;

            foreach (TouchLocation location in touchState)
            {
                x = (int)location.Position.X;
                y = (int)location.Position.Y;

                return new Vector2(x, y);
            }
            if (_debug)
            {
                Console.WriteLine("GetPosition: " + new Vector2(x, y));
            }
            return new Vector2(x, y);
        }
    }
}
