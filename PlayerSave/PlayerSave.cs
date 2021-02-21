using System;
using MiNET.Plugins;
using MiNET.Plugins.Attributes;

namespace PlayerSave
{

    [Plugin(PluginName = "PlayerSave", Description = "PlayerSave", PluginVersion = "1.25", Author = "wsj7178, bl_3an_dev")]
    public class PlayerSave : Plugin
    {

        protected override void OnEnable()
        {
            Console.WriteLine("PlayerSave 플러그인이 활성화되었습니다.");
            Context.Server.PlayerFactory.PlayerCreated += PlayerFactory_PlayerCreated;
        }

        public override void OnDisable()
        {
            Console.WriteLine("PlayerSave 플러그인이 비활성화되었습니다.");
        }

        private void PlayerFactory_PlayerCreated(object sender, MiNET.PlayerEventArgs e)
        {
            e.Player.PlayerJoin += Player_PlayerJoin;
            e.Player.PlayerLeave += Player_PlayerLeave;
        }

        private void Player_PlayerLeave(object sender, MiNET.PlayerEventArgs e)
        {
            e.Player.Save(true);
        }

        private void Player_PlayerJoin(object sender, MiNET.PlayerEventArgs e)
        {
            e.Player.Load();
        }

    }

}