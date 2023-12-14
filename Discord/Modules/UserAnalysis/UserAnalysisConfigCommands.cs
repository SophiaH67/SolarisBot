﻿using Discord.Interactions;
using Discord;
using Microsoft.Extensions.Logging;
using SolarisBot.Database;
using SolarisBot.Discord.Common;
using SolarisBot.Discord.Common.Attributes;

namespace SolarisBot.Discord.Modules.UserAnalysis
{
    [Module("useranalysis"), Group("useranalysis", "[MODERATE MEMBERS ONLY] User analysis commands")]
    [RequireContext(ContextType.Guild), DefaultMemberPermissions(GuildPermission.ModerateMembers), RequireUserPermission(GuildPermission.ModerateMembers)]
    internal class UserAnalysisConfigCommands : SolarisInteractionModuleBase
    {
        private readonly ILogger<UserAnalysisConfigCommands> _logger;
        private readonly DatabaseContext _dbContext;
        private readonly BotConfig _config;
        internal UserAnalysisConfigCommands(ILogger<UserAnalysisConfigCommands> logger, DatabaseContext dbctx, BotConfig config)
        {
            _dbContext = dbctx;
            _logger = logger;
            _config = config;
        }

        [SlashCommand("config", "Set up user analysis")] //todo: [TESTING] Does configure work?
        public async Task ConfigureAnalysisAsync
        (
            [Summary(description: "Notification channel (none to disable)")] IChannel? channel = null, //todo: tweak defaults
            [Summary(description: "[Optional] Minimum points for warning")] ulong minWarn = 0,
            [Summary(description: "[Optional] Minimum points for ban")] ulong minBan = 0
        )
        {
            var guild = await _dbContext.GetOrCreateTrackedGuildAsync(Context.Guild.Id);

            guild.UserAnalysisChannel = channel?.Id ?? 0;
            guild.UserAnalysisWarnAt = minWarn;
            guild.UserAnalysisBanAt = minBan;

            _logger.LogDebug("{intTag} Setting userAnalysis to channel={analysisChannel}, minWarn={minWarn}, minBan={minBan} in guild {guild}", GetIntTag(), channel?.Log() ?? "0", minWarn, minBan, Context.Guild.Log());
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("{intTag} Set userAnalysis to channel={analysisChannel}, minWarn={minWarn}, minBan={minBan} in guild {guild}", GetIntTag(), channel?.Log() ?? "0", minWarn, minBan, Context.Guild.Log());
            await Interaction.ReplyAsync($"User analysis is currently **{(channel is not null ? "enabled" : "disabled")}**\n\nChannel: **{(channel is null ? "None" : $"<#{channel.Id}>")}**\nWarn at: **{minWarn}**\nBan at: **{minBan}**");
        }

        [UserCommand("Analyze"), SlashCommand("analyze", "Analyze a user")] //todo: [TESTING] Does the user command apply this way?
        public async Task AnalyzeUserAsync(IUser user)
        {
            var gUser = GetGuildUser(user);
            var embed = UserAnalysis.ForUser(gUser, _config).GenerateSummaryEmbed();
            await Interaction.ReplyAsync(embed);
        }

        [ComponentInteraction("solaris_analysis_kick.*", true), RequireBotPermission(GuildPermission.KickMembers)]
        public async Task HandleButtonAnalysisKickAsync(string userId)
            => await ModerateUserAsync(userId, false);

        [ComponentInteraction("solaris_analysis_ban.*", true), RequireBotPermission(GuildPermission.BanMembers)]
        public async Task HandleButtonAnalysisBanAsync(string userId)
            => await ModerateUserAsync(userId, true);

        private async Task ModerateUserAsync(string userId, bool ban) //todo: [TESTING] Does moderation work?
        {
            var gUser = GetGuildUser(Context.User);
            if ((!ban && !gUser.GuildPermissions.KickMembers) || (ban && !gUser.GuildPermissions.BanMembers))
            {
                await Interaction.ReplyErrorAsync($"You do not have permission to {(ban ? "ban" : "kick")} members");
                return;
            }

            var targetUser = await Context.Guild.GetUserAsync(ulong.Parse(userId));
            if (targetUser is null)
            {
                await Interaction.ReplyErrorAsync("User could not be found");
                return;
            }

            var verb = ban ? "Bann" : "Kick";
            _logger.LogDebug("{verb}ing user {targetUser} from guild {guild} via analysis result button triggered by {user}", verb, targetUser.Log(), Context.Guild.Log(), Context.User.Log());
            if (ban)
                await targetUser.BanAsync(reason: $"Banned by {Context.User.Log()} via analysis result button");
            else
                await targetUser.KickAsync($"Kicked by {Context.User.Log()} via analysis result button");
            _logger.LogInformation("{verb}ed user {targetUser} from guild {guild} via analysis result button triggered by {user}", verb, targetUser.Log(), Context.Guild.Log(), Context.User.Log());
            await Interaction.ReplyAsync($"User has been {verb.ToLower()}ed");
        }
    }
}
