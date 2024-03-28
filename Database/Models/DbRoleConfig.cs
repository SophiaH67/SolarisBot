﻿using Microsoft.EntityFrameworkCore;

namespace SolarisBot.Database
{
    [PrimaryKey(nameof(RoleConfigId))]
    public class DbRoleConfig : DbModelBase
    {
        public ulong RoleConfigId { get; set; } = ulong.MinValue;
        public ulong RoleId { get; set; } = ulong.MinValue;
        public ulong RoleGroupId { get; set; } = ulong.MinValue;
        public string Identifier { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsDeleted { get; set; } = false; //todo: impl

        public virtual DbRoleGroup RoleGroup { get; set; } = null!;

        public DbRoleConfig() { }

        public override string ToString()
            => $"{Identifier}(Group {RoleGroupId})";
    }
}
