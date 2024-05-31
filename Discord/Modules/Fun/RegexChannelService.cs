using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SolarisBot.Discord.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolarisBot.Discord.Modules.Fun
{
    [Module("fun/regex"), AutoLoadService]
    internal class RegexChannelService : IHostedService
    {
        private readonly ILogger<RegexChannelService> _logger;
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _provider;

        public RegexChannelService(ILogger<RegexChannelService> logger, DiscordSocketClient client, IServiceProvider provider)
        {
            _logger = logger;
            _client = client;
            _provider = provider;
        }

        public Task StartAsync(CancellationToken cancellationToken) //todo: [FEATURE] On edit?
        {
            _client.MessageReceived += CheckForRegexAsync;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _client.MessageReceived -= CheckForRegexAsync;
            return Task.CompletedTask;
        }

        private Task CheckForRegexAsync(SocketMessage arg)
        {
            return Task.CompletedTask;
        }
    }
}
