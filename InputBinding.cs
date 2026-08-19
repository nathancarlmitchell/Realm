using System;
using Microsoft.Xna.Framework.Input;

namespace Realm
{
    public enum MouseButton
    {
        Left,
        Right,
        Middle,
    }

    // One binding, either a keyboard key or a mouse button — lets
    // KeyBindings store either kind under the same Action without every
    // caller needing two parallel dictionaries. Left is deliberately never
    // offered as an assignable mouse option (see Input.GetAnyNewInputBinding)
    // since it already has a fixed, unavoidable meaning: basic attack fire
    // and every UI click in the game.
    public readonly struct InputBinding : IEquatable<InputBinding>
    {
        public enum InputKind
        {
            Keyboard,
            Mouse,
        }

        public InputKind Kind { get; }
        public Keys Key { get; }
        public MouseButton Button { get; }

        private InputBinding(InputKind kind, Keys key, MouseButton button)
        {
            Kind = kind;
            Key = key;
            Button = button;
        }

        public static InputBinding FromKey(Keys key) => new(InputKind.Keyboard, key, default);

        public static InputBinding FromMouseButton(MouseButton button) =>
            new(InputKind.Mouse, Keys.None, button);

        public override string ToString() =>
            Kind == InputKind.Keyboard ? Key.ToString() : $"Mouse {Button}";

        // Flat "Key:W" / "Mouse:Right" string — keeps Data/KeyBindingsData.cs
        // as one plain string property per action, matching the rest of the
        // codebase's flat-DTO save style instead of a nested JSON object.
        public string Serialize() =>
            Kind == InputKind.Keyboard ? $"Key:{Key}" : $"Mouse:{Button}";

        public static InputBinding Deserialize(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                string[] parts = value.Split(':', 2);
                if (parts.Length == 2)
                {
                    if (parts[0] == "Mouse" && Enum.TryParse(parts[1], out MouseButton button))
                        return FromMouseButton(button);
                    if (parts[0] == "Key" && Enum.TryParse(parts[1], out Keys key))
                        return FromKey(key);
                }
            }

            // Malformed/missing data falls back to an inert binding rather
            // than throwing — same "just don't apply" tolerance every other
            // Load*() in Util.cs already has for corrupt save data.
            return FromKey(Keys.None);
        }

        public bool Equals(InputBinding other) =>
            Kind == other.Kind && Key == other.Key && Button == other.Button;

        public override bool Equals(object obj) => obj is InputBinding other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Kind, Key, Button);

        public static bool operator ==(InputBinding a, InputBinding b) => a.Equals(b);

        public static bool operator !=(InputBinding a, InputBinding b) => !a.Equals(b);
    }
}
