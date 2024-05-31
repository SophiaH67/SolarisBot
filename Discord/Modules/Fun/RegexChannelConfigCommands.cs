﻿using Discord.Interactions;
using Discord;
using SolarisBot.Discord.Common;
using SolarisBot.Discord.Common.Attributes;
using Microsoft.Extensions.Logging;
using SolarisBot.Database;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using System;

namespace SolarisBot.Discord.Modules.Fun
{
    [Module("fun/regex"), Group("cfg-regex", "[MANAGE CHANNELS ONLY] Bridge config commands")]
    [RequireContext(ContextType.Guild), DefaultMemberPermissions(GuildPermission.ManageChannels), RequireUserPermission(GuildPermission.ManageChannels)] //todo: [FEATURE] Info commands
    public sealed class RegexChannelConfigCommands : SolarisInteractionModuleBase
    {
        private readonly ILogger<RegexChannelConfigCommands> _logger;
        private readonly DatabaseContext _dbContext;
        internal RegexChannelConfigCommands(ILogger<RegexChannelConfigCommands> logger, DatabaseContext dbctx)
        {
            _dbContext = dbctx;
            _logger = logger;
        }

        //todo: [FEATURE] service
        [SlashCommand("add", "Add a RegEx channel")]
        public async Task ConfigureRegexChannelAsync 
        (
            [Summary(description: "[Opt] Target channel")] IChannel? channel = null,
            [Summary(description: "[Opt] Regex to enforce (None to disable)")] string regex = "",
            [Summary(description: "[Opt] Role to apply as punishment")] IRole? punishmentRole = null,
            [Summary(description: "[Opt] Message to send on fail")] string punishmentMsg = "",
            [Summary(description: "[Opt] Delete fail message")] bool deleteMsg = false
        ) 
        {
            var thisChannel = channel ?? Context.Channel;
            if (string.IsNullOrWhiteSpace(regex))
            {
                var deleteChannel = await _dbContext.RegexChannels.FirstOrDefaultAsync(x => x.ChannelId == thisChannel.Id);
                if (deleteChannel is null)
                {
                    await Interaction.ReplyErrorAsync($"Failed to find a RegEx config for channel id {thisChannel.Id}");
                    return;
                }
                _dbContext.RegexChannels.Remove(deleteChannel);
                _logger.LogDebug("{intTag} Deleting regex {deleteRegex} for channel {channel} in guild {guild}", GetIntTag(), deleteChannel, thisChannel.Log(), Context.Guild.Log());
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("{intTag} Deleted regex {deleteRegex} for channel {channel} in guild {guild}", GetIntTag(), deleteChannel, thisChannel.Log(), Context.Guild.Log());
                await Interaction.ReplyAsync($"Removed RegEx channel **\"{deleteChannel.Regex}\"** with id **{deleteChannel.RegexChannelId}** for channel **<#{thisChannel.Id}>**");
                return;
            }

            try
            {
                _ = new Regex(regex);
            }
            catch
            {
                await Interaction.ReplyErrorAsync($"Failed to validate RegEx: {regex}");
                return;
            }

            var dbGuild = await _dbContext.GetOrCreateTrackedGuildAsync(Context.Guild.Id, x => x.Include(y => y.RegexChannels));
            var dbChannel = dbGuild.RegexChannels.FirstOrDefault(x => x.ChannelId == thisChannel.Id)
                ?? new DbRegexChannel() { GuildId = Context.Guild.Id, ChannelId = thisChannel.Id };

            dbChannel.Regex = regex;
            dbChannel.AppliedRoleId = punishmentRole?.Id ?? ulong.MinValue;
            dbChannel.PunishmentMessage = punishmentMsg;
            dbChannel.PunishmentDelete = deleteMsg;

            _dbContext.RegexChannels.Update(dbChannel);
            _logger.LogDebug("{intTag} Setting regex to rx={channelRegex}, role={punishmentRole}, msg={punishmentMsg}, del={delete} for channel {channel} in guild {guild}", GetIntTag(), dbChannel.Regex, dbChannel.AppliedRoleId, dbChannel.PunishmentMessage, dbChannel.PunishmentDelete, thisChannel.Log(), Context.Guild.Log());
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("{intTag} Set regex to rx={channelRegex}, role={punishmentRole}, msg={punishmentMsg}, del={delete} for channel {channel} in guild {guild}", GetIntTag(), dbChannel.Regex, dbChannel.AppliedRoleId, dbChannel.PunishmentMessage, dbChannel.PunishmentDelete, thisChannel.Log(), Context.Guild.Log());
            await Interaction.ReplyAsync($"RegEx for **<#{dbChannel.ChannelId}>** created\n\nRegex: **{regex}**\nRole: **{(punishmentRole is null ? "None" : $"{punishmentRole.Mention}")}**\nMessage: **{(string.IsNullOrWhiteSpace(dbChannel.PunishmentMessage) ? "None" : $"\"{dbChannel.PunishmentMessage}\"")}**\nDelete: **{(dbChannel.PunishmentDelete ? "Yes" : "No")}**");
        }

        [SlashCommand("list", "List all RegEx channels")]
        public async Task ListRegexChannelsAsync()
        {
            var regexChannels = await _dbContext.RegexChannels.ForGuild(Context.Guild.Id).ToArrayAsync();
            if (regexChannels.Length == 0)
            {
                await Interaction.ReplyErrorAsync(GenericError.NoResults);
                return;
            }

            var responseText = string.Join("\n", regexChannels.Select(x => $"- {x.RegexChannelId}: {x.Regex} in <#{x.ChannelId}>"));
            await Interaction.ReplyAsync($"Bridges for this guild", responseText); //tpdo: [REFACTOR] Investigate extra newline?
        }

        [SlashCommand("remove", "Remove RegEx channels")]
        //todo: [FEATURE] Channel ID option to avoid leftovers
        public async Task DeleteRegexChannelsAsync
        (
            [Summary(description: "[Opt] Id to delete")] string? targetId = null
        )
        {
            var parsedTargetId = Utils.ToUlongOrNull(targetId);
            if (targetId is not null && parsedTargetId is null)
            {
                await Interaction.ReplyInvalidParameterErrorAsync("target ID");
                return;
            }

            var query = _dbContext.RegexChannels.ForGuild(Context.Guild.Id);
            query = parsedTargetId is null
                ? query.ForChannel(Context.Channel.Id)
                : query.Where(x => x.RegexChannelId == parsedTargetId);

            var regexChannels = await query.ToArrayAsync();
            if (regexChannels.Length == 0)
            {
                await Interaction.ReplyErrorAsync(GenericError.NoResults);
                return;
            }

            _dbContext.RegexChannels.RemoveRange(regexChannels);
            _logger.LogDebug("{intTag} Removing {channelCount} regex channels in guild {guild}", GetIntTag(), regexChannels.Length, Context.Guild.Log());
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("{intTag} Removed {channelCount} regex channels in guild {guild}", GetIntTag(), regexChannels.Length, Context.Guild.Log());
            await Interaction.ReplyAsync($"Removed **{regexChannels.Length}** RegEx channel{(regexChannels.Length == 1 ? string.Empty : "s")}");
        }
    }
}
