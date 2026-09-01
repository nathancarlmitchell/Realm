namespace Realm.States
{
    public static class StateManager
    {
        public static void EnterPortal()
        {
            Sound.Play(Sound.EnterRealm, 0.35f);

            Game1.Instance.ChangeState(
                new RealmState(Game1.Instance, Game1.Instance.GraphicsDevice, Game1.Instance.Content)
            );
        }

        // Entered via any BossDestination portal (e.g. the one SpriteGod
        // drops on death — see Enemy.WasShot()) — bossDestination says
        // which boss the resulting arena should spawn.
        public static void EnterBossRealm(Portal.Destination.BossDestination bossDestination)
        {
            Sound.Play(Sound.EnterRealm, 0.35f);

            Game1.Instance.ChangeState(
                new BossRealmState(
                    Game1.Instance,
                    Game1.Instance.GraphicsDevice,
                    Game1.Instance.Content,
                    bossDestination
                )
            );
        }

        public static void Nexus()
        {
            EntityManager.Reset();

            Util.SavePlayerData();
            Util.SaveInventoryData();
            Util.SaveBankData();
            Util.SaveFameData();

            Game1.Instance.ChangeState(
                new NexusState(
                    Game1.Instance,
                    Game1.Instance.GraphicsDevice,
                    Game1.Instance.Content
                )
            );
        }

        public static void MainMenu()
        {
            EntityManager.Reset();

            Util.SavePlayerData();
            Util.SaveInventoryData();
            Util.SaveBankData();
            Util.SaveFameData();

            Game1.Instance.ChangeState(
                new MenuState(Game1.Instance, Game1.Instance.GraphicsDevice, Game1.Instance.Content)
            );
        }

        // Renamed from SelectClass() — lands on the character-slots list
        // now, not straight on a class-picker (that's CharacterCreationState,
        // reached only by clicking an empty slot from here). Mirrors
        // Nexus()/MainMenu() — this is also a "leaving gameplay" exit point
        // (the in-world Character Select portal, and the main menu's
        // Character Select button) — except it deliberately does NOT call
        // Util.SavePlayerData()/SaveInventoryData(): if the player just
        // deleted their own live character from this same screen,
        // Player.Instance is a throwaway with a freshly-generated ID (see
        // CharacterSlotsState's delete handler), and saving it here would
        // write a small, harmless-but-untracked orphan save pair for that
        // throwaway. The account-wide files still need saving, same
        // reasoning as every other exit point here.
        public static void CharacterSlots()
        {
            EntityManager.Reset();

            Util.SaveBankData();
            Util.SaveFameData();
            Util.SaveCharacterSlotsData();
            Util.SaveClassRecordsData();

            Game1.Instance.ChangeState(
                new CharacterSlotsState(
                    Game1.Instance,
                    Game1.Instance.GraphicsDevice,
                    Game1.Instance.Content
                )
            );
        }

        // Used by the main menu's Nexus button and its Enter-key shortcut. Neither
        // has a character in mind of its own (unlike
        // CharacterCreationState.SelectCharacter/CharacterSlotsState's row-click,
        // or GameOver's New Game button, which call NewGame() directly), so this is
        // the one place that needs to ask "has anything ever actually been played?"
        // first — if not, Player.Instance is only ever a boot-time default (see
        // Util.DetermineLastPlayedCharacter), and jumping straight into gameplay with
        // it would silently start a Wizard nobody chose.
        public static void EnterNexus()
        {
            if (Util.AnyCharacterHasBeenPlayed())
                NewGame();
            else
                CharacterSlots();
        }

        public static void NewGame()
        {
            // The single choke point every "start playing" entry reaches
            // (Character Creation, EnterNexus above, and GameOver's New Game
            // button) — persisting HasBeenPlayed here, rather than at each
            // call site, guarantees a character shows as deletable in the
            // slots list as soon as it's actually entered, even if nothing
            // else ever saves again.
            Player.Instance.HasBeenPlayed = true;
            Util.SavePlayerData();
            Util.SaveInventoryData();
            Util.SaveBankData();
            Util.SaveFameData();

            Game1.Instance.ChangeState(
                new NexusState(
                    Game1.Instance,
                    Game1.Instance.GraphicsDevice,
                    Game1.Instance.Content
                )
            );
        }

        public static void GameOver()
        {
            Game1.Instance.ChangeState(
                new GameOverState(
                    Game1.Instance,
                    Game1.Instance.GraphicsDevice,
                    Game1.Instance.Content
                )
            );
        }

        public static void ExitGame()
        {
            Game1.Instance.Exit();
        }

        // Opens the key-bindings screen from wherever the player currently
        // is — Main Menu or mid-game — and remembers that exact state
        // object (not a fixed destination like every other method here) so
        // Settings' own Back button can return to it directly via
        // ChangeState() rather than re-navigating/reconstructing it.
        public static void OpenSettings(State returnState)
        {
            Game1.Instance.ChangeState(
                new SettingsState(
                    Game1.Instance,
                    Game1.Instance.GraphicsDevice,
                    Game1.Instance.Content,
                    returnState
                )
            );
        }
    }
}
