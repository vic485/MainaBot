using System.IO;
using System.Threading.Tasks;
using Discord.Commands;
using Maina.Core;
using Maina.Core.Logging;
using Raven.Client.Documents.Operations.Backups;

namespace Maina.Owner.Commands
{
    [RequireOwner, Name("Owner")]
    public class BackupDb : MainaBase
    {
        [Command("backup")]
        public async Task RunBackup()
        {
            var localFolder = Path.Combine(Directory.GetCurrentDirectory(), "raven-bkp");
            if (!Directory.Exists(localFolder))
                Directory.CreateDirectory(localFolder);

            var config = new PeriodicBackupConfiguration
            {
                Name = "Backup",
                BackupType = BackupType.Backup,
                FullBackupFrequency = "*/10 * * * *",
                IncrementalBackupFrequency = "0 2 * * *",
                LocalSettings = new LocalSettings { FolderPath = localFolder }
            };
            var operation = new UpdatePeriodicBackupOperation(config);
            var result = await Context.Database.Store.Maintenance.SendAsync(operation);

            await Context.Database.Store.Maintenance.SendAsync(new StartBackupOperation(true, result.TaskId));
            await ReplyAsync($"Database backed up to {Path.Combine(Directory.GetCurrentDirectory(), "raven-bkp")}");
        }
    }
}
