namespace Maina.Core.Logging
{
    public enum LogType
    {
        /// <summary>
        /// Debugging level messages
        /// </summary>
        Debug = 0,
        /// <summary>
        /// Program flow messages
        /// </summary>
        Verbose = 1,
        /// <summary>
        /// Informational messages about program actions
        /// </summary>
        Info = 2,
        /// <summary>
        /// Warning messages, less severe than errors but too many can indicate problems. No serious effect on performance
        /// </summary>
        Warning = 3,
        /// <summary>
        /// Error messages, more severe. Impacting performance of the bot
        /// </summary>
        Error = 4,
        /// <summary>
        /// Most severe errors. Main process will most likely exit after
        /// </summary>
        Critical = 5,
        /// <summary>
        /// Messages that must be output no matter what. Typically for times human interaction is required.
        /// </summary>
        Force = 6
    }
}