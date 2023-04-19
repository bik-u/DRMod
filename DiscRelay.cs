using Terraria.ModLoader;
using System.Threading;
using System.Threading.Tasks;
using System;
using Terraria.ModLoader.Config;
using Terraria.Chat;
using Terraria.Localization;
using Microsoft.Xna.Framework;
using System.Drawing.Printing;
using System.Net.Http;
using System.Text.Json;
using System.Net.Http.Json;

namespace DiscRelay
{

    public class Webhook
    {
        public string name { get; set; }
    }

    public class DiscMessage
    {
        public string content { get; set; }
    }

    public class DiscRelay : Mod
    {

        private Bot bot;
        private Thread bthread;
        public const string API = "https://discord.com/api/";
        private HttpClient client;

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
            Console.WriteLine(ConfigManager.ServerModConfigPath);
            if (RelayConfig.BotToken == "" || RelayConfig.ChannelId == "")
            {
                Console.WriteLine("\nEdit config in here:");
                Console.WriteLine(ConfigManager.ModConfigPath);
                Console.Write('\n');
                return;
            }

            bthread = new Thread(Start);
            bthread.Start();

            client = new HttpClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("Authorization", $"Bot {RelayConfig.BotToken}");

            On.Terraria.Chat.ChatHelper.BroadcastChatMessage += (orig, text, color, player) =>
            {
                orig(text, color, player);
                // Ignore if player is -2 which is from discord
                if (player == -2)
                {
                    return;
                }
            };

            On.Terraria.Chat.ChatHelper.BroadcastChatMessageAs += async (orig, author, text, color, player) =>
            {
                orig(author, text, color, player);
                if (player == -2)
                {
                    return;

                }
                string name = Terraria.Main.player[author].name;
                string msg = text.ToString();
                if (name == null || name == "") {
                    name = Terraria.Main.worldName;

                }
                await SendMessage(name, text.ToString());
                


            };

        }

        async Task SendMessage(string name, string text)
        {
            // Find a webhook for user, if not create one and send message 
            String webhookUrl = $"{API}channels/{RelayConfig.ChannelId}/webhooks";
            try
            {
                string result = await client.GetStringAsync(webhookUrl);
                Console.WriteLine(result);
                JsonElement data = JsonSerializer.Deserialize<JsonElement>(result);
                string id = "";
                string token = "";
                bool hasName = false;

                for (int i = 0; i < data.GetArrayLength(); i++)
                {
                    JsonElement h = data[i];
                    if (h.GetProperty("name").GetString() == name)
                    {
                        hasName = true;
                        id = h.GetProperty("id").GetString();
                        token = h.GetProperty("token").GetString();
                        break;
                    }
                };

                if (!hasName)
                {
                    Webhook user = new Webhook();
                    user.name = name;
                    HttpResponseMessage createdResult = await client.PostAsJsonAsync(webhookUrl, user);
                    JsonElement webhookUser = JsonSerializer.Deserialize<JsonElement>(await createdResult.Content.ReadAsStringAsync());
                    id = webhookUser.GetProperty("id").GetString();
                    token = webhookUser.GetProperty("token").GetString();
                }

                string executeUrl = $"{API}webhooks/{id}/{token}";
                DiscMessage msg = new DiscMessage();
                msg.content = text;
                await client.PostAsJsonAsync(executeUrl, msg);

            }
            catch (HttpRequestException ex)
            {

            };
        }

        public override void PostSetupContent()
        {
            base.PostSetupContent();
        }

    }
  
}