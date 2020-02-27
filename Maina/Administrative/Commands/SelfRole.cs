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
        public async Task SelfRoleAdd(string em, SocketRole role)
        {
			await InternalSelfRoleAdd(em, role, Context.GuildConfig.DefaultSelfRoleMenu);
        }

		[Command("selfrole add")]
        public async Task SelfRoleAdd(string em, SocketRole role, string list)
        {
			if (Context.GuildConfig.SelfRoleMenus.ContainsKey(list)) {
				await InternalSelfRoleAdd(em, role, Context.GuildConfig.SelfRoleMenus[list], list);
			}
			else {
				await DiscordAPIHelper.ReplyWithError(Context.Message, 
					$"There is no {list} selfrole menu.",
					Context.HttpServerManager.GetIp + "/images/error.png");
			}
			
        }

		private async Task InternalSelfRoleAdd (string em, SocketRole role, RoleMenu rm, string list = null) {
			IEmote emote = GetEmote(em);
			if (rm.SelfRoles.ContainsKey(emote.ToString()))
			{
				rm.SelfRoles[emote.ToString()] = role.Id;
				await UpdateSelfRoleMessage(rm, list);
				await ReplyAsync($"Self role for {Emote.Parse(emote.ToString())} changed to `{role.Name}` in {list ?? "default"} menu.",
					updateGuild: true);
			}
			else {
				rm.SelfRoles.Add(emote.ToString(), role.Id);
				await UpdateSelfRoleMessage(rm, list);
				await ReplyAsync($"Added self role {role.Name} - {emote.ToString()} to {list ?? "default"} menu.", updateGuild: true);
			}
		}
			   		 

        [Command("selfrole remove")]
        public async Task SelfRoleRemoveAsync(string em)
        {
            await InternalSelfRoleRemove(em, Context.GuildConfig.DefaultSelfRoleMenu);
        }

		[Command("selfrole remove")]
        public async Task SelfRoleRemoveAsync(string em, string list)
        {
            if (Context.GuildConfig.SelfRoleMenus.ContainsKey(list)) {
				await InternalSelfRoleRemove(em, Context.GuildConfig.SelfRoleMenus[list], list);
			}
			else {
				await DiscordAPIHelper.ReplyWithError(Context.Message, 
					$"There is no {list} selfrole menu.",
					Context.HttpServerManager.GetIp + "/images/error.png");
			}
        }

		private async Task InternalSelfRoleRemove (string em, RoleMenu rm, string list = null) {
			IEmote emote = GetEmote(em);

            if (!rm.SelfRoles.ContainsKey(emote.ToString()))
            {
				await DiscordAPIHelper.ReplyWithError(Context.Message, 
					$"There is not a role assigned to {emote.ToString()} in {list ?? "default"} menu.",
					Context.HttpServerManager.GetIp + "/images/error.png");
                return;
            }

            rm.SelfRoles.Remove(emote.ToString());
			await UpdateSelfRoleMessage(rm, list);
            await ReplyAsync($"Removed self role assigned to {emote.ToString()} in {list ?? "default"} menu.", updateGuild: true);
		}


		[Command("selfrole list")]
        public async Task SelfRoleListAsync()
        {
			await InternalSelfRoleList(Context.GuildConfig.DefaultSelfRoleMenu);
        }

		[Command("selfrole list")]
        public async Task SelfRoleListAsync(string list)
        {
			if (Context.GuildConfig.SelfRoleMenus.ContainsKey(list)) {
				await InternalSelfRoleList(Context.GuildConfig.SelfRoleMenus[list], list);
			}
			else {
				await DiscordAPIHelper.ReplyWithError(Context.Message, 
					$"There is no {list} selfrole menu.",
					Context.HttpServerManager.GetIp + "/images/error.png");
			}
        }

		private async Task InternalSelfRoleList (RoleMenu rm, string list = null) {
			if (rm.SelfRoles.Count == 0)
			{
				await DiscordAPIHelper.ReplyWithError(Context.Message, 
					$"Guild has no self assignable roles in {list ?? "default"} menu.",
					Context.HttpServerManager.GetIp + "/images/error.png");
				return;
			}
            
			if (rm.Channel.HasValue && rm.Message.HasValue) {
				if ((Context.Guild.GetChannel(rm.Channel.Value) is SocketTextChannel channel &&
						await channel.GetMessageAsync(rm.Message.Value) is IUserMessage prevMessage))
				{
					await prevMessage.DeleteAsync();
				}
			}


			EmbedBuilder embedBuilder = CreateEmbed(EmbedColor.SalmonPink);

			
			StringBuilder sb = new StringBuilder();
			foreach (string key in rm.SelfRoles.Keys)
				sb.AppendLine($"{key} - {Context.Guild.GetRole(rm.SelfRoles[key]).Mention}\n");
           
			string title = list ?? "Self Roles";
			embedBuilder.AddField($"**{title}**", sb.ToString());
			IUserMessage message = await ReplyAsync(string.Empty, embedBuilder.Build());

			

			foreach (var em in rm.SelfRoles.Keys)
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
            
			
			rm.Channel = message.Channel.Id;
			rm.Message = message.Id;
			Context.Database.Save(Context.GuildConfig); // Save manually rather than sending another message
			await Context.Message.DeleteAsync(); //It will look more clean if we delete the command message
		}


		[Command("selfrole list create")]
        public async Task SelfRoleListCreateAsync(string list)
        {
			if (Context.GuildConfig.SelfRoleMenus.ContainsKey(list)) {
				await DiscordAPIHelper.ReplyWithError(Context.Message, 
					$"There is already a {list} selfrole menu.",
					Context.HttpServerManager.GetIp + "/images/error.png");
			}
			else {
				Context.GuildConfig.SelfRoleMenus.Add(list, new RoleMenu());
				await ReplyAsync($"Created {list} self role menu.", updateGuild: true);
			}
        }


		[Command("selfrole list delete")]
        public async Task SelfRoleListDeleteAsync(string list)
        {
			if (!Context.GuildConfig.SelfRoleMenus.ContainsKey(list)) {
				await DiscordAPIHelper.ReplyWithError(Context.Message, 
					$"There is no selfrole menu for {list}.",
					Context.HttpServerManager.GetIp + "/images/error.png");
			}
			else {
				Context.GuildConfig.SelfRoleMenus.Remove(list);
				await ReplyAsync($"Deleted self role menu for {list}", updateGuild: true);
			}
        }



		private async Task<IUserMessage> GetSelfRoleMessage (RoleMenu rm) {
			IUserMessage res = null;
			if (rm.Channel.HasValue && rm.Channel.HasValue) {
				if (Context.Guild.GetChannel(rm.Channel.Value) is SocketTextChannel channel &&
					  await channel.GetMessageAsync(rm.Message.Value) is IUserMessage message)
				{
					res = message;
				}
			}
			return res;
		}

		private async Task UpdateSelfRoleMessage (RoleMenu rm, string list) {
			IUserMessage message = await GetSelfRoleMessage(rm);
			if (message != null) {
				EmbedBuilder embedBuilder = CreateEmbed(EmbedColor.SalmonPink);

				StringBuilder sb = new StringBuilder();
				List<IEmote> reactions = new List<IEmote>();
				foreach (string key in rm.SelfRoles.Keys) {
					sb.AppendLine($"{key} - {Context.Guild.GetRole(rm.SelfRoles[key]).Mention}\n");
					reactions.Add(GetEmote(key));
				}
				if (rm.SelfRoles.Keys.Count > 0) {
					string title = list ?? "Self Roles";
					embedBuilder.AddField($"**{title}**", sb.ToString());
				}
				else
					embedBuilder.AddField($"**There are no roles in {list ?? "default"} self role menu**", "ごめんね");

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

        
    }
}