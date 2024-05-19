﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace SolarisBot.Database
{
    internal sealed class DatabaseContext : DbContext
    {
        private readonly ILogger<DatabaseContext> _logger;
        private static bool _hasMigrated = false;

        public DatabaseContext(DbContextOptions<DatabaseContext> options, ILogger<DatabaseContext> logger) : base(options) //Todo: [OPTIMIZE] Use select statements, better updates?
        {
            _logger = logger;
            TryMigrate();
        }

        public DbSet<DbGuildConfig> GuildConfigs { get; set; }
        public DbSet<DbJokeTimeout> JokeTimeouts { get; set; }
        public DbSet<DbQuote> Quotes { get; set; }
        public DbSet<DbRoleConfig> RoleConfigs { get; set; }
        public DbSet<DbRoleGroup> RoleGroups { get; set; }
        public DbSet<DbReminder> Reminders { get; set; }
        public DbSet<DbBridge> Bridges { get; set; }
        public DbSet<DbRegexChannel> RegexChannels { get; set; }

        /// <summary>
        /// Sets the filters for all queries to remove soft deleted entries
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder) //todo: [TEST] Do global query filters apply?
        {
            modelBuilder.Entity<DbRoleGroup>().HasQueryFilter(x => !x.DeletedAt.HasValue);
            modelBuilder.Entity<DbRoleConfig>().HasQueryFilter(x => !x.DeletedAt.HasValue);
            modelBuilder.Entity<DbQuote>().HasQueryFilter(x => !x.DeletedAt.HasValue);
            modelBuilder.Entity<DbJokeTimeout>().HasQueryFilter(x => !x.DeletedAt.HasValue);
            modelBuilder.Entity<DbReminder>().HasQueryFilter(x => !x.DeletedAt.HasValue);
            modelBuilder.Entity<DbBridge>().HasQueryFilter(x => !x.DeletedAt.HasValue);
            modelBuilder.Entity<DbRegexChannel>().HasQueryFilter(x => !x.DeletedAt.HasValue);
        }

        /// <summary>
        /// Attempts to save changes to the database
        /// </summary>
        /// <returns>Number of changes, or -1 on error</returns>
        internal async Task<(int, Exception?)> TrySaveChangesAsync()
        {
            try
            {
                return (await SaveChangesAsync(), null);
            }
            catch (Exception ex)
            {
                return (-1, ex);
            }
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) //todo: handle modified and added via db too, make added immutable
        {
            var changedEntries = ChangeTracker.Entries().Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);
            if (changedEntries.Any())
            {
                var now = Utils.GetCurrentUnix();
                foreach (var entry in changedEntries)
                {
                    if (entry.Entity is not DbModelBase baseEntity)
                        continue;

                    if (entry.State == EntityState.Added)
                        baseEntity.CreatedAt = now;
                    baseEntity.UpdatedAt = now;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }

        #region Migration
        /// <summary>
        /// Attempts to migrate the database, throws on error
        /// </summary>
        private void TryMigrate() //todo: [FEATURE] code first, data validation?
        {
            if (_hasMigrated) return;

            var versionQuery = Database.SqlQueryRaw<int>("PRAGMA user_version").AsEnumerable();
            var version = versionQuery.Any() ? versionQuery.FirstOrDefault() : 0;
            var migrationVersion = version;
            _logger.LogInformation("Current database version is {version}, checking for migrations", version);

            try
            {
                var transaction = Database.BeginTransaction();
                var queries = new List<string>();

                if (version < 1)
                {
                    queries.AddRange(new List<string>()
                    {
                        "PRAGMA foreign_keys = ON",

                        "CREATE TABLE GuildConfigs(GuildId INTEGER PRIMARY KEY NOT NULL, VouchRoleId INTEGER NOT NULL DEFAULT 0, VouchPermissionRoleId INTEGER NOT NULL DEFAULT 0, CustomColorPermissionRoleId INTEGER NOT NULL DEFAULT 0, JokeRenameOn BOOL NOT NULL DEFAULT 0, JokeRenameTimeoutMin INTEGER NOT NULL DEFAULT 0, JokeRenameTimeoutMax INTEGER NOT NULL DEFAULT 0, MagicRoleId INTEGER NOT NULL DEFAULT 0, MagicRoleTimeout INTEGER NOT NULL DEFAULT 0, MagicRoleNextUse INTEGER NOT NULL DEFAULT 0, MagicRoleRenameOn BOOL NOT NULL DEFAULT 0, RemindersOn BOOL NOT NULL DEFAULT 0, QuotesOn BOOL NOT NULL DEFAULT 0, AutoRoleId INTEGER NOT NULL DEFAULT 0, SpellcheckRoleId INTEGER NOT NULL DEFAULT 0, StealNicknameOn BOOL NOT NULL DEFAULT 0, GififyOn BOOL NOT NULL DEFAULT 0, QuarantineRoleId INTEGER NOT NULL DEFAULT 0, UserAnalysisChannelId INTEGER NOT NULL DEFAULT 0, UserAnalysisWarnAt INTEGER NOT NULL DEFAULT 0, UserAnalysisKickAt INTEGER NOT NULL DEFAULT 0, UserAnalysisBanAt INTEGER NOT NULL DEFAULT 0, CreatedAt INTEGER NOT NULL DEFAULT strftime('%s', 'now', 'utc'), UpdatedAt INTEGER NOT NULL DEFAULT strftime('%s', 'now', 'utc'))",

                        "CREATE TABLE RoleGroups(RoleGroupId INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, GuildId INTEGER REFERENCES GuildConfigs(GuildId) ON DELETE CASCADE ON UPDATE CASCADE, Identifier TEXT NOT NULL DEFAULT \"\", Description TEXT NOT NULL DEFAULT \"\", AllowOnlyOne BOOL NOT NULL DEFAULT 0, RequiredRoleId INTEGER NOT NULL DEFAULT 0, DeletedAt INTEGER DEFAULT NULL, CreatedAt INTEGER NOT NULL DEFAULT strftime('%s', 'now', 'utc'), UpdatedAt INTEGER NOT NULL DEFAULT strftime('%s', 'now', 'utc'))",
                        "CREATE TRIGGER RoleGroupsAvoidDuplicateInsert BEFORE INSERT ON RoleGroups BEGIN SELECT RAISE(ABORT, 'Duplicate role group on insert') WHERE EXISTS (SELECT 1 FROM RoleGroups WHERE (NEW.DeletedAt IS NULL AND DeletedAt IS NULL AND NEW.GuildId = GuildId AND NEW.Identifier = Identifier)); END;",
                        "CREATE TRIGGER RoleGroupsAvoidDuplicateUpdate BEFORE UPDATE ON RoleGroups BEGIN SELECT RAISE(ABORT, 'Duplicate role group on update') WHERE EXISTS (SELECT 1 FROM RoleGroups WHERE (NEW.DeletedAt IS NULL AND DeletedAt IS NULL AND NEW.GuildId = GuildId AND NEW.Identifier = Identifier)); END;",
                        "CREATE TRIGGER RoleGroupsSoftDelete BEFORE DELETE ON RoleGroups FOR EACH ROW BEGIN UPDATE RoleGroups SET DeletedAt = strftime('%s', 'now', 'utc') WHERE RoleGroupId = OLD.RoleGroupId; END;",

                        "CREATE TABLE RoleConfigs(RoleConfigId INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, RoldId INTEGER NOT NULL DEFAULT 0, RoleGroupId INTEGER REFERENCES RoleGroups(RoleGroupId) ON DELETE CASCADE ON UPDATE CASCADE, Identifier TEXT NOT NULL DEFAULT \"\", Description TEXT NOT NULL DEFAULT \"\", DeletedAt INTEGER DEFAULT NULL, CreatedAt INTEGER NOT NULL DEFAULT strftime('%s', 'now', 'utc'), UpdatedAt INTEGER NOT NULL DEFAULT strftime('%s', 'now', 'utc')))",
                        "CREATE TRIGGER RoleConfigsAvoidDuplicateInsert BEFORE INSERT ON RoleConfigs BEGIN SELECT RAISE(ABORT, 'Duplicate role config on insert') WHERE EXISTS (SELECT 1 FROM RoleConfigs WHERE (NEW.DeletedAt IS NULL AND DeletedAt IS NULL AND (NEW.RoleId = RoleId OR (NEW.RoleGroupId = RoleGroupId AND NEW.Identifier = Identifier)))); END;",
                        "CREATE TRIGGER RoleConfigsAvoidDuplicateUpdate BEFORE UPDATE ON RoleConfigs BEGIN SELECT RAISE(ABORT, 'Duplicate role config on update') WHERE EXISTS (SELECT 1 FROM RoleConfigs WHERE (NEW.DeletedAt IS NULL AND DeletedAt IS NULL AND (NEW.RoleId = RoleId OR (NEW.RoleGroupId = RoleGroupId AND NEW.Identifier = Identifier)))); END;",
                        "CREATE TRIGGER RoleConfigsSoftDelete BEFORE DELETE ON RoleConfigs FOR EACH ROW BEGIN UPDATE RoleConfigs SET DeletedAt = strftime('%s', 'now', 'utc') WHERE RoleConfigId = OLD.RoleConfigId; END;",
                        "CREATE TRIGGER RoleConfigsCascadedSoftDelete AFTER UPDATE ON RoleGroups WHEN NEW.DeletedAt IS NOT NULL AND OLD.DeletedAt != NEW.DeletedAt BEGIN UPDATE RoleConfigs SET DeletedAt = NEW.DeletedAt WHERE RoleGroupId = NEW.RoleGroupId; END;", //todo: does this need "FOR EACH ROW"

                        "CREATE TABLE Quotes(QuoteId INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, GuildId INTEGER REFERENCES GuildConfigs(GuildId) ON DELETE CASCADE ON UPDATE CASCADE, Text TEXT NOT NULL DEFAULT \"\", AuthorId INTEGER NOT NULL DEFAULT 0, CreatorId INTEGER NOT NULL DEFAULT 0, ChannelId INTEGER NOT NULL DEFAULT 0, MessageId INTEGER NOT NULL DEFAULT 0, DeletedAt INTEGER DEFAULT NULL, CreatedAt INTEGER NOT NULL DEFAULT strftime('%s', 'now', 'utc'), UpdatedAt INTEGER NOT NULL DEFAULT strftime('%s', 'now', 'utc')))",
                        "CREATE TRIGGER QuotesAvoidDuplicateInsert BEFORE INSERT ON Quotes BEGIN SELECT RAISE(ABORT, 'Duplicate quote on insert') WHERE EXISTS (SELECT 1 FROM Quotes WHERE (NEW.DeletedAt IS NULL AND DeletedAt IS NULL AND (NEW.MessageId = MessageId OR (NEW.AuthorId = AuthorId AND NEW.GuildId = GuildId AND NEW.Text = Text)))); END;",
                        "CREATE TRIGGER QuotesAvoidDuplicateUpdate BEFORE UPDATE ON Quotes BEGIN SELECT RAISE(ABORT, 'Duplicate quote on update') WHERE EXISTS (SELECT 1 FROM Quotes WHERE (NEW.DeletedAt IS NULL AND DeletedAt IS NULL AND (NEW.MessageId = MessageId OR (NEW.AuthorId = AuthorId AND NEW.GuildId = GuildId AND NEW.Text = Text)))); END;",
                        "CREATE TRIGGER QuotesSoftDelete BEFORE DELETE ON Quotes FOR EACH ROW BEGIN UPDATE Quotes SET DeletedAt = strftime('%s', 'now', 'utc') WHERE QuoteId = OLD.QuoteId; END;",

                        "CREATE TABLE JokeTimeouts(JokeTimeoutId INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, UserId INTEGER NOT NULL DEFAULT 0, GuildId INTEGER REFERENCES GuildConfigs(GuildId) ON DELETE CASCADE ON UPDATE CASCADE, NextUse INTEGER NOT NULL DEFAULT 0, DeletedAt INTEGER DEFAULT NULL, CreatedAt INTEGER NOT NULL DEFAULT strftime('%s', 'now', 'utc'), UpdatedAt INTEGER NOT NULL DEFAULT strftime('%s', 'now', 'utc')), UNIQUE(GuildId, UserId))",
                        "CREATE TRIGGER JokeTimeoutsAvoidDuplicateInsert BEFORE INSERT ON JokeTimeouts BEGIN SELECT RAISE(ABORT, 'Duplicate joke timeout on insert') WHERE EXISTS (SELECT 1 FROM JokeTimeouts WHERE (NEW.DeletedAt IS NULL AND DeletedAt IS NULL AND NEW.GuildId = GuildId AND NEW.UserId = UserId)); END;",
                        "CREATE TRIGGER JokeTimeoutsAvoidDuplicateUpdate BEFORE UPDATE ON JokeTimeouts BEGIN SELECT RAISE(ABORT, 'Duplicate joke timeout on update') WHERE EXISTS (SELECT 1 FROM JokeTimeouts WHERE (NEW.DeletedAt IS NULL AND DeletedAt IS NULL AND NEW.GuildId = GuildId AND NEW.UserId = UserId)); END;",
                        "CREATE TRIGGER JokeTimeoutsSoftDelete BEFORE DELETE ON JokeTimeouts FOR EACH ROW BEGIN UPDATE JokeTimeouts SET DeletedAt = strftime('%s', 'now', 'utc') WHERE JokeTimeoutId = OLD.JokeTimeoutId; END;",

                        "CREATE TABLE Reminders(ReminderId INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, UserId INTEGER NOT NULL DEFAULT 0, GuildId INTEGER REFERENCES GuildConfigs(GuildId) ON DELETE CASCADE ON UPDATE CASCADE, ChannelId INTEGER NOT NULL DEFAULT 0, RemindAt INTEGER NOT NULL DEFAULT 0, Text TEXT NOT NULL DEFAULT \"\", DeletedAt INTEGER DEFAULT NULL, CreatedAt INTEGER NOT NULL DEFAULT strftime('%s', 'now', 'utc'), UpdatedAt INTEGER NOT NULL DEFAULT strftime('%s', 'now', 'utc')))",
                        "CREATE TRIGGER RemindersAvoidDuplicateInsert BEFORE INSERT ON Reminders BEGIN SELECT RAISE(ABORT, 'Duplicate reminder on insert') WHERE EXISTS (SELECT 1 FROM Reminders WHERE (NEW.DeletedAt IS NULL AND DeletedAt IS NULL AND NEW.GuildId = GuildId AND NEW.UserId = UserId AND NEW.Text = Text)); END;",
                        "CREATE TRIGGER RemindersAvoidDuplicateUpdate BEFORE UPDATE ON Reminders BEGIN SELECT RAISE(ABORT, 'Duplicate reminder on update') WHERE EXISTS (SELECT 1 FROM Reminders WHERE (NEW.DeletedAt IS NULL AND DeletedAt IS NULL AND NEW.GuildId = GuildId AND NEW.UserId = UserId AND NEW.Text = Text)); END;",
                        "CREATE TRIGGER RemindersSoftDelete BEFORE DELETE ON Reminders FOR EACH ROW BEGIN UPDATE Reminders SET DeletedAt = strftime('%s', 'now', 'utc') WHERE ReminderId = OLD.ReminderId; END;",

                        "CREATE TABLE Bridges(BridgeId INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, Name TEXT NOT NULL DEFAULT \"\", GuildAId INTEGER NOT NULL DEFAULT 0, ChannelAId INTEGER NOT NULL DEFAULT 0, GuildBId INTEGER NOT NULL DEFAULT 0, ChannelBId INTEGER NOT NULL DEFAULT 0, DeletedAt INTEGER DEFAULT NULL, CreatedAt INTEGER NOT NULL DEFAULT strftime('%s', 'now', 'utc'), UpdatedAt INTEGER NOT NULL DEFAULT strftime('%s', 'now', 'utc')))",
                        "CREATE TRIGGER BridgesAvoidDuplicateInsert BEFORE INSERT ON Bridges BEGIN SELECT RAISE(ABORT, 'Duplicate bridge on insert') WHERE EXISTS (SELECT 1 FROM Bridges WHERE (NEW.DeletedAt IS NULL AND DeletedAt IS NULL AND ((ChannelAId = NEW.ChannelAId AND ChannelBId = NEW.ChannelBId) OR (ChannelBId = NEW.ChannelAId AND ChannelAId = NEW.ChannelBId)))); END;",
                        "CREATE TRIGGER BridgesAvoidDuplicateUpdate BEFORE UPDATE ON Bridges BEGIN SELECT RAISE(ABORT, 'Duplicate bridge on update') WHERE EXISTS (SELECT 1 FROM Bridges WHERE (NEW.DeletedAt IS NULL AND DeletedAt IS NULL AND ((ChannelAId = NEW.ChannelAId AND ChannelBId = NEW.ChannelBId) OR (ChannelBId = NEW.ChannelAId AND ChannelAId = NEW.ChannelBId)))); END;",
                        "CREATE TRIGGER BridgesSoftDelete BEFORE DELETE ON Bridges FOR EACH ROW BEGIN UPDATE Bridges SET DeletedAt = strftime('%s', 'now', 'utc') WHERE BridgeId = OLD.BridgeId; END;",

                        "CREATE TABLE RegexChannels(RegexChannelId INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, GuildId REFERENCES GuildConfigs(GuildId) ON DELETE CASCADE ON UPDATE CASCADE, ChannelId INTEGER NOT NULL DEFAULT 0, Regex TEXT NULL DEFAULT \"\", Punishment INTEGER NOT NULL DEFAULT 0, PunishmentValue INTEGER NOT NULL DEFAULT 0, PunishmentMessage TEXT NOT NULL DEFAULT \"\", PunishmentDelete BOOL NOT NULL DEFAULT 0, PunishmentTimeout INTEGER NOT NULL DEFAULT 0, DeletedAt INTEGER DEFAULT NULL, CreatedAt INTEGER NOT NULL DEFAULT strftime('%s', 'now', 'utc'), UpdatedAt INTEGER NOT NULL DEFAULT strftime('%s', 'now', 'utc')))",
                        "CREATE TRIGGER RegexChannelsAvoidDuplicateInsert BEFORE INSERT ON RegexChannels BEGIN SELECT RAISE(ABORT, 'Duplicate regex channel on insert') WHERE EXISTS (SELECT 1 FROM RegexChannels WHERE (NEW.DeletedAt IS NULL AND DeletedAt IS NULL AND NEW.ChannelId = ChannelId)); END;",
                        "CREATE TRIGGER RegexChannelsAvoidDuplicateUpdate BEFORE UPDATE ON RegexChannels BEGIN SELECT RAISE(ABORT, 'Duplicate regex channel on update') WHERE EXISTS (SELECT 1 FROM RegexChannels WHERE (NEW.DeletedAt IS NULL AND DeletedAt IS NULL AND NEW.ChannelId = ChannelId)); END;",
                        "CREATE TRIGGER RegexChannelsSoftDelete BEFORE DELETE ON RegexChannels FOR EACH ROW BEGIN UPDATE RegexChannels SET DeletedAt = strftime('%s', 'now', 'utc') WHERE RegexChannelId = OLD.RegexChannelId; END;",
                    });
                    migrationVersion = 1;
                }

                if (queries.Count > 0)
                {
                    foreach (var query in queries)
                        Database.ExecuteSqlRaw(query);
                }

                if (migrationVersion > version)
                    Database.ExecuteSql($"PRAGMA user_version = {migrationVersion}");

                transaction.Commit();
                _logger.LogInformation("Database migration complete: {oldVersion} => {newVersion}", version, migrationVersion);
                _hasMigrated = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to migrate database from {originVersion} to {migrationVersion}", version, migrationVersion);
                throw;
            }
        }
        #endregion
    }
}
