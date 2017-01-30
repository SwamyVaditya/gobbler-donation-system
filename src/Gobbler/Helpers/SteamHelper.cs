using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Gobbler.Helpers
{
    public class SteamHelper
    {
        public static bool IsSteamIdValid( string steamid )
        {
            return Regex.Match( steamid, @"^STEAM_[0-5]:[01]:\d{1,15}$", RegexOptions.IgnoreCase )
                .Success;
        }
    }
}
