using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Maina.Core;
using Maina.Core.Logging;

namespace Maina.Administrative.Commands
{
    [RequireBotPermission(GuildPermission.ManageRoles), RequireUserPermission(GuildPermission.ManageRoles)]
    public class SelfRole : MainaBase
    {
        [Command("selfrole add")]
        public Task SelfRoleAdd(string s, SocketRole role)
        {
            var emote = new Emoji(s);

            if (Context.GuildConfig.SelfRoles.ContainsKey(emote.Name))
            {
                Context.GuildConfig.SelfRoles[emote.Name] = role.Id;
                return ReplyAsync($"Self role for {Emote.Parse(emote.Name)} changed to `{role.Name}`",
                    updateGuild: true);
            }

            Context.GuildConfig.SelfRoles.Add(emote.Name, role.Id);
            return ReplyAsync($"Added self role {role.Name} - {emote.Name}", updateGuild: true);
        }

        [Command("selfrole list")]
        public async Task SelfRoleListAsync()
        {
            if (Context.GuildConfig.SelfRoles.Count == 0)
            {
                await ReplyAsync("Guild has no self assignable roles");
                return;
            }
            
            var embedBuilder = CreateEmbed(EmbedColor.Aqua);

            var selfRoleList = Context.GuildConfig.SelfRoles.Aggregate(string.Empty,
                (current, selfRole) =>
                    current + $"{selfRole.Key} - {Context.Guild.GetRole(selfRole.Value).Mention}\n");

            embedBuilder.AddField("**Self Roles**", selfRoleList);
            var message = await ReplyAsync(string.Empty, embedBuilder.Build());

			

            foreach (var em in Context.GuildConfig.SelfRoles.Keys)
            {
				try {
					IEmote res = null;
					Emote emote = null;
					if (Emote.TryParse(em, out emote))
						res = emote;
					else
						res = new Emoji(em);
					await message.AddReactionAsync(res);
				}
				catch (Exception ex) {
					Logger.LogException(ex);
					throw ex;
				}
            }
            
            Context.GuildConfig.SelfRoleMenu = new[] {message.Channel.Id, message.Id};
            Context.Database.Save(Context.GuildConfig); // Save manually rather than sending another message
        }

        [Command("selfrole remove")]
        public async Task SelfRoleRemoveAsync(string s)
        {
            var emote = new Emoji(s);

            if (!Context.GuildConfig.SelfRoles.ContainsKey(emote.Name))
            {
                await ReplyAsync($"There is not a role assigned to {emote}");
                return;
            }

            Context.GuildConfig.SelfRoles.Remove(emote.Name);
            await ReplyAsync($"Removed self role assigned to {emote.Name}", updateGuild: true);
        }

        // TODO: Maybe automatically do this when adding/removing a self role?
        [Command("selfrole update")]
        public async Task SelfRoleUpdateAsync()
        {
            if (!(Context.Guild.GetChannel(Context.GuildConfig.SelfRoleMenu[0]) is SocketTextChannel channel &&
                  await channel.GetMessageAsync(Context.GuildConfig.SelfRoleMenu[1]) is IUserMessage message))
            {
                await ReplyAsync("Could not find self role message. Perhaps try re-posting it?");
                return;
            }

            var embedBuilder = CreateEmbed(EmbedColor.Aqua);

            var selfRoleList = Context.GuildConfig.SelfRoles.Aggregate(string.Empty,
                (current, selfRole) =>
                    current + $"{selfRole.Key} - {Context.Guild.GetRole(selfRole.Value).Mention}\n");

            embedBuilder.AddField("**Self Roles**", selfRoleList);

            await message.ModifyAsync(x => x.Embed = embedBuilder.Build());
        }
    }
}