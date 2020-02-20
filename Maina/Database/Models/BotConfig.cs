namespace Maina.Database.Models
{
    /// <summary>
    /// Shared bot configuration settings
    /// </summary>
    public class BotConfig : DatabaseItem
    {
        /// <summary>
        /// Token to connect to discord
        /// </summary>
        public string Token { get; set; }
        
        /// <summary>
        /// Default prefix for commands
        /// </summary>
        public string Prefix { get; set; }



		public string SecretToken { get; set; }
    }
}