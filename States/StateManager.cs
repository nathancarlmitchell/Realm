using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace Realm.States
{
    public static class StateManager
    {
        public static void EnterPortal()
        {
            Sound.Play(Sound.EnterRealm, 0.35f);

            Game1.Instance.ChangeState(
                new GameState(Game1.Instance, Game1.Instance.GraphicsDevice, Game1.Instance.Content)
            );
        }

        public static void Nexus()
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

        public static void MainMenu()
        {
            Game1.Instance.ChangeState(
                new MenuState(Game1.Instance, Game1.Instance.GraphicsDevice, Game1.Instance.Content)
            );
        }

        public static void NewGame()
        {
            Game1.Instance.ChangeState(
                new RealmState(
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
}
