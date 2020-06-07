using System.IO;
using Newtonsoft.Json;

namespace Maina.Configuration
{
    public static class SettingsLoader
    {
        public static LocalSettings Load()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "settings.json");
            if (!File.Exists(path))
                File.WriteAllText(path, JsonConvert.SerializeObject(new LocalSettings(), Formatting.Indented));

            return JsonConvert.DeserializeObject<LocalSettings>(File.ReadAllText(path));
        }
    }
}
