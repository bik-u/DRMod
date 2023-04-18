using Terraria.ModLoader;
using System.Threading;
using System.Threading.Tasks;
using System;
using Terraria.ModLoader.Config;
using Terraria.Chat;
using Terraria.Localization;
using Microsoft.Xna.Framework;
using System.Drawing.Printing;

namespace DiscRelay
{

    public class DiscRelay : Mod
    {

        private Bot bot;
        private Thread bthread;
       

        public async Task StartAsync()
        {
            bot = new Bot(RelayConfig.BotToken, RelayConfig.ChannelId);
            await bot.start();
        }

        public void Start() => StartAsync();


        public override void Load()
        {
            base.Load();
            Console.WriteLine(this.Side);
            if (RelayConfig.BotToken == "" || RelayConfig.ChannelId == "")
            {
                Console.WriteLine("\nEdit config in here:");
                Console.WriteLine(ConfigManager.ModConfigPath);
                Console.Write('\n');
                return;
            }

            bthread = new Thread(Start);
            bthread.Start();
            On.Terraria.Chat.ChatHelper.BroadcastChatMessage += (orig, text, color, player) =>
            {
                orig(text, color, player);
                Console.WriteLine("Message wrote!");

            };

            On.Terraria.Chat.ChatHelper.BroadcastChatMessageAs += (orig, author, text, color, player) =>
            {
                orig(author, text, color, player);
            };

        }

        public override void PostSetupContent()
        {
            base.PostSetupContent();
        }

    }
  
}