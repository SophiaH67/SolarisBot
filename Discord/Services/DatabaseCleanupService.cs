using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SolarisBot.Database;
using SolarisBot.Discord.Common;
using SolarisBot.Discord.Common.Attributes;
using SolarisBot.Discord.Modules.Bridges;

namespace SolarisBot.Discord.Services
{
    /// <summary>
    /// Handles DatabaseCleanup for some discord events
    /// </summary>
    [AutoLoadService]
    internal sealed class DatabaseCleanupService : IHostedService
    {
        private readonly ILogger<DatabaseCleanupService> _logger;
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _provider;

        public DatabaseCleanupService(ILogger<DatabaseCleanupService> logger, DiscordSocketClient client, IServiceProvider provider) //todo: [REFACTOR] Remove as much as possible from here
        {
            _client = client;
            _provider = provider;
            _logger = logger;
        }

        #region Start / Stop
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _client.UserLeft += OnUserLeftHandleAsync;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _client.UserLeft -= OnUserLeftHandleAsync;
            return Task.CompletedTask;
        }
        #endregion

        #region Events - OnUserLeft
        /// <summary>
        /// Handles all OnUserLeft events
        /// </summary>
        private async Task OnUserLeftHandleAsync(SocketGuild guild, SocketUser user)
        {
            var dbCtx = _provider.GetRequiredService<DatabaseContext>();

            var changes = await OnUserLeftRemoveQuotesAsync(dbCtx, guild, user);

            if (!changes)
                return;

            _logger.LogDebug("Deleting references to user {user} in guild {guild} from DB", user.Log(), guild.Log());
            var (_, err) = await dbCtx.TrySaveChangesAsync();
            if (err is not null)
                _logger.LogError(err, "Failed to delete references to user {user} in guild {guild} from DB", user.Log(), guild.Log());
            else
                _logger.LogInformation("Deleted references to user {user} in guild {guild} from DB", user.Log(), guild.Log());
        }

        /// <summary>
        /// Deletes associated DbQuotes for left user
        /// </summary>
        private async Task<bool> OnUserLeftRemoveQuotesAsync(DatabaseContext dbCtx, SocketGuild guild, SocketUser user)
        {
            var quotes = await dbCtx.Quotes.ForGuild(guild.Id).Where(x => x.CreatorId == user.Id).ToArrayAsync();
            if (quotes.Length == 0)
                return false;

            _logger.LogDebug("Removing {quotes} related quotes for left user {user} in guild {guild}", quotes.Length, user.Log(), guild.Log());
            dbCtx.Quotes.RemoveRange(quotes);
            return true;
        }
        #endregion
    }
}
