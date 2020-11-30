using Discord;
using Discord.WebSocket;
using Maina.Core;
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
		public static async Task<IUserMessage> ReplyWithError (IUserMessage message, string errorMessage, string imageUrl = null) {
			EmbedBuilder eb = new EmbedBuilder {Color = new Color((uint)EmbedColor.Red) };
			if (imageUrl != null)
				eb.WithThumbnailUrl(imageUrl);
			eb.WithAuthor(errorMessage);
			await message.Channel.TriggerTypingAsync().ConfigureAwait(false);
            return await message.Channel.SendMessageAsync(string.Empty, false, eb.Build());
		}


		public static async Task DeleteAllReactionsWithEmote (IUserMessage message, IEmote emote) {
			if (message.Reactions.ContainsKey(emote)) {
				await using (var usersEnumerator = message.GetReactionUsersAsync(emote, int.MaxValue).GetAsyncEnumerator()) {
					while (await usersEnumerator.MoveNextAsync()) {
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

							bool publish = false;
							StringBuilder pings = new StringBuilder();
							if (gc.AllNewsRole.HasValue) {
								SocketRole role = guild.GetRole(gc.AllNewsRole.Value);
								pings.Append(role.Mention);
								pings.Append(" ");
								publish = true;
							}
							foreach (string tag in tags) {
								if (gc.NewsRoles.ContainsKey(tag)) {
									SocketRole role = guild.GetRole(gc.NewsRoles[tag]);
									pings.Append(role.Mention);
									pings.Append(" ");
									publish = true;
								}
							}

							//Only publish if there is at least one tag assigned to a role.
							if (publish) {
								SocketTextChannel channel = guild.GetTextChannel(gc.NewsChannel.Value);
								_ = channel.SendMessageAsync(pings.ToString(), false, payload.Build());
							}
						}
					}
					catch (Exception){ }
				}
			}
		}

	}
}
