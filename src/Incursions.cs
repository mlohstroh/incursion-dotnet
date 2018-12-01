using System;
using ESI.NET.Models.Incursions;
using System.Collections.Generic;
using ESI.NET.Models.Universe;
using System.Threading.Tasks;

namespace Jabber
{
    public static class Incursions
    {
        private const float SecuritySystemThreshold = 0.4f;
        private const int DefaultStagingSystemId = 30004759;

        public static void CheckIncursions()
        {
            // Called by the scheduler
        }

        public static async Task<string> GetDefaultIncursionMessage(this Incursion incursion)
        {
            Constellation constellation = await EsiWrapper.GetConstellation(incursion.ConstellationId);
            string regionName = await constellation.GetRegionName();

            string dotlan = string.Format("http://evemaps.dotlan.net/map/{0}/{1}", regionName, constellation.Name).DotLanSafeString();

            float systemStatus = SecuritySystemThreshold; // awful fallback
            // It should always have at least one infected system
            int jumpCount = -1; // Error
            if(incursion.InfestedSystems.Length > 0)
            {
                int systemId = (int)incursion.InfestedSystems[0];

                int[] jumps = await EsiWrapper.GetJumps(DefaultStagingSystemId, systemId);
                jumpCount = jumps.Length;

                SolarSystem system = await EsiWrapper.GetSystem(systemId);
                systemStatus = (float)system.SecurityStatus;
            }

            // Example String: Arodan {0.3} {Armi - Domain} Influence: 100% - Status mobilizing- 40 jumps from staging - Dotlan: http://evemaps.dotlan.net/map/Domain/Armi
            return string.Format("{0:F1} {{{1} - {2}}} Influence: {3}% - Status {4} - {5} estimated jumps from staging - Dotlan {6}", systemStatus, constellation.Name, regionName, incursion.Influence * 100, incursion.State, jumpCount, dotlan);
        }

        public static async Task<string> GetRegionName(this Constellation constellation)
        {
            Region region = await EsiWrapper.GetRegion((int)constellation.RegionId);
            return region.Name;
        }

        public static string DotLanSafeString(this string s)
        {
             return s.Replace(' ', '_');
        }
    }
}


//// TODO: It feels like this method doesn't belong here since the struct isn't here
//func(server* Server) GetConstellationForIncursion(incursion* EsiIncursion) * EsiConstellation
//{
//	return server.GetConstellation(incursion.ConstellationId)
//}

//func(server* Server) GetNewIncursionMessage(incursion* EsiIncursion, buffer* bytes.Buffer)
//{
//    constellation:= server.GetConstellationForIncursion(incursion)

//    dotlan:= fmt.Sprintf("http://evemaps.dotlan.net/map/%v/%v", constellation.RegionName, constellation.Name)


//    dotlan = strings.Replace(dotlan, " ", "_", -1)

//    // Filter
//    if incursion.StagingSystem.SecurityStatus <= server.Config.SecurityStatusThreshold {
//        buffer.WriteString(fmt.Sprintf("New Incursion detected in %v {%.1v} {%v - %v} - %v jumps from staging - Dotlan: %v\n", incursion.StagingSystem.Name, incursion.StagingSystem.SecurityStatus, incursion.ConsellationName, constellation.RegionName, len(incursion.Route) - 1, dotlan))

//    }
//}

//func(server* Server) GetDefaultIncurionsMessage(incursion* EsiIncursion, buffer* bytes.Buffer)
//{
//    constellation:= server.GetConstellationForIncursion(incursion)

//    dotlan:= fmt.Sprintf("http://evemaps.dotlan.net/map/%v/%v", constellation.RegionName, constellation.Name)

//    dotlan = strings.Replace(dotlan, " ", "_", -1)


//    if incursion.StagingSystem.SecurityStatus <= server.Config.SecurityStatusThreshold {
//        buffer.WriteString(fmt.Sprintf("%v {%.1v} {%v - %v} Influence: %.3v%% - Status %v- %v jumps from staging - Dotlan: %v\n", incursion.StagingSystem.Name, incursion.StagingSystem.SecurityStatus, incursion.ConsellationName, constellation.RegionName, incursion.Influence * 100, incursion.State, len(incursion.Route) - 1, dotlan))

//    }
//}

//func(server* Server) GetChangedIncursionMessage(incursion* EsiIncursion, buffer* bytes.Buffer)
//{
//    constellation:= server.GetConstellationForIncursion(incursion)

//    dotlan:= fmt.Sprintf("http://evemaps.dotlan.net/map/%v/%v", constellation.RegionName, constellation.Name)

//    dotlan = strings.Replace(dotlan, " ", "_", -1)


//    if incursion.StagingSystem.SecurityStatus <= server.Config.SecurityStatusThreshold {
//        buffer.WriteString(fmt.Sprintf("Incursion in %v {%.1v} {%v - %v} Changed status to - Status %v - %v jumps from staging - Dotlan: %v\n", incursion.StagingSystem.Name, incursion.StagingSystem.SecurityStatus, incursion.ConsellationName, constellation.RegionName, incursion.State, len(incursion.Route) - 1, dotlan))

//    }
//}

//func(server* Server) GetDespawnedIncursionMessage(incursion* EsiIncursion, buffer* bytes.Buffer)
//{
//    constellation:= server.GetConstellationForIncursion(incursion)


//    if incursion.StagingSystem.SecurityStatus <= server.Config.SecurityStatusThreshold {
//        buffer.WriteString(fmt.Sprintf("Incursion in %v {%.1v} {%v - %v} Despawned", incursion.StagingSystem.Name, incursion.StagingSystem.SecurityStatus, incursion.ConsellationName, constellation.RegionName))

//    }
//}
