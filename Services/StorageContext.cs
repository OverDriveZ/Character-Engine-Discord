﻿using CharacterEngineDiscord.Models.Database;
using Microsoft.EntityFrameworkCore;
using static CharacterEngineDiscord.Services.CommonService;
using CharacterEngineDiscord.Models.Common;

namespace CharacterEngineDiscord.Services
{
    internal class StorageContext : DbContext
    {
        internal DbSet<BlockedGuild> BlockedGuilds { get; set; }
        internal DbSet<BlockedUser> BlockedUsers { get; set; }
        internal DbSet<CaiHistory> CaiHistories { get; set; }
        internal DbSet<Channel> Channels { get; set; }
        internal DbSet<Character> Characters { get; set; }
        internal DbSet<CharacterWebhook> CharacterWebhooks { get; set; }
        internal DbSet<Guild> Guilds { get; set; }
        internal DbSet<HuntedUser> HuntedUsers { get; set; }
        internal DbSet<OpenAiHistoryMessage> OpenAiHistoryMessages { get; set; }

        public StorageContext()
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connString = ConfigFile.DbConnString.Value.IsEmpty() ?
                   $"Data Source={EXE_DIR}{SC}storage{SC}{ConfigFile.DbFileName.Value}" :
                   ConfigFile.DbConnString.Value;

            optionsBuilder.UseSqlite(connString).UseLazyLoadingProxies(true);

            if (Environment.GetEnvironmentVariable("READY") is not null)
            { 
                if (ConfigFile.DbLogEnabled.Value.ToBool())
                    optionsBuilder.LogTo(SqlLog, new[] { DbLoggerCategory.Database.Command.Name })
                                  .EnableSensitiveDataLogging(true)
                                  .EnableDetailedErrors(true);
            }
        }


        protected internal static void SqlLog(string text)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(new string('~', Console.WindowWidth));

            if (text.Contains("INSERT"))
                Console.ForegroundColor = ConsoleColor.Green;
            else if (text.Contains("DELETE"))
                Console.ForegroundColor = ConsoleColor.DarkRed;
            else if (text.Contains("UPDATE"))
                Console.ForegroundColor = ConsoleColor.Magenta;
            else if (text.Contains("SELECT"))
                Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine(text);
            Console.ResetColor();
        }

        protected internal static async Task<Guild> FindOrStartTrackingGuildAsync(ulong guildId, StorageContext? db = null)
        {
            db ??= new StorageContext();
            var guild = await db.Guilds.FindAsync(guildId);

            if (guild is null)
            {
                guild = new() { Id = guildId, GuildMessagesFormat = "[System note: Name of the user: \"{{user}}\"] {{msg}}", BtnsRemoveDelay = 90, GuildCaiPlusMode = false, GuildCaiUserToken = null, GuildOpenAiApiToken = null, GuildOpenAiModel = null, GuildOpenAiApiEndpoint = null };
                await db.Guilds.AddAsync(guild);
                await db.SaveChangesAsync();
                return await FindOrStartTrackingGuildAsync(guildId, db);
            }

            return guild;
        }

        protected internal static async Task<Channel> FindOrStartTrackingChannelAsync(ulong channelId, ulong guildId, StorageContext? db = null)
        {
            db ??= new StorageContext();
            var channel = await db.Channels.FindAsync(channelId);

            if (channel is null)
            {
                channel = new() { Id = channelId, GuildId = (await FindOrStartTrackingGuildAsync(guildId, db)).Id, RandomReplyChance = 0 };
                await db.Channels.AddAsync(channel);
                await db.SaveChangesAsync();
                return await FindOrStartTrackingChannelAsync(channelId, guildId, db);
            }

            return channel;
        }

        protected internal static async Task<Character> FindOrStartTrackingCharacterAsync(Character notSavedCharacter, StorageContext? db = null)
        {
            db ??= new StorageContext();
            var character = await db.Characters.FindAsync(notSavedCharacter.Id);

            if (character is null)
            {
                character = db.Characters.Add(notSavedCharacter).Entity;
                await db.SaveChangesAsync();
                return await FindOrStartTrackingCharacterAsync(character, db);
            }

            return character;
        }
    }
}
