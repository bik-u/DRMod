using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace DiscRelay
{
    public class RelayConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        public static RelayConfig Instance => ModContent.GetInstance<RelayConfig>();

        [Label("Bot Token")]
        [Tooltip("Token for the main bot.")]
        [DefaultValue("")]
        public string botToken;

        [Label("Main channel id")]
        [Tooltip("Main channel id for bot to relay")]
        [DefaultValue("")]
        public string channelId;


        [JsonIgnore]
        public static string BotToken => Instance.botToken;
        [JsonIgnore]
        public static string ChannelId => Instance.channelId;
        

    }
}
