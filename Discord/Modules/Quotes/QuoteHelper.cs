using Discord;
using Microsoft.EntityFrameworkCore;
using SolarisBot.Database;

namespace SolarisBot.Discord.Modules.Quotes
{
    internal static class QuoteHelper
    {
        internal static async Task<DbQuote[]> GetQuotesAsync(this DatabaseContext dbCtx, ulong guild, ulong? authorId = null, ulong? creatorId = null, ulong? quoteId = null, string? content = null, int offset = 0, int limit = 0)
        {
            if (authorId is null && creatorId is null && quoteId is null && content is null && offset != 0)
                return [];

            IQueryable<DbQuote> dbQuery = dbCtx.Quotes;
            if (guild != 0)
                dbQuery = dbQuery.ForGuild(guild);

            if (quoteId is not null)
                dbQuery = dbQuery.Where(x => x.QuoteId == quoteId);
            else
            {
                if (authorId is not null)
                    dbQuery = dbQuery.Where(x => x.AuthorId == authorId);
                if (creatorId is not null)
                    dbQuery = dbQuery.Where(x => x.CreatorId == creatorId);
                if (content is not null)
                    dbQuery = dbQuery.Where(x => EF.Functions.Like(x.Text, $"%{content}%"));
                if (offset > 0)
                    dbQuery = dbQuery.Skip(offset);
            }

            if (limit > 0)
                dbQuery = dbQuery.Take(limit);

            return await dbQuery.ToArrayAsync();
        }
    }
}
