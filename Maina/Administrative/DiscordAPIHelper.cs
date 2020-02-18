using Discord;
using Discord.WebSocket;
using Maina.Database;
using Maina.Database.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Maina.Administrative
{
	public class DiscordAPIHelper
	{

		public static async Task DeleteAllReactionsWithEmote (IUserMessage message, IEmote emote) {
			if (message.Reactions.ContainsKey(emote)) {
				using (var usersEnumerator = message.GetReactionUsersAsync(emote, int.MaxValue).GetEnumerator()) {
					while (await usersEnumerator.MoveNext()) {
						foreach (IUser user in usersEnumerator.Current) {
							await message.RemoveReactionAsync(emote, user);
						}
					}
				}
			}
		}

		public static async Task PublishNews (EmbedBuilder payload, DatabaseManager databaseManager, DiscordSocketClient discordSocketClient, params string [] tags) {
			if (payload != null) {
				foreach (GuildConfig gc in databaseManager.GetAllGuilds()) {
					try {
						if (gc.NewsChannel.HasValue) {
							SocketGuild guild = discordSocketClient.GetGuild(gc.NumberId);

							StringBuilder pings = new StringBuilder();
							if (gc.AllNewsRole.HasValue) {
								SocketRole role = guild.GetRole(gc.AllNewsRole.Value);
								pings.Append(role.Mention);
								pings.Append(" ");
							}
							foreach (string tag in tags) {
								if (gc.NewsRoles.ContainsKey(tag)) {
									SocketRole role = guild.GetRole(gc.NewsRoles[tag]);
									pings.Append(role.Mention);
									pings.Append(" ");
								}
							}
							
						
							SocketTextChannel channel = guild.GetTextChannel(gc.NewsChannel.Value);
							_ = channel.SendMessageAsync(pings.ToString(), false, payload.Build());
						}
					}
					catch (Exception){ }
				}
			}
		}

	}
}
