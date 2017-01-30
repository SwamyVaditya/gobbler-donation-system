using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gobbler.Helpers;
using Newtonsoft.Json;

namespace Gobbler.Models
{
    internal class AppConfig
    {
        public MySqlConnectionInfo MySql { get; set; }
        public PayPalSettings PayPal { get; set; }
        public ImapSettings Gmail { get; set; }
        public TransactionParserSettings TransactionParser { get; set; }

        public static AppConfig FromFile( string file )
        {
            var contents = File.ReadAllText( file );
            return JsonConvert.DeserializeObject<AppConfig>( contents );
        }

        internal class MySqlConnectionInfo
        {
            public string ConnectionString { get; set; }
        }

        internal class PayPalSettings
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public string Signature { get; set; }
            public string Mode { get; set; }
        }

        internal class TransactionParserSettings
        {
            public string[] FilterCodes { get; set; }
        }
    }
}
