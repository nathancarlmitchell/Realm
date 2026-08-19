using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace Realm
{
    // Every remappable action, plus the currently-bound input (a keyboard
    // key or a mouse button — see InputBinding) for each. Movement/ability/
    // interaction keys only — F3 (debug HUD), PageUp/PageDown (volume),
    // Add/Subtract (debug level), Escape, and Enter stay fixed system/menu
    // keys, not exposed here.
    public static class KeyBindings
    {
        public enum Action
        {
            MoveUp,
            MoveDown,
            MoveLeft,
            MoveRight,
            UseAbility,
            UseHealthPotion,
            UseManaPotion,
            ReturnToNexus,
            ToggleAutoFire,
            ToggleMute,
        }

        // The order the Settings screen lists them in.
        public static readonly Action[] AllActions =
        [
            Action.MoveUp,
            Action.MoveDown,
            Action.MoveLeft,
            Action.MoveRight,
            Action.UseAbility,
            Action.UseHealthPotion,
            Action.UseManaPotion,
            Action.ReturnToNexus,
            Action.ToggleAutoFire,
            Action.ToggleMute,
        ];

        public static string DisplayName(Action action) =>
            action switch
            {
                Action.MoveUp => "Move Up",
                Action.MoveDown => "Move Down",
                Action.MoveLeft => "Move Left",
                Action.MoveRight => "Move Right",
                Action.UseAbility => "Use Ability",
                Action.UseHealthPotion => "Health Potion",
                Action.UseManaPotion => "Mana Potion",
                Action.ReturnToNexus => "Return to Nexus",
                Action.ToggleAutoFire => "Toggle Auto-Fire",
                Action.ToggleMute => "Toggle Mute",
                _ => action.ToString(),
            };

        private static readonly Dictionary<Action, InputBinding> Defaults =
            new()
            {
                [Action.MoveUp] = InputBinding.FromKey(Keys.W),
                [Action.MoveDown] = InputBinding.FromKey(Keys.S),
                [Action.MoveLeft] = InputBinding.FromKey(Keys.A),
                [Action.MoveRight] = InputBinding.FromKey(Keys.D),
                [Action.UseAbility] = InputBinding.FromKey(Keys.Space),
                [Action.UseHealthPotion] = InputBinding.FromKey(Keys.Q),
                [Action.UseManaPotion] = InputBinding.FromKey(Keys.F),
                [Action.ReturnToNexus] = InputBinding.FromKey(Keys.E),
                [Action.ToggleAutoFire] = InputBinding.FromKey(Keys.C),
                [Action.ToggleMute] = InputBinding.FromKey(Keys.M),
            };

        private static Dictionary<Action, InputBinding> bindings = new(Defaults);

        public static InputBinding Get(Action action) => bindings[action];

        // Returns the other action that used to hold `binding`, if any — the
        // caller (SettingsState) swaps that action onto the binding being
        // freed up, so two actions never end up sharing one key/button.
        public static Action? FindConflict(Action action, InputBinding binding)
        {
            foreach (var kvp in bindings)
            {
                if (kvp.Key != action && kvp.Value == binding)
                    return kvp.Key;
            }
            return null;
        }

        public static void Set(Action action, InputBinding binding) => bindings[action] = binding;

        public static void ResetToDefaults() => bindings = new(Defaults);

        public static Data.KeyBindingsData ToData() =>
            new()
            {
                MoveUp = bindings[Action.MoveUp].Serialize(),
                MoveDown = bindings[Action.MoveDown].Serialize(),
                MoveLeft = bindings[Action.MoveLeft].Serialize(),
                MoveRight = bindings[Action.MoveRight].Serialize(),
                UseAbility = bindings[Action.UseAbility].Serialize(),
                UseHealthPotion = bindings[Action.UseHealthPotion].Serialize(),
                UseManaPotion = bindings[Action.UseManaPotion].Serialize(),
                ReturnToNexus = bindings[Action.ReturnToNexus].Serialize(),
                ToggleAutoFire = bindings[Action.ToggleAutoFire].Serialize(),
                ToggleMute = bindings[Action.ToggleMute].Serialize(),
            };

        public static void FromData(Data.KeyBindingsData data)
        {
            if (data == null)
                return;

            bindings[Action.MoveUp] = InputBinding.Deserialize(data.MoveUp);
            bindings[Action.MoveDown] = InputBinding.Deserialize(data.MoveDown);
            bindings[Action.MoveLeft] = InputBinding.Deserialize(data.MoveLeft);
            bindings[Action.MoveRight] = InputBinding.Deserialize(data.MoveRight);
            bindings[Action.UseAbility] = InputBinding.Deserialize(data.UseAbility);
            bindings[Action.UseHealthPotion] = InputBinding.Deserialize(data.UseHealthPotion);
            bindings[Action.UseManaPotion] = InputBinding.Deserialize(data.UseManaPotion);
            bindings[Action.ReturnToNexus] = InputBinding.Deserialize(data.ReturnToNexus);
            bindings[Action.ToggleAutoFire] = InputBinding.Deserialize(data.ToggleAutoFire);
            bindings[Action.ToggleMute] = InputBinding.Deserialize(data.ToggleMute);
        }
    }
}
