﻿using Microsoft.EntityFrameworkCore;

namespace SolarisBot.Database
{
    [PrimaryKey(nameof(RoleId))]
    public class DbRole
    {
        public ulong RoleId { get; set; } = 0;
        public ulong RoleGroupId { get; set; } = 0;
        public string Identifier { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public virtual DbRoleGroup RoleGroup { get; set; } = null!;

        public DbRole() { }

        public override string ToString()
            => $"{Identifier}(Group {RoleGroupId})";
    }
}
