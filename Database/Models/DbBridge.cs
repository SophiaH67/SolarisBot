﻿using Microsoft.EntityFrameworkCore;

namespace SolarisBot.Database
{
    [PrimaryKey(nameof(BridgeId))]
    public class DbBridge : DbModelBase
    {
        public ulong BridgeId { get; set; } = ulong.MinValue;
        public string Name { get; set; } = string.Empty;
        public ulong GuildAId { get; set; } = ulong.MinValue;
        public ulong ChannelAId { get; set; } = ulong.MinValue;
        public ulong GuildBId { get; set; } = ulong.MinValue;
        public ulong ChannelBId { get; set; } = ulong.MinValue;
        public ulong? DeletedAt { get; set; } = null; //todo: [TEST] Do new DB Constraints and soft delete work, make deleted field own class?

        public DbBridge() { }

        public override string ToString()
            => $"[{BridgeId}]{Name}({ChannelAId}({GuildAId}) <=> {ChannelBId}({GuildBId}))";
        internal string ToDiscordInfoString()
            => $"**{Name}** *({BridgeId})*";
    }

    internal static class DbBridgeExtensions
    {
        internal static IQueryable<DbBridge> ForChannel(this IQueryable<DbBridge> query, ulong id)
            => query.Where(x => x.ChannelBId == id || x.ChannelAId == id);

        internal static IQueryable<DbBridge> ForGuild(this IQueryable<DbBridge> query, ulong id)
            => query.Where(x => x.GuildBId == id || x.GuildAId == id);
    }
}
