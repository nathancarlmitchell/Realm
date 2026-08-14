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

    public static Rectangle MouseBounds
    {
        get { return new Rectangle((int)Input.MousePosition.X, (int)Input.MousePosition.Y, 1, 1); }
    }

    private static KeyboardState keyboard,
        previousKeyboard;

    public static void Update(State currentState)
    {
        previousKeyboard = keyboard;
        keyboard = Keyboard.GetState();

        previousMouse = mouse;
        mouse = Mouse.GetState();

        // Universal input.
        //
        // Toggle Mute
        if (WasKeyPressed(Keys.M))
        {
            Sound.ToggleMute();
        }

        // Volume up
        if (WasKeyPressed(Keys.PageUp))
        {
            Sound.SongVolume(0.05f);
        }

        // Volume down
        if (WasKeyPressed(Keys.PageDown))
        {
            Sound.SongVolume(-0.05f);
        }

        // Toggle debug HUD (Overlay.DrawDebug).
        if (WasKeyPressed(Keys.F3))
        {
            Game1._Debug = !Game1._Debug;
        }

        // Return to main menu. Only from the hub (NexusState) — not from the
        // active dungeon (RealmState).
        if (WasKeyPressed(Keys.Escape) && currentState is NexusState)
        {
            StateManager.MainMenu();
        }

        // State specific input.
        if (currentState is MenuState)
        {
            if (keyboard.IsKeyDown(Keys.Enter) && !previousKeyboard.IsKeyDown(Keys.Enter))
            {
                StateManager.EnterNexus();
            }
        }

        // Ability. Usable in the Nexus as well as the dungeon (unlike potions/
        // leveling below), so the player can try it out or just mess around
        // without needing to be in an active RealmState.
        if (
            (currentState is RealmState || currentState is NexusState)
            && WasKeyPressed(Keys.Space)
        )
        {
            Player.Instance.UseAbility();
        }

        if (currentState is RealmState)
        {
            // Use health potion.
            if (WasKeyPressed(Keys.Q))
            {
                Player.Instance.Inventory.UsePotion("Health Potion");
            }

            // Use mana potion.
            if (WasKeyPressed(Keys.F))
            {
                Player.Instance.Inventory.UsePotion("Mana Potion");
            }

            // Level up. Capped like normal leveling — past 20, EnemySpawner's
            // spawn-chance formula (1500 - Level * 50) goes negative and crashes.
            if (WasKeyPressed(Keys.Add) && Player.Instance.Level < 20)
            {
                Player.Instance.LevelUp();
            }

            // Level down. Floored at 1 so it can't push Level negative, which would
            // make LevelUp()'s ExperienceNextLevel go negative and auto-trigger
            // itself every frame until Level recovered.
            if (WasKeyPressed(Keys.Subtract) && Player.Instance.Level > 1)
            {
                Player.Instance.Level -= 2;
                Player.Instance.LevelUp();
            }

            // Return to Nexus.
            if (WasKeyPressed(Keys.E))
            {
                StateManager.Nexus();
            }
        }

        if (currentState is GameOverState)
        {
            // New game. See GameOverState.NewGameButton_Click for why this is
            // EnterNexus() rather than a direct NewGame().
            if (keyboard.IsKeyDown(Keys.Enter))
            {
                StateManager.EnterNexus();
            }
        }

    }

    // Checks if a key was just pressed down
    public static bool WasKeyPressed(Keys key)
    {
        return previousKeyboard.IsKeyUp(key) && keyboard.IsKeyDown(key);
    }

    public static Vector2 GetMovementDirection()
    {
        Vector2 direction = new Vector2();
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

    public static bool GetMouseClick()
    {
        return previousMouse.LeftButton == ButtonState.Released
            && mouse.LeftButton == ButtonState.Pressed;
    }

    public static bool MousePressed()
    {
        return mouse.LeftButton == ButtonState.Pressed;
    }

    public static bool MouseReleased()
    {
        return previousMouse.LeftButton == ButtonState.Pressed
            && mouse.LeftButton == ButtonState.Released;
    }
}
