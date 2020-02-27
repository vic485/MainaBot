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
                string token = Console.ReadLine();
                Logger.LogForce("Enter bot prefix: ");
                string prefix = Console.ReadLine();
				Logger.LogForce ("Enter HTTP password: ");
				string pw = Console.ReadLine();

                Save(new BotConfig
                {
                    Id = "Config",
                    Token = token,
                    Prefix = prefix,
					SecretToken = pw
                });
            }
        }


		public void ResetConfig() {
			using (var session = Store.OpenSession())
            {
				if (session.Advanced.Exists("Config")) {
					session.Delete("Config");
					session.SaveChanges();
					Logger.LogInfo($"Reset bot configuration.");
				}
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

		/// <summary>
		/// Gets all items of a type from the DB by filtered by and id prefix.
		/// </summary>
		/// <typeparam name="T">The type of items to retrieve.</typeparam>
		/// <param name="prefix">The prefix an item id mus have to be included in the collection.</param>
		/// <returns>An array with all items of type T.</returns>
		public T[] GetAll<T> (string prefix)
		{
			Logger.LogVerbose($"Retrieving all {typeof(T).Name} from database.");
			using (var session = Store.OpenSession())
				return session.Advanced.LoadStartingWith<T>(prefix);
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
		/// Removes an item from the Database.
		/// </summary>
		/// <param name="item">The item to remove (only requires Id)</param>
		/// <typeparam name="T">Type deriving from <see cref="DatabaseItem"/></typeparam>
		public void Remove<T> (T item) where T : DatabaseItem{
			using (var session = Store.OpenSession())
            {
                session.Delete(item.Id);
				session.SaveChanges();
                Logger.LogInfo($"Removed {typeof(T).Name} from DB ({item.Id}).");
            }
		}

		public bool Exists<T> (T item) where T : DatabaseItem {
			using (var session = Store.OpenSession())
            {
                return session.Advanced.Exists(item.Id);
                
            }
		}



		#region Guilds
		public GuildConfig[] GetAllGuilds ()
        {
			return GetAll<GuildConfig>("guild-");
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
        /// Removes a guild from the database
        /// </summary>
        /// <param name="id">Guild id</param>
        /// <param name="name">Guild name</param>
        public void RemoveGuild(ulong id, string name)
        {
            using (var session = Store.OpenSession())
            {
                session.Delete($"guild-{id}");
				session.SaveChanges();
                Logger.LogInfo($"Removed config for {name} ({id}).");
            }
        }


		public void UpdateGuilds () {
			try {
				OldGuildConfig[] oldGuilds = GetAll<OldGuildConfig>("guild-");
				if (oldGuilds != null) {
					foreach (OldGuildConfig og in oldGuilds) {
						GuildConfig ng = new GuildConfig{
							Id = og.Id,
							Prefix = og.Prefix,
							NewsRoles = og.NewsRoles,
							AllNewsRole = og.AllNewsRole,
							NewsChannel = og.NewsChannel,
							WelcomeChannel = og.WelcomeChannel,
							WelcomeMessage = og.WelcomeMessage,
						};
						RoleMenu rm = ng.DefaultSelfRoleMenu;
						rm.Channel = og.SelfRoleMenu[0];
						rm.Message = og.SelfRoleMenu[1];
						rm.SelfRoles = og.SelfRoles;
						Remove<OldGuildConfig>(og);
						Save<GuildConfig>(ng);
					}
				}
			}
			catch(Exception) {
				Logger.LogInfo("No need to update guilds.");
			}
		}

		#endregion

		public void Dispose()
        {
            Store.Dispose();
        }
    }
}
