﻿using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace SolarisBot.Database
{
    [PrimaryKey(nameof(RgId))]
    public class DbRoleGroup
    {
        public ulong RgId { get; set; } = 0;
        public ulong GId { get; set; } = 0;
        public string Identifier { get; set; } = string.Empty;
        public bool AllowOnlyOne { get; set; } = false;
        public string Description { get; set; } = string.Empty;
        public ulong RequiredRoleId { get; set; } = 0;

        [ForeignKey(nameof(DbRole.RgId))]
        public virtual ICollection<DbRole> Roles { get; set; } = new HashSet<DbRole>();

        public DbRoleGroup() { } //To avoid defaults not setting

        public override string ToString()
            => $"{Identifier}(Guild {GId})";
    }
}
