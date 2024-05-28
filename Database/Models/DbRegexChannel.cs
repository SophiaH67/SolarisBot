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

        public override string ToString()
            => $"[{RegexChannelId}]{Regex}(Guild {GuildId}, Channel {ChannelId})";
    }
}
