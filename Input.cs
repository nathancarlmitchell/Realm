using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Realm.States;

namespace Realm;

public static class Input
{
    private static readonly Game1 game = Game1.Instance;
    private static readonly GraphicsDevice graphicsDevice = Game1.Instance.GraphicsDevice;
    private static readonly ContentManager content = Game1.Instance.Content;

    private static KeyboardState keyboard,
        previousKeyboard;

    public static void Update(State currentState)
    {
        previousKeyboard = keyboard;
        keyboard = Keyboard.GetState();

        //TouchCollection touchState = TouchPanel.GetState();

        // Universal input.
        //
        // Toggle Mute
        //if (keyboard.IsKeyDown(Keys.M) && !previousKeyboard.IsKeyDown(Keys.M))
        //{
        //    Sound.ToggleMute();
        //}

        //// Volume up
        //if (keyboard.IsKeyDown(Keys.Add) && !previousKeyboard.IsKeyDown(Keys.Add))
        //{
        //    Sound.SongVolume(0.05f);
        //}

        //// Volume down
        //if (keyboard.IsKeyDown(Keys.Subtract) && !previousKeyboard.IsKeyDown(Keys.Subtract))
        //{
        //    Sound.SongVolume(-0.05f);
        //}

        // State specific input.
        if (currentState is MenuState)
        {
            if (keyboard.IsKeyDown(Keys.Enter) && !previousKeyboard.IsKeyDown(Keys.Enter))
            {
                NewGame();
            }
        }

        if (currentState is GameState)
        {
            // Check keybord input.
            //
            // Jump.
            if (keyboard.IsKeyDown(Keys.Space))
            {
                //GameState.Player.Jump(GameState.Player.JumpVelocity);
            }

            // Boost.
            if (keyboard.IsKeyDown(Keys.LeftShift))
            {
                //GameState.Player.Jump(GameState.Player.JumpVelocity * 2);
            }

            // Pause.
            if (keyboard.IsKeyDown(Keys.Escape) || keyboard.IsKeyDown(Keys.P))
            {
                //game.ChangeState(new PauseState(game, graphicsDevice, content));
            }

            // Game over.
            if (keyboard.IsKeyDown(Keys.Q))
            {
                //game.ChangeState(new GameOverState(game, graphicsDevice, content));
            }

            // Check touch input.
            //if (touchState.AnyTouch())
            //{
            //    int x = (int)touchState.GetPosition().X;
            //    int y = (int)touchState.GetPosition().Y;

            //    //int xBoost = Art.BoostTexture.Width;
            //    //int yBoost = Game1.ScreenHeight - (Art.BoostTexture.Height * 2);

            //    // Boost.
            //    //if (x < xBoost && y > yBoost)
            //    //{
            //    //    GameState.Player.Jump(GameState.Player.JumpVelocity * 2);
            //    //    return;
            //    //}

            //    // Jump.
            //    //GameState.Player.Jump(GameState.Player.JumpVelocity);
            //}
        }

        //if (currentState is PauseState)
        //{
        //    // Continue game.
        //    if (keyboard.IsKeyDown(Keys.Space) || keyboard.IsKeyDown(Keys.Enter))
        //    {
        //        ContinueGame();
        //    }
        //}

        //if (currentState is GameOverState)
        //{
        //    // New game.
        //    if (keyboard.IsKeyDown(Keys.Enter))
        //    {
        //        NewGame();
        //    }
        //}

        //if (currentState is SkinsState)
        //{
        //    // Left.
        //    if (keyboard.IsKeyDown(Keys.Left) && !previousKeyboard.IsKeyDown(Keys.Left))
        //    {
        //        SkinsState.LeftArrowKey(-1);
        //        Util.SaveSkinData();
        //    }

        //    // Right.
        //    if (keyboard.IsKeyDown(Keys.Right) && !previousKeyboard.IsKeyDown(Keys.Right))
        //    {
        //        SkinsState.RightArrowKey();
        //        Util.SaveSkinData();
        //    }

        //    // Enter.
        //    if (keyboard.IsKeyDown(Keys.Enter) && !previousKeyboard.IsKeyDown(Keys.Enter))
        //    {
        //        MainMenu();
        //    }
        //}

        //if (currentState is TrophyState)
        //{
        //    // Enter.
        //    if (keyboard.IsKeyDown(Keys.Enter) && !previousKeyboard.IsKeyUp(Keys.Enter))
        //    {
        //        MainMenu();
        //    }
        //}
    }

    // Checks if a key was just pressed down
    public static bool WasKeyPressed(Keys key)
    {
        return previousKeyboard.IsKeyUp(key) && keyboard.IsKeyDown(key);
    }

    public static Vector2 GetMovementDirection()
    {
        Vector2 direction = new Vector2();
        //Vector2 direction = gamepadState.ThumbSticks.Left;
        direction.Y *= -1; // invert the y-axis
        if (keyboard.IsKeyDown(Keys.A))
        {
            direction.X -= 1;
        }

        if (keyboard.IsKeyDown(Keys.D))
            direction.X += 1;
        if (keyboard.IsKeyDown(Keys.W))
            direction.Y -= 1;
        if (keyboard.IsKeyDown(Keys.S))
            direction.Y += 1;
        // Clamp the length of the vector to a maximum of 1.
        //if (direction.LengthSquared() > 1)
        //    direction.Normalize();
        Game1.Camera.Pos += direction * Player.Speed;
        return direction;
    }

    public static void MainMenu()
    {
        game.ChangeState(new MenuState(game, graphicsDevice, content));
    }

    public static void NewGame()
    {
        Game1.GameState = null;
        //GameState.Score = 0;
        //GameState.Coins = 0;
        Game1.Instance.ChangeState(new GameState(game, graphicsDevice, content));
    }

    public static void ContinueGame()
    {
        Game1.Instance.ChangeState(Game1.GameState);
        Game1.Instance.IsMouseVisible = false;
        //Background.SetAlpha(100);
    }
}
