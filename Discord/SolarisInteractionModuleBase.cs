﻿using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;

namespace SolarisBot.Discord
{
    /// <summary>
    /// Extended InteractionModeuleBase with a few core functions
    /// </summary>
    public abstract class SolarisInteractionModuleBase : InteractionModuleBase
    {
        /// <summary>
        /// Needs to be overriden for internal error logging
        /// </summary>
        protected virtual ILogger? GetLogger() => null;

        #region Embeds
        /// <summary>
        /// Respond with an embed
        /// </summary>
        internal async Task RespondEmbedAsync(Embed embed, bool isEphemeral = false)
        {
            try
            {
                await RespondAsync(embed: embed, ephemeral: isEphemeral);
            }
            catch (Exception ex)
            {
                GetLogger()?.LogError(ex, "Failed to respond to interaction");
            }
        }

        /// <summary>
        /// Respond with an embed
        /// </summary>
        internal async Task RespondEmbedAsync(string title, string content, EmbedResponseType responseType = EmbedResponseType.Default, bool isEphemeral = false)
        {
            var embed = DiscordUtils.Embed(title, content, responseType);
            await RespondEmbedAsync(embed, isEphemeral: isEphemeral);
        }

        /// <summary>
        /// Respond with an error embed
        /// </summary>
        internal async Task RespondErrorEmbedAsync(string title, string content, bool isEphemeral = false)
        {
            var embed = DiscordUtils.EmbedError(title, content);
            await RespondEmbedAsync(embed, isEphemeral: isEphemeral);
        }

        /// <summary>
        /// Respond with an error embed by exception
        /// </summary>
        internal async Task RespondErrorEmbedAsync(Exception exception, bool isEphemeral = false)
        {
            var embed = DiscordUtils.EmbedError(exception);
            await RespondEmbedAsync(embed, isEphemeral: isEphemeral);
        }

        /// <summary>
        /// Respond with error embed by GET
        /// </summary>
        internal async Task RespondErrorEmbedAsync(EmbedGenericErrorType get, bool isEphemeral = false)
        {
            var embed = DiscordUtils.EmbedError(get);
            await RespondEmbedAsync(embed, isEphemeral: isEphemeral);
        }
        #endregion
    }
}
