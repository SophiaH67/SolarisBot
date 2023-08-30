﻿using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace SolarisBot.Database
{
    [PrimaryKey(nameof(GId))]
    public class DbGuild
    {
        public ulong GId { get; set; } = 0;
        public ulong VouchRoleId { get; set; } = 0;
        public ulong VouchPermissionRoleId { get; set; } = 0;
        public ulong CustomColorPermissionRoleId { get; set; } = 0;
        public bool JokeRenameOn { get; set; } = false;
        public ulong JokeRenameTimeoutMin { get; set; } = 0;
        public ulong JokeRenameTimeoutMax { get; set; } = 0;
        public ulong MagicRoleId { get; set; } = 0;
        public ulong MagicRoleTimeout { get; set; } = 0;
        public ulong MagicRoleNextUse { get; set; } = 0;
        public bool MagicRoleRenameOn { get; set; } = false;

        [ForeignKey(nameof(DbRoleGroup.GId))]
        public virtual ICollection<DbRoleGroup> RoleGroups { get; set; } = new HashSet<DbRoleGroup>();

        [ForeignKey(nameof(DbQuote.GId))]
        public virtual ICollection<DbQuote> Quotes { get; set; } = new HashSet<DbQuote>();

        [ForeignKey(nameof(DbJokeTimeout.GId))]
        public virtual ICollection<DbJokeTimeout> JokeTimeouts { get; set; } = new HashSet<DbJokeTimeout>();

        public DbGuild() { } //To avoid defaults not setting
    }
}
