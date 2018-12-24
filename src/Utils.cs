using Jabber;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace jabber
{
    public static class Utils
    {
        // Get a constellations region name.
        public static async Task<string> GetRegionName(this ESI.NET.Models.Universe.Constellation constellation)
        {
            ESI.NET.Models.Universe.Region region = await EsiWrapper.GetRegion((int)constellation.RegionId);
            return region.Name;
        }

        // Prepare dotlan links
        public static string DotlanSafe(this string link)
        {
            return link.Replace(' ', '_');
        }
    }
}
