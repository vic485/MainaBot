﻿using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Maina.Core.Logging;
using Maina.Database;
using Maina.Database.Models;

namespace Maina.Core
{
    public class DiscordHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly DatabaseManager _database;
        private readonly CommandService _commandService;

        private IServiceProvider _serviceProvider;

        public DiscordHandler(DiscordSocketClient client, DatabaseManager database, CommandService commandService)
        {
            _client = client;
            _database = database;
            _commandService = commandService;
        }

        // Connects events and logs into discord
        public async Task InitializeAsync(IServiceProvider services)
        {
            // TODO: Add the rest of the events we will handle here
            _client.Connected += Connected;
            _client.Disconnected += Disconnected;
            _client.Ready += ReadyAsync;
            //_client.LatencyUpdated += LatencyUpdated;
            _client.Log += Log;
            //_client.ChannelCreated
            //_client.ChannelDestroyed
            //_client.ChannelUpdated
            _client.GuildAvailable += GuildAvailable;
            _client.GuildUnavailable += GuildUnavailable;
            //_client.GuildUpdated
            _client.JoinedGuild += JoinedGuildAsync;
            _client.LeftGuild += LeftGuild;
            //_client.LoggedIn
            //_client.LoggedOut
            //_client.MessageDeleted
            _client.MessageReceived += MessageReceivedAsync;
            //_client.MessageUpdated
            _client.ReactionAdded += ReactionAddedAsync;
            _client.ReactionRemoved += ReactionRemovedAsync;
            //_client.ReactionsCleared
            //_client.RecipientAdded
            //_client.RecipientRemoved
            //_client.RoleCreated
            //_client.RoleDeleted
            //_client.RoleUpdated
            //_client.UserBanned
            _client.UserJoined += UserJoinedAsync;
            //_client.UserLeft
            //_client.UserUnbanned
            //_client.UserUpdated
            //_client.CurrentUserUpdated
            //_client.GuildMembersDownloaded
            //_client.GuildMemberUpdated
            //_client.UserIsTyping
            //_client.VoiceServerUpdated
            //_client.UserVoiceStateUpdated

            Logger.LogVerbose("Logging into Discord.");
            await _client.LoginAsync(TokenType.Bot, _database.Get<BotConfig>("Config").Token).ConfigureAwait(false);
            await _client.StartAsync().ConfigureAwait(false);

            _serviceProvider = services;
            await _commandService.AddModulesAsync(Assembly.GetEntryAssembly(), services);
            Logger.LogVerbose($"Commands registered: {_commandService.Commands.Count()}.");
        }




		/// <summary>
		/// Sends discord log messages to Reimu's logger
		/// </summary>
		/// <param name="logMessage">Discord message</param>
		private static Task Log(LogMessage logMessage)
        {
            Logger.LogInfo(logMessage.Message ?? logMessage.Exception.Message);
            return Task.CompletedTask;
        }

        #region Connections

        /// <summary>
        /// Made a successful connection to discord, and Reimu is ready to run
        /// </summary>
        /// <returns></returns>
        private async Task ReadyAsync()
        {
            Logger.LogInfo("Everything ready to run.");
            // TODO: Change this to custom status when we can
            await _client.SetGameAsync($"{_database.Get<BotConfig>("Config").Prefix}help");
        }

        /// <summary>
        /// Informs the log that we have been disconnected from Discord
        /// </summary>
        /// <param name="error">Error message</param>
        private static Task Disconnected(Exception error)
        {
            Logger.LogInfo($"Disconnected from Discord: {error.Message}.");
            return Task.CompletedTask;
        }

        private static Task Connected()
        {
            Logger.LogInfo("Connected to Discord gateway.");
            return Task.CompletedTask;
        }

        #endregion

        #region Guild Events

        private Task GuildAvailable(SocketGuild guild)
        {
            Logger.LogVerbose($"Guild {guild.Name} is available.");
            /*if (_database.Get<BotConfig>("Config").GuildBlacklist.Contains(guild.Id))
            {
                Logger.LogVerbose($"Guild {guild.Name} is blacklisted, leaving...");
                guild.LeaveAsync();
            }
            else
            {
                _database.AddGuild(guild.Id, guild.Name, guild.VoiceRegionId);
            }*/
            _database.AddGuild(guild.Id, guild.Name);

            return Task.CompletedTask;
        }

        private Task GuildUnavailable(SocketGuild guild)
        {
            Logger.LogVerbose($"Guild {guild.Name} is unavailable.");
            return Task.CompletedTask;
        }

        private async Task JoinedGuildAsync(SocketGuild guild)
        {
            Logger.LogVerbose($"Joined guild {guild.Name}.");
            var config = _database.Get<BotConfig>("Config");
            /*if (config.GuildBlacklist.Contains(guild.Id))
            {
                Logger.LogVerbose($"Guild {guild.Name} is blacklisted, leaving...");
                await guild.LeaveAsync();
                return;
            }*/

            _database.AddGuild(guild.Id, guild.Name);
            // TODO: Send message to default channel
        }

