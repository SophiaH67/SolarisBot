using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolarisBot.Database
{
    [PrimaryKey(nameof(RegexChannelId))]
    public class DbRegexChannel : DbModelBase
    {
        public ulong RegexChannelId { get; set; } = ulong.MinValue;
        public ulong GuildId { get; set; } = ulong.MinValue;
        public ulong ChannelId { get; set; } = ulong.MinValue;
        public string Regex { get; set; } = string.Empty;
        public ulong AppliedRoleId { get; set; } = ulong.MinValue;
        public string PunishmentMessage { get; set; } = string.Empty;
        public bool PunishmentDelete { get; set; } = false;
        public ulong PunishmentTimeout { get; set; } = ulong.MinValue;
        public bool IsDeleted { get; set; } = false; //todo: [TEST] Do new DB Constraints and soft delete work?

        public override string ToString() //todo: [Logging] Add better ToString?
            => Regex;
    }

    internal static class DbRegexChannelExtensions
    {
        internal static IQueryable<DbRegexChannel> IsDeleted(this IQueryable<DbRegexChannel> query, bool isDeleted)
            => query.Where(x => x.IsDeleted == isDeleted);
    }
}
