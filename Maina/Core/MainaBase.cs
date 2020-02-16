using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Maina.Core.Logging;

namespace Maina.Core
{
    public class MainaBase : ModuleBase<BotContext>
    {
        public async Task<IUserMessage> ReplyAsync(string message, Embed embed = null, bool updateConfig = false,
            bool updateGuild = false)
        {
            await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);
            _ = Task.Run(() => SaveDocuments(updateConfig, updateGuild));
            return await base.ReplyAsync(message, false, embed, null);
        }

        public async Task<IUserMessage> ReplyFile(string path, string message = null)
        {
            if (message != null)
                await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);
            return await Context.Channel.SendFileAsync(path, message);
        }

        public EmbedBuilder CreateEmbed(EmbedColor color)
        {
			return new EmbedBuilder {Color = new Color((uint)color) };
        }

        // TODO: This might be easier/cleaner to use a system to check flags on what was changed by a command
        private void SaveDocuments(bool configChange, bool guildChange)
        {
            if (configChange)
            {
                Logger.LogVerbose("Bot configuration update requested.");
                Context.Database.Save(Context.Config);
            }

            if (guildChange)
            {
                Logger.LogVerbose($"Guild configuration update requested for {Context.GuildConfig.Id}.");
                Context.Database.Save(Context.GuildConfig);
            }

            if (Context.Session.Advanced.HasChanges)
                Logger.LogError("One or more documents were not saved after a command was run");
        }
    }
}