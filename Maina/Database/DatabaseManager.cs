using System;
using Maina.Core.Logging;
using Maina.Database.Models;
using Raven.Client.Documents;
using Raven.Embedded;

namespace Maina.Database
{
    public class DatabaseManager : IDisposable
    {
        public readonly IDocumentStore Store;

        public DatabaseManager()
        {
            EmbeddedServer.Instance.StartServer();
            Store = EmbeddedServer.Instance.GetDocumentStore("Maina");
        }

        /// <summary>
        /// Checks if config exists, and creates it if not
        /// </summary>
        public void CheckConfig()
        {
            using (var session = Store.OpenSession())
            {
                if (session.Advanced.Exists("Config"))
                {
                    Logger.LogVerbose("Configuration exists in database.");
                    return;
                }
                
                Logger.LogVerbose("No configuration found in database, creating now.");
                Logger.LogForce("Enter bot token: ");
                var token = Console.ReadLine();
                Logger.LogForce("Enter bot prefix: ");
                var prefix = Console.ReadLine();
                
                Save(new BotConfig
                {
                    Id = "Config",
                    Token = token,
                    Prefix = prefix
                });
            }
        }

        /// <summary>
        /// Retrieves an item from the database
        /// </summary>
        /// <param name="id">Unique id of the data</param>
        /// <typeparam name="T">Type deriving from <see cref="DatabaseItem"/></typeparam>
        /// <returns>Data with provided id</returns>
        public T Get<T>(string id) where T : DatabaseItem
        {
            Logger.LogVerbose($"Retrieving from database: {id}.");
            using (var session = Store.OpenSession())
                return session.Load<T>(id); // Will return null if non-existent
        }

		public GuildConfig[] GetAllGuilds () 
        {
			
            Logger.LogVerbose($"Retrieving all {typeof(GuildConfig).Name} from database.");
            using (var session = Store.OpenSession())
				return session.Advanced.LoadStartingWith<GuildConfig>("guild-");
        }
        
        public void AddGuild(ulong id, string name)
        {
            using (var session = Store.OpenSession())
            {
                if (session.Advanced.Exists($"guild-{id}"))
                    return;

                Save(new GuildConfig
                {
                    Id = $"guild-{id}",
                    Prefix = Get<BotConfig>("Config").Prefix
                });

                Logger.LogInfo($"Added config for {name} ({id}).");
            }
        }

        /// <summary>
        /// Save an item or its changes to the database
        /// </summary>
        /// <param name="item">Information to save</param>
        /// <typeparam name="T">Type deriving from <see cref="DatabaseItem"/></typeparam>
        public void Save<T>(T item) where T : DatabaseItem
        {
            if (item == null)
                return;
            
            using (var session = Store.OpenSession())
            {
                session.Store(item, item.Id);
                session.SaveChanges();
            }
        }
        
        /// <summary>
        /// Removes a guild from the database
        /// </summary>
        /// <param name="id">Guild id</param>
        /// <param name="name">Guild name</param>
        public void RemoveGuild(ulong id, string name)
        {
            using (var session = Store.OpenSession())
            {
                session.Delete($"guild-{id}");
                Logger.LogInfo($"Removed config for {name} ({id}).");
            }
        }

        public void Dispose()
        {
            Store.Dispose();
        }
    }
}