using System;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Threading;
using System.Text.Json;
using System.Text;
using Terraria.Chat;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria.Localization;

namespace DiscRelay
{
    class Message
    {

    }

    class Heartbeat
    {
        public UInt64 d { get; set; }
        public int op {get; set;}
    }

    class Data<T>
    {
        public T d {get; set; }
        public int op { get; set; }
    }

    class IdentityProperties
    {
        public string os { get; set; }
        public string browser { get; set; }
        public string device { get; set; }
    }

    class Identity
    {
        public string token { get; set; }
        public IdentityProperties properties { get; set; }
        public int intents { get; set; }

    }

    public class Bot
    {
        private ClientWebSocket socket;
        private CancellationTokenSource mainSocketCancel;
        private Task heartTask;
        private UInt32 heartbeatInterval;
        private bool heartbeat_ack = false;
        private bool restarting = false;
        private UInt64 sequence;
        private string token;
        private string mainChannel;

        public Bot(string token, string mainChannel)
        {
            this.token = token;
            this.mainChannel = mainChannel;
            mainSocketCancel = new CancellationTokenSource();
        }

        public async Task start()
        {
            restarting = false;
            this.socket = new ClientWebSocket();
            Uri uri = new Uri("wss://gateway.discord.gg/?v=8&encoding=json");
            await this.socket.ConnectAsync(uri, mainSocketCancel.Token);
            byte[] buffer = new byte[262144];
            while (true)
            {
                CancellationTokenSource recieveSource = new CancellationTokenSource();
                ArraySegment<byte> data = new ArraySegment<byte>(buffer);
                try
                {
                    WebSocketReceiveResult result = await this.socket.ReceiveAsync(data, recieveSource.Token);
                    if (result.Count == 0)
                    {
                        // TODO: Add btr disconnection handling.
                        continue;
                    }
                    JsonElement dataJson = JsonSerializer.Deserialize<JsonElement>(new ArraySegment<byte>(buffer, 0, result.Count));
                    await onMessage(dataJson);
                    buffer = new byte[262144];
                } catch (InvalidOperationException ex)
                {
                    if (!restarting)
                    {
                        await restart();
                    }
                } 
            }
        }


        private async Task restart()
        {
            restarting = true;
            sequence = 0;
            heartbeat_ack = false;
            this.socket.Abort();
            await Task.Factory.StartNew(start);
        }

        public async Task socketSend(string data)
        {
            byte[] rawData = Encoding.UTF8.GetBytes(data);
            CancellationTokenSource sendSource = new CancellationTokenSource();
            try
            {
                await socket.SendAsync(new ArraySegment<byte>(rawData), WebSocketMessageType.Binary, true, sendSource.Token);
            }
            catch (InvalidOperationException ex)
            {

            }
        }


        private async Task onMessage(JsonElement data)
        {

            switch(data.GetProperty("op").GetUInt16())
            {
                case 0:
                    switch(data.GetProperty("t").GetString())
                    {
                        case "MESSAGE_CREATE":
                            if (data.GetProperty("d").GetProperty("channel_id").GetString() == mainChannel)
                            {
                                // Don't send if it's a webhook
                                JsonElement webhook;
                                if (data.GetProperty("d").TryGetProperty("webhook_id", out webhook))
                                {
                                    break;
                                }

                                JsonElement author = data.GetProperty("d").GetProperty("author").GetProperty("username");
                                JsonElement content = data.GetProperty("d").GetProperty("content");
                                string message = $"<{author.GetString()}> {content.GetString()}";
                                Terraria.Chat.ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(message), Color.White, -2);
                               
                            }
                            break;
                        default:
                            break;
                    }
                    break;
                case 10:
                    // TODO: Add proper disconnect handling (resuming etc.)
                    heartbeatInterval = data.GetProperty("d").GetProperty("heartbeat_interval").GetUInt32();
                    if (heartTask!= null && heartTask.IsCompleted)
                    {
                        heartTask.Dispose();
                        heartTask = null;
                    }
                    if (heartTask == null)
                    {
                        heartTask = await Task.Factory.StartNew(startHeart);
                    }
                    Data<Identity> identityData = new Data<Identity>();
                    Identity identity = new Identity();
                    IdentityProperties prop = new IdentityProperties();
                    prop.os = ".Net";
                    prop.browser = "custom";
                    prop.device = "custom";
                    identity.token = this.token;
                    identity.properties = prop;
                    identity.intents = 1 << 9;
                    identityData.d = identity;
                    identityData.op = 2;
                    await socketSend(JsonSerializer.Serialize(identityData));
                    break;
                case 11:
                    heartbeat_ack = true;
                    break;
                default:
                   break;
            }

        }

        private async Task startHeart()
        {
            while(true)
            {
                Heartbeat beat = new Heartbeat();
                beat.d = sequence;
                beat.op = 1;
                await socketSend(JsonSerializer.Serialize(beat));
                await Task.Delay((int) heartbeatInterval);
                if (!heartbeat_ack)
                {
                    await restart();
                    break;
                } else
                {
                    heartbeat_ack = false;
                }
            }

        }


    }
}
