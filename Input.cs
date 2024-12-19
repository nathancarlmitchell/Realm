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

    public static MouseState mouse,
        previousMouse;
    public static Vector2 MousePosition
    {
        get { return new Vector2(mouse.X, mouse.Y); }
    }

    private static KeyboardState keyboard,
        previousKeyboard;

    public static void Update(State currentState)
    {
        previousKeyboard = keyboard;
        keyboard = Keyboard.GetState();

        previousMouse = mouse;
        mouse = Mouse.GetState();

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

        float zoomIncrement = 0.01f;

        if (currentState is GameState)
        {
            // Check keybord input.
            //

            // Adjust zoom if the mouse wheel has moved
            //if (mouse.ScrollWheelValue > previousMouse.ScrollWheelValue)
            //    Game1.Camera.Zoom += zoomIncrement;
            //else if (mouse.ScrollWheelValue < previousMouse.ScrollWheelValue)
            //    Game1.Camera.Zoom -= zoomIncrement;

            // Use health potion.
            if (WasKeyPressed(Keys.Q))
            {
                Player.Instance.Inventory.RemoveItem("HealthPotion");
                // Move to
                //HealthPotion.Use();
            }

            // Use mana potion.
            if (WasKeyPressed(Keys.F))
            {
                Player.Instance.Inventory.RemoveItem("ManaPotion");
                // Move to
                //HealthPotion.Use();
            }

            // Ability.
            if (WasKeyPressed(Keys.Space))
            {
                Player.UseAbility();
            }

            // Level up.
            if (WasKeyPressed(Keys.Add))
            {
                Player.LevelUp();
            }

            // Level down.
            if (WasKeyPressed(Keys.Subtract))
            {
                Player.Level -= 2;
                Player.LevelUp();
            }

            // Return to Nexus.
            if (WasKeyPressed(Keys.E))
            {
                EntityManager.Reset();
                Util.SavePlayerData();
                Game1.Instance.ChangeState(
                    new RealmState(
                        Game1.Instance,
                        Game1.Instance.GraphicsDevice,
                        Game1.Instance.Content
                    )
                );
            }

            // Pause.
            //if (keyboard.IsKeyDown(Keys.Escape) || keyboard.IsKeyDown(Keys.P))
            //{
            //    //game.ChangeState(new PauseState(game, graphicsDevice, content));
            //}

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
            direction.X -= 1;
        if (keyboard.IsKeyDown(Keys.D))
            direction.X += 1;
        if (keyboard.IsKeyDown(Keys.W))
            direction.Y -= 1;
        if (keyboard.IsKeyDown(Keys.S))
            direction.Y += 1;

        // Clamp the length of the vector to a maximum of 1.
        if (direction.LengthSquared() > 1)
            direction.Normalize();

        return direction;
    }

    public static Vector2 GetMouseAimDirection()
    {
        Vector2 direction = MousePosition - Player.Instance.Position;

        // Transform mouse input from view to world position
        Matrix inverse = Matrix.Invert(Game1.Camera.GetTransformation());
        direction = Vector2.Transform(direction, inverse);

        if (direction == Vector2.Zero)
            return Vector2.Zero;
        else
            return Vector2.Normalize(direction);
    }

    public static Vector2 GetMousePosition()
    {
        Vector2 position = MousePosition;

        // Transform mouse input from view to world position
        Matrix inverse = Matrix.Invert(Game1.Camera.GetTransformation());
        position = Vector2.Transform(position, inverse);

        return position;
    }

    public static void MainMenu()
    {
        game.ChangeState(new MenuState(game, graphicsDevice, content));
    }

    public static void NewGame()
    {
        Game1.Instance.ChangeState(new RealmState(game, graphicsDevice, content));
    }

    //public static void ContinueGame()
    //{
    //    Game1.Instance.ChangeState(Game1.GameState);
    //    Game1.Instance.IsMouseVisible = false;
    //}

    public static void ExitGame()
    {
        Game1.Instance.Exit();
    }
}
