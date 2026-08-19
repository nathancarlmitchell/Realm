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

        // Entered via the portal SpriteGod drops on death (Enemy.WasShot()).
        public static void EnterBossRealm()
        {
            Sound.Play(Sound.EnterRealm, 0.35f);

            Game1.Instance.ChangeState(
                new BossRealmState(
                    Game1.Instance,
                    Game1.Instance.GraphicsDevice,
                    Game1.Instance.Content
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

        public static void SelectClass()
        {
            // Mirrors Nexus()/MainMenu() — this is also a "leaving gameplay" exit
            // point (the in-world Character Select portal, and the main menu's
            // Character Select button), so it needs the same save as those or
            // whatever was last changed in-memory (e.g. an equipment drag) is
            // silently discarded the next time this class is loaded, while the
            // other exit points correctly persist it — a real inconsistency, not
            // just a missed no-op.
            EntityManager.Reset();

            Util.SavePlayerData();
            Util.SaveInventoryData();
            Util.SaveBankData();
            Util.SaveFameData();

            Game1.Instance.ChangeState(
                new CharacterSelectState(
                    Game1.Instance,
                    Game1.Instance.GraphicsDevice,
                    Game1.Instance.Content
                )
            );
        }

        // Used by the main menu's Nexus button and its Enter-key shortcut. Neither
        // has a class in mind of its own (unlike CharacterSelectState.SelectCharacter
        // or GameOver's New Game button, which call NewGame() directly), so this is
        // the one place that needs to ask "has anything ever actually been played?"
        // first — if not, Player.Instance is only ever a boot-time default (see
        // Util.DetermineLastPlayedClass), and jumping straight into gameplay with it
        // would silently start a Wizard nobody chose.
        public static void EnterNexus()
        {
            if (Util.AnyCharacterHasBeenPlayed())
                NewGame();
            else
                SelectClass();
        }

        public static void NewGame()
        {
            // The single choke point every "start playing" entry reaches
            // (Character Select, EnterNexus above, and GameOver's New Game
            // button) — persisting HasBeenPlayed here, rather than at each
            // call site, guarantees Character Select's Delete link appears
            // as soon as a character is actually entered, even if nothing
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
