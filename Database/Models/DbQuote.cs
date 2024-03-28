﻿using Microsoft.EntityFrameworkCore;

namespace SolarisBot.Database
{
    [PrimaryKey(nameof(QuoteId))]
    public class DbQuote : DbModelBase
    {
        public ulong QuoteId { get; set; } = ulong.MinValue;
        public ulong GuildId { get; set; } = ulong.MinValue;
        public string Text { get; set; } = string.Empty;
        public ulong AuthorId { get; set; } = ulong.MinValue;
        public ulong CreatorId { get; set; } = ulong.MinValue;
        public ulong ChannelId { get; set; } = ulong.MinValue;
        public ulong MessageId { get; set; } = ulong.MinValue;
        public bool IsDeleted { get; set; } = false; //todo: [TEST] Do new DB Constraints and soft delete work?

        public DbQuote() { }

        public override string ToString()
        {
            var len = Text.Length;
            var text = len > 30 ? Text[..27] + "..." : Text;
            return $"{QuoteId}: \"{text}\" - <@{AuthorId}>";
        }
    }

    internal static class DbQuoteExtensions
    {
        internal static IQueryable<DbQuote> ForGuild(this IQueryable<DbQuote> query, ulong id)
            => query.Where(x => x.GuildId == id);

        internal static IQueryable<DbQuote> IsDeleted(this IQueryable<DbQuote> query, bool isDeleted)
            => query.Where(x => x.IsDeleted == isDeleted);
    }
}
