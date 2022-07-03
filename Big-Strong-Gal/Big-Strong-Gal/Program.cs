using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BigStrongGal
{
    class Program
    {
        //[TODO] Make this secure
        private static string APP_TOKEN = "OTU3MTI1NTg0OTYwNDg3NDU0.GcbjB2.0SBdh8jkaDBBDSfmsAWpVqPeCGPqXw9QWOE0go";
        private static ulong APP_GUILD_ID = 957125584960487454;

        private DiscordSocketClient mClient;
        private CommandService mCommands;
        private IServiceProvider mServices;

        static void Main(string[] args) => new Program().RunBotAsync().GetAwaiter().GetResult();

        public async Task RunBotAsync()
        {
            mClient = new DiscordSocketClient();
            mCommands = new CommandService();

            mServices = new ServiceCollection()
                .AddSingleton(mClient)
                .AddSingleton(mCommands)
                .BuildServiceProvider();

            mClient.Log += clientLog;

            mClient.UserVoiceStateUpdated += (user, before, after) => {
                ulong general_channelID = 690459268742119438; //[TODO] don't make this hard coded
                IMessageChannel chnl = mClient.GetChannel(general_channelID) as IMessageChannel;
                string message = $"{user} - {before.VoiceChannel?.Name ?? "null"} -> {after.VoiceChannel?.Name ?? "null"}";
                if (before.VoiceChannel != null && after.VoiceChannel == null)
                {
                    message = $"{user} has left {before.VoiceChannel.Name}";
                }
                else if (before.VoiceChannel == null && after.VoiceChannel != null)
                {
                    message = $"{user} has entered {after.VoiceChannel.Name}";
                }
                chnl.SendMessageAsync(message);
                return Task.CompletedTask;
            };

            await RegisterCommandsAsync();
            await mClient.LoginAsync(TokenType.Bot, APP_TOKEN);
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
            if(message.HasStringPrefix("!", ref argPos))
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