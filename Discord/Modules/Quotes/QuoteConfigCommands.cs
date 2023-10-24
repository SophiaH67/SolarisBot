﻿using Discord.Interactions;
using Discord;
using Microsoft.Extensions.Logging;
using SolarisBot.Database;
using SolarisBot.Discord.Common;

namespace SolarisBot.Discord.Modules.Quotes
{
    [Module("quotes"), Group("cfg-quotes", "[MANAGE MESSAGES ONLY] Quotes config commands")]
    [RequireContext(ContextType.Guild), DefaultMemberPermissions(GuildPermission.ManageMessages), RequireUserPermission(GuildPermission.ManageMessages)]
    public sealed class QuoteConfigCommands : SolarisInteractionModuleBase
    {
        private readonly ILogger<QuoteConfigCommands> _logger;
        private readonly DatabaseContext _dbContext;
        internal QuoteConfigCommands(ILogger<QuoteConfigCommands> logger, DatabaseContext dbctx)
        {
            _dbContext = dbctx;
            _logger = logger;
        }

        [SlashCommand("config", "Enable quotes")]
        public async Task EnableQuotesAsync(bool enabled)
        {
            var guild = await _dbContext.GetOrCreateTrackedGuildAsync(Context.Guild.Id);

            guild.QuotesOn = enabled;

            _logger.LogDebug("{intTag} Setting quotes to {enabled} in guild {guild}", GetIntTag(), enabled, Context.Guild.Log());
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("{intTag} Set quotes to {enabled} in guild {guild}", GetIntTag(), enabled, Context.Guild.Log());
            await Interaction.ReplyAsync($"Quotes are currently **{(enabled ? "enabled" : "disabled")}**");
        }

        [SlashCommand("wipe", "Wipe quotes from guild, make sure to search")]
        public async Task WipeQuotesAsync(IUser? author = null, IUser? creator = null, string? content = null, [MinValue(0)] int offset = 0, [MinValue(0)] int limit = 0)
        {
            var quotes = await _dbContext.GetQuotesAsync(Context.Guild.Id, author: author, creator: creator, content: content, offset: offset, limit: limit);
            if (quotes.Length == 0)
            {
                await Interaction.ReplyErrorAsync(GenericError.NoResults);
                return;
            }

            _logger.LogDebug("{intTag} Wiping {quotes} from guild {guild}", GetIntTag(), quotes.Length, Context.Guild.Log());
            _dbContext.Quotes.RemoveRange(quotes);
            await _dbContext.SaveChangesAsync();
            _logger.LogDebug("{intTag} Wiped {quotes} from guild {guild}", GetIntTag(), quotes.Length, Context.Guild.Log());
            await Interaction.ReplyAsync($"Wiped **{quotes.Length}** quotes from database");
        }
    }
}
