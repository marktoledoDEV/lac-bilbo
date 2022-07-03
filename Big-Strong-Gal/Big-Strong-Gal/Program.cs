using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BigStrongGal
{
    class Program
    {
        private DiscordSocketClient mClient;
        private CommandService mCommands;
        private IServiceProvider mServices;
        private IConfigurationRoot mConfiguration { get; set; }

        static void Main(string[] args) => new Program().RunBotAsync().GetAwaiter().GetResult();

        public async Task RunBotAsync()
        {
            string token = Environment.GetEnvironmentVariable("BIG_STRONG_GAL_TOKEN");
            mClient = new DiscordSocketClient();
            mCommands = new CommandService();
            mServices = new ServiceCollection()
                .AddSingleton(mClient)
                .AddSingleton(mCommands)
                .BuildServiceProvider();

            mClient.Log += clientLog;

            mClient.UserVoiceStateUpdated += (user, before, after) =>
            {
                ulong general_channelID = 690459268742119438; //[TODO] don't make this hard coded
                IMessageChannel chnl = mClient.GetChannel(general_channelID) as IMessageChannel;
                string message = "";
                if (before.VoiceChannel != null && after.VoiceChannel == null)
                {
                    message = $"{user} has left {before.VoiceChannel.Name}";
                }
                else if (before.VoiceChannel == null && after.VoiceChannel != null)
                {
                    message = $"{user} has entered {after.VoiceChannel.Name}";
                }
                else
                {
                    message = $"{user} - {before.VoiceChannel?.Name ?? "null"} -> {after.VoiceChannel?.Name ?? "null"}";
                }

                //[TODO] This is the part where we send the message to telegram
                chnl.SendMessageAsync(message);
                return Task.CompletedTask;
            };

            await RegisterCommandsAsync();
            await mClient.LoginAsync(TokenType.Bot, token);
            await mClient.StartAsync();
            await Task.Delay(-1);
        }

        private Task clientLog(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }

        public async Task RegisterCommandsAsync()
        {
            mClient.MessageReceived += HandleCommandAsync;
            await mCommands.AddModulesAsync(Assembly.GetEntryAssembly(), mServices);
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            var message = arg as SocketUserMessage;
            var context = new SocketCommandContext(mClient, message);
            if (message.Author.IsBot)
            {
                return;
            }

            int argPos = 0;
            if (message.HasStringPrefix("!", ref argPos))
            {
                var result = await mCommands.ExecuteAsync(context, argPos, mServices);
                if (!result.IsSuccess)
                {
                    Console.WriteLine(result.ErrorReason);
                }
            }
        }
    }
}