        private Task LeftGuild(SocketGuild guild)
        {
            Logger.LogVerbose($"Removed from, or left guild {guild.Name}");
            _database.RemoveGuild(guild.Id, guild.Name);
            return Task.CompletedTask;
        }

        #endregion

        private async Task UserJoinedAsync(SocketGuildUser user)
        {
            var config = _database.Get<GuildConfig>($"guild-{user.Guild.Id}");
            var channel = user.Guild.GetTextChannel(config.WelcomeChannel);
            
            if (channel == null || string.IsNullOrWhiteSpace(config.WelcomeMessage))
                return;

            await channel.SendMessageAsync(config.WelcomeMessage.Replace("{user}", user.Mention));
        }

        #region Messages

        private async Task MessageReceivedAsync(SocketMessage message)
        {
            // Ignore system, webhook, and other bot messages
            if (!(message is SocketUserMessage userMessage) || message.Author.IsBot)
                return;

            var context = new BotContext(_client, userMessage, _serviceProvider);
            var argPos = 0;
            if (!(userMessage.HasStringPrefix(context.Config.Prefix, ref argPos) ||
                  userMessage.HasStringPrefix(context.GuildConfig.Prefix, ref argPos)))
                return;

            var result = await _commandService.ExecuteAsync(context, argPos, _serviceProvider, MultiMatchHandling.Best);
            if (!result.Error.HasValue)
            {
                // TODO: Log commands for cooldown
                return;
            }

            switch (result.Error)
            {
                case CommandError.UnknownCommand:
                    break;
                case CommandError.ParseFailed:
                    Logger.LogVerbose($"Failed to parse command: {result.ErrorReason}.");
                    break;
                case CommandError.BadArgCount:
                    break;
                case CommandError.ObjectNotFound:
                    break;
                case CommandError.MultipleMatches:
                    break;
                case CommandError.UnmetPrecondition:
                    // TODO: DM error if we can't send messages?
                    if (!result.ErrorReason.Contains("SendMessages"))
                        await context.Channel.SendMessageAsync(result.ErrorReason);
                    break;
                case CommandError.Exception:
                    break;
                case CommandError.Unsuccessful:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> cacheable, ISocketMessageChannel channel,
            SocketReaction reaction)
        {
			try {
				var message = await cacheable.GetOrDownloadAsync();
				if (!(reaction.Channel is SocketGuildChannel guildChannel && reaction.User.Value is SocketGuildUser user))
					return;

				var guild = guildChannel.Guild;
				var config = _database.Get<GuildConfig>($"guild-{guild.Id}");

				RoleMenu rm = FindRoleMenu (config, reaction.Channel.Id, message.Id);
				if (rm == null)
					return;

				string final = reaction.Emote.ToString();
				if (!rm.SelfRoles.ContainsKey(final)) {
					int index = final.IndexOf("<") + 1;
					if (index != -1)
						final= final.Insert(index, "a");
					if (!rm.SelfRoles.ContainsKey(final))
						return;
				}

				var role = guild.GetRole(rm.SelfRoles[final]);
				await user.AddRoleAsync(role);
			}
			catch (Exception e) {

			}
            //await (await user.GetOrCreateDMChannelAsync()).SendMessageAsync($"You have received the role {role.Name}");
        }

		

		private async Task ReactionRemovedAsync(Cacheable<IUserMessage, ulong> cacheable, ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            var message = await cacheable.GetOrDownloadAsync();
            if (!(reaction.Channel is SocketGuildChannel guildChannel && reaction.User.Value is SocketGuildUser user))
                return;

            var guild = guildChannel.Guild;
            var config = _database.Get<GuildConfig>($"guild-{guild.Id}");

			RoleMenu rm = FindRoleMenu (config, reaction.Channel.Id, message.Id);
            if (rm == null)
                return;

			
			string final = reaction.Emote.ToString();
			if (!rm.SelfRoles.ContainsKey(final)) {
				int index = final.IndexOf("<") + 1;
				if (index != -1)
					final= final.Insert(index, "a");
				if (!rm.SelfRoles.ContainsKey(final))
					return;
			}

			var role = guild.GetRole(rm.SelfRoles[final]);

            if (!user.Roles.Contains(role))
                return;

            await user.RemoveRoleAsync(role);
            //await (await user.GetOrCreateDMChannelAsync()).SendMessageAsync($"Removed role {role.Name}");
        }


		private RoleMenu FindRoleMenu(GuildConfig config, ulong channelId, ulong messageId)
		{
			RoleMenu res = null;
			foreach (RoleMenu rm in config.SelfRoleMenus.Values) {
				if (rm.Channel.HasValue && rm.Channel.Value == channelId &&
					rm.Message.HasValue && rm.Message.Value == messageId)
					return rm;
			}
			return res;
		}

        #endregion
    }
}
