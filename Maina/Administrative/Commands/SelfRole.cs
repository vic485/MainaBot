using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Maina.Core;
using Maina.Core.Logging;
using Maina.Database.Models;

namespace Maina.Administrative.Commands
{
    [RequireBotPermission(GuildPermission.ManageRoles), RequireUserPermission(GuildPermission.ManageRoles)]
    public class SelfRole : MainaBase
    {
		
		private IEmote GetEmote (string key) {
			IEmote resEmote = null;
			Emote emote = null;
			if (Emote.TryParse(key, out emote))
				resEmote = emote;
			else
				resEmote = new Emoji(key);
			return resEmote;
		}

        [Command("selfrole add")]
        public async Task SelfRoleAdd(string s, SocketRole role)
        {
			IEmote emote = GetEmote(s);

            if (Context.GuildConfig.SelfRoles.ContainsKey(emote.ToString()))
            {
                Context.GuildConfig.SelfRoles[emote.ToString()] = role.Id;
				IUserMessage srm = await GetSelfRoleMessage();
				await UpdateSelfRoleMessage(srm);
                await ReplyAsync($"Self role for {Emote.Parse(emote.ToString())} changed to `{role.Name}`",
                    updateGuild: true);
            }
			else {
				Context.GuildConfig.SelfRoles.Add(emote.ToString(), role.Id);
				IUserMessage srm = await GetSelfRoleMessage();
				await UpdateSelfRoleMessage(srm);
				await ReplyAsync($"Added self role {role.Name} - {emote.ToString()}", updateGuild: true);
			}
			
        }

        [Command("selfrole list")]
        public async Task SelfRoleListAsync()
        {
            if (Context.GuildConfig.SelfRoles.Count == 0)
            {
				await DiscordAPIHelper.ReplyWithError(Context.Message, 
					"Guild has no self assignable roles.",
					Context.HttpServerManager.GetIp + "/images/error.png");
                return;
            }
            
			if (Context.GuildConfig.DefaultSelfRoleMenu.ha)
			if ((Context.Guild.GetChannel(Context.GuildConfig.DefaultSelfRoleMenu.Value.Channel) is SocketTextChannel channel &&
                  await channel.GetMessageAsync(Context.GuildConfig.DefaultSelfRoleMenu.Value.Message) is IUserMessage prevMessage))
            {
                await prevMessage.DeleteAsync();
            }


            EmbedBuilder embedBuilder = CreateEmbed(EmbedColor.SalmonPink);

			
			StringBuilder sb = new StringBuilder();
			foreach (string key in Context.GuildConfig.SelfRoles.Keys)
				sb.AppendLine($"{key} - {Context.Guild.GetRole(Context.GuildConfig.SelfRoles[key]).Mention}\n");
           

            embedBuilder.AddField("**Self Roles**", sb.ToString());
            IUserMessage message = await ReplyAsync(string.Empty, embedBuilder.Build());

			

            foreach (var em in Context.GuildConfig.SelfRoles.Keys)
            {
				try {
					IEmote emote = GetEmote(em);
					await message.AddReactionAsync(emote);
				}
				catch (Exception ex) {
					Logger.LogException(ex);
					throw ex;
				}
            }
            
			RoleMenu srm = Context.GuildConfig.DefaultSelfRoleMenu;
            srm.Channel = message.Channel.Id;
			srm.Message = message.Id;
            Context.Database.Save(Context.GuildConfig); // Save manually rather than sending another message
			await Context.Message.DeleteAsync(); //It will look more clean if we delete the command message
        }

        [Command("selfrole remove")]
        public async Task SelfRoleRemoveAsync(string s)
        {
            IEmote emote = GetEmote(s);

            if (!Context.GuildConfig.SelfRoles.ContainsKey(emote.ToString()))
            {
				await DiscordAPIHelper.ReplyWithError(Context.Message, 
					$"There is not a role assigned to {emote.ToString()}",
					Context.HttpServerManager.GetIp + "/images/error.png");
                return;
            }

            Context.GuildConfig.SelfRoles.Remove(emote.ToString());
			IUserMessage srm = await GetSelfRoleMessage();
			await UpdateSelfRoleMessage(srm);
            await ReplyAsync($"Removed self role assigned to {emote.ToString()}", updateGuild: true);
        }

		private async Task<IUserMessage> GetSelfRoleMessage () {
			if (!(Context.Guild.GetChannel(Context.GuildConfig.DefaultSelfRoleMenu.Channel) is SocketTextChannel channel &&
                  await channel.GetMessageAsync(Context.GuildConfig.DefaultSelfRoleMenu.Message) is IUserMessage message))
            {
                return null;
            }
			return message;
		}

		private async Task UpdateSelfRoleMessage (IUserMessage message) {
			if (message != null) {
				EmbedBuilder embedBuilder = CreateEmbed(EmbedColor.SalmonPink);

				StringBuilder sb = new StringBuilder();
				List<IEmote> reactions = new List<IEmote>();
				foreach (string key in Context.GuildConfig.SelfRoles.Keys) {
					sb.AppendLine($"{key} - {Context.Guild.GetRole(Context.GuildConfig.SelfRoles[key]).Mention}\n");
					reactions.Add(GetEmote(key));
				}
				if (Context.GuildConfig.SelfRoles.Keys.Count > 0)
					embedBuilder.AddField("**Self Roles**", sb.ToString());
				else
					embedBuilder.AddField("**There are no Self Roles**", "ごめんね");

				await message.ModifyAsync(x => x.Embed = embedBuilder.Build());

				//Get the difference set of the message reactions set minus the final reactions set
				List<IEmote> toDelete = new List<IEmote>(message.Reactions.Keys.Except<IEmote>(reactions));
				foreach (IEmote emote in toDelete) {
					await DiscordAPIHelper.DeleteAllReactionsWithEmote(message, emote); //Making this was :CoconaSweat:
				}
			
				//Get the difference set of the final reactions set minus the message reactions set
				List<IEmote> toAdd = new List<IEmote>(reactions.Except<IEmote>(message.Reactions.Keys));
				await message.AddReactionsAsync(toAdd.ToArray());

				//Just because of pride, if the Discord API improves in the future, leave this shit here.
			}		

		}

        // TODO: Maybe automatically do this when adding/removing a self role?
        [Command("selfrole update")]
        public async Task SelfRoleUpdateAsync()
        {
			IUserMessage message = await GetSelfRoleMessage();
            if (message == null)
            {
				await DiscordAPIHelper.ReplyWithError(Context.Message, 
					"Could not find self role message. Perhaps try re-posting it?",
					Context.HttpServerManager.GetIp + "/images/error.png");
                return;
            }
			await UpdateSelfRoleMessage(message);
            
        }
    }
}