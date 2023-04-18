using Terraria.ModLoader;
using System.Threading;
using System.Threading.Tasks;
using System;
using Terraria.ModLoader.Config;

namespace DiscRelay
{
    public class BotThread
    {
        private Bot bot;
        public void Start() => new BotThread().StartAsync();
        public async Task StartAsync()
        {
            bot = new Bot(RelayConfig.BotToken);
            await bot.start();
        }
    }

	public class DiscRelay : Mod
	{
        private BotThread b;
        private Thread bthread;

        public override void Load()
        {
            base.Load();
            if (RelayConfig.BotToken == "" || RelayConfig.ChannelId == "")
            {
                Console.WriteLine("\nEdit config in here:");
                Console.WriteLine(ConfigManager.ModConfigPath);
                Console.Write('\n');
                return;
            }
            b = new BotThread();
            bthread = new Thread(b.Start);
            bthread.Start();

        }

        public override void PostSetupContent()
        {
            base.PostSetupContent();
            Console.WriteLine(RelayConfig.BotToken);
        }
    }
}