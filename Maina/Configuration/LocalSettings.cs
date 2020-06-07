using System.IO;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;

namespace Maina.Configuration
{
    public class LocalSettings
    {
        /// <summary>
        /// Name of the database to access
        /// </summary>
        public string DatabaseName { get; set; } = "Maina";

        /// <summary>
        /// Urls to access database nodes
        /// Must be http(s):// fqdn OR ip:port
        /// </summary>
        public string[] DatabaseUrls { get; set; } = {"http://localhost:8080"};

        /// <summary>
        /// Path to certificate file (.pfx) on disk
        /// </summary>
        public string CertificatePath { get; set; }

        /// <summary>
        /// X509 certificate to authenticate with the database, if required
        /// </summary>
        [JsonIgnore]
        public X509Certificate2 Certificate
            => !string.IsNullOrWhiteSpace(CertificatePath) ? new X509Certificate2(CertificatePath) : null;
    }
}
