using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Threading;
using System.Text.Json;
using System.Text;
using System.Diagnostics.Tracing;
using System.Drawing.Printing;
using IL.Terraria.WorldBuilding;
using System.Security.Policy;

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

        public Bot(string token)
        {
            this.token = token;
            mainSocketCancel = new CancellationTokenSource();
        }

        public async Task start()
        {
            restarting = false;
            this.socket = new ClientWebSocket();
            Uri uri = new Uri("wss://gateway.discord.gg/?v=10&encoding=json");
            await this.socket.ConnectAsync(uri, mainSocketCancel.Token);
            byte[] buffer = new byte[262144];
            while (true)
            {
                CancellationTokenSource recieveSource = new CancellationTokenSource();
                ArraySegment<byte> data = new ArraySegment<byte>(buffer);
                try
                {
                    WebSocketReceiveResult result = await this.socket.ReceiveAsync(data, recieveSource.Token);
                    Console.WriteLine(Encoding.UTF8.GetString(new ArraySegment<byte>(buffer, 0, result.Count)));
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
            Console.WriteLine(data);
            byte[] rawData = Encoding.UTF8.GetBytes(data);
            CancellationTokenSource sendSource = new CancellationTokenSource();
            await socket.SendAsync(new ArraySegment<byte>(rawData), WebSocketMessageType.Binary, true, sendSource.Token);
        }


        private async Task onMessage(JsonElement data)
        {
            switch(data.GetProperty("op").GetUInt16())
            {
                case 0:
                    switch(data.GetProperty("t").GetString())
                    {
                        case "MESSAGE_CREATE":
                            
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
                    identity.intents = 0 + 1 << 9;
                    identityData.d = identity;
                    identityData.op = 2;
                    Console.WriteLine(JsonSerializer.Serialize(identityData));
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
