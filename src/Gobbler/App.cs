using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Gobbler.Models;
using NLog;

namespace Gobbler
{
    class App
    {
        static readonly ILogger log = LogManager.GetCurrentClassLogger();

        static void Main( string[] args )
        {
            log.Debug( "Starting Gobbler..." );

            var cfg = AppConfig.FromFile( "../config.json" );

            try {
                new ProcessDonationsCmd( cfg ).Execute();
            }
            catch ( Exception ex ) {
                log.Fatal( ex );
                throw;
            }
        }
    }
}
