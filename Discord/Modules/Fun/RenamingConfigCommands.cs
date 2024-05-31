using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SolarisBot.Database;
using SolarisBot.Discord.Common;
using SolarisBot.Discord.Common.Attributes;

namespace SolarisBot.Discord.Modules.Fun
{
    [Module("fun/renaming"), Group("cfg-rename", "[MANAGE NAMES ONLY] Renaming config commands")]
    [RequireContext(ContextType.Guild), DefaultMemberPermissions(GuildPermission.ManageNicknames), RequireUserPermission(GuildPermission.ManageNicknames)]
    public sealed class RenamingConfigCommands : SolarisInteractionModuleBase
    {
        private readonly ILogger<RenamingConfigCommands> _logger;
        private readonly DatabaseContext _dbContext;
        internal RenamingConfigCommands(ILogger<RenamingConfigCommands> logger, DatabaseContext dbctx)
        {
            _dbContext = dbctx;
            _logger = logger;
        }

        [SlashCommand("config", "Set up joke renaming (Timeout in seconds)")]
        public async Task ConfigureRenameAsync
        (
            [Summary(description: "Is feature enabled?")] bool enabled,
            [Summary(description: "[Opt] Minimum time between renaming (in sec)")] string minTimeout = "1800",
            [Summary(description: "[Opt] Maximum time between renaming (in sec)")] string maxTimeout = "86400"
        )
        {
            var parsedMinTimeout = DiscordUtils.StringToIdZeroInclusive(minTimeout);
            if (parsedMinTimeout is null)
            {
                await Interaction.ReplyInvalidParameterErrorAsync("min timeout");
                return;
            }
            var parsedMaxTimeout = DiscordUtils.StringToIdZeroInclusive(maxTimeout);
            if (parsedMaxTimeout is null)
            {
                await Interaction.ReplyInvalidParameterErrorAsync("max timeout");
                return;
            }

            var guild = await _dbContext.GetOrCreateTrackedGuildAsync(Context.Guild.Id);

            guild.JokeRenameOn = enabled;
            guild.JokeRenameTimeoutMax = parsedMaxTimeout.Value;
            guild.JokeRenameTimeoutMin = parsedMinTimeout.Value > parsedMaxTimeout.Value ? parsedMaxTimeout.Value : parsedMinTimeout.Value;

            _logger.LogDebug("{intTag} Setting joke renaming to enabled={role}, mintimeout={minTimeout}, maxtimeout={maxTimeout} in guild {guild}", GetIntTag(), enabled, parsedMinTimeout, parsedMaxTimeout.Value, Context.Guild.Log());
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("{intTag} Set joke renaming to enabled={role}, mintimeout={minTimeout}, maxtimeout={maxTimeout} in guild {guild}", GetIntTag(), enabled, parsedMinTimeout, parsedMaxTimeout.Value, Context.Guild.Log());
            await Interaction.ReplyAsync($"Joke Renaming is currently **{(enabled ? "enabled" : "disabled")}**\n\nTime: **{parsedMinTimeout} - {parsedMaxTimeout.Value} seconds**");
        }

        [SlashCommand("reset", "Reset joke rename cooldowns")]
        public async Task RenameResetCooldownsAsync()
        {
            _logger.LogDebug("{intTag} Deleting all joke timeout cooldowns for guild {guild}", GetIntTag(), Context.Guild.Log());
            var deleted = await _dbContext.JokeTimeouts.ForGuild(Context.Guild.Id).ExecuteDeleteAsync();
            _logger.LogInformation("{intTag} Deleted all {delCount} joke timeout cooldowns for guild {guild}", GetIntTag(), deleted, Context.Guild.Log());
            if (deleted == 0)
                await Interaction.ReplyErrorAsync(GenericError.NoResults);
            else
                await Interaction.ReplyAsync($"Successfully deleted all **{deleted}** joke timeouts for this guild");
        }
    }
}
