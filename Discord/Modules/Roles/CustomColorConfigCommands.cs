﻿using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using SolarisBot.Database;
using SolarisBot.Discord.Common;
using SolarisBot.Discord.Common.Attributes;

namespace SolarisBot.Discord.Modules.Roles
{
    [Module("roles/customcolor")]
    public sealed class CustomColorConfigCommands : SolarisInteractionModuleBase
    {
        private readonly ILogger<CustomColorConfigCommands> _logger;
        private readonly DatabaseContext _dbContext;
        internal CustomColorConfigCommands(ILogger<CustomColorConfigCommands> logger, DatabaseContext dbctx)
        {
            _dbContext = dbctx;
            _logger = logger;
        }

        [SlashCommand("cfg-customcolor", "[MANAGE ROLES ONLY] Set up custom color creation (Not setting disabled it)"), DefaultMemberPermissions(GuildPermission.ManageRoles), RequireUserPermission(ChannelPermission.ManageRoles)]
        public async Task ConfigureCustomColorAsync(IRole? creationrole = null)
        {
            var guild = await _dbContext.GetOrCreateTrackedGuildAsync(Context.Guild.Id);

            guild.CustomColorPermissionRoleId = creationrole?.Id ?? ulong.MinValue;

            _logger.LogDebug("{intTag} Setting custom colors to role={role} in guild {guild}", GetIntTag(), creationrole?.Log() ?? "0", Context.Guild.Log());
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("{intTag} Set custom colors to role={role} in guild {guild}", GetIntTag(), creationrole?.Log() ?? "0", Context.Guild.Log());
            await Interaction.ReplyAsync($"Custom color creation is currently **{(creationrole is not null ? "enabled" : "disabled")}**\n\nCreation Role: **{creationrole?.Mention ?? "None"}**");
        }
    }
}
