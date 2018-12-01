using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using ESI.NET;
using ESI.NET.Enumerations;
using ESI.NET.Models.Incursions;
using System.Threading.Tasks;
using ESI.NET.Models.Universe;
using System.Linq;
using System.Net;

namespace Jabber
{
    public static class EsiWrapper
    {
        private const long IncursionCacheTime = 600; // seconds
        private static EsiClient s_client;
        private static List<Incursion> s_incursions = new List<Incursion>();
        private static DateTime s_lastIncursionPoll = DateTime.MinValue;
        private static Dictionary<int, ResolvedInfo> s_cachedNames = new Dictionary<int, ResolvedInfo>();
        private static Dictionary<int, Constellation> s_cachedConstellations = new Dictionary<int, Constellation>();
        private static Dictionary<int, SolarSystem> s_cachedSystems = new Dictionary<int, SolarSystem>();

        public static void EnsureInit()
        {
            if(s_client == null)
            {
                try
                {
                    IOptions<EsiConfig> options = Options.Create<EsiConfig>(new EsiConfig()
                    {
                        EsiUrl = "https://esi.tech.ccp.is/",
                        DataSource = DataSource.Tranquility,
                        UserAgent = "goons-incursion-jabber-bot"
                    });

                    s_client = new EsiClient(options);

                    LoadUpCachedData();
                }
                catch(Exception ex)
                {
                    Console.WriteLine("[Fatal] Exception Configuring ESI Client. Exception: {0}", ex);
                    Environment.Exit(1);
                }
            }
        }

        private static void LoadUpCachedData()
        {
            // Load data from redis...
        }

        public static async Task<List<Incursion>> GetIncursions()
        {
            EnsureInit();

            if ((DateTime.UtcNow - s_lastIncursionPoll).TotalSeconds < IncursionCacheTime)
            {
                return s_incursions;
            }

            EsiResponse<List<Incursion>> incursionResponse = await s_client.Incursions.All();

            if(incursionResponse.StatusCode != HttpStatusCode.OK)
            {
                // Note: not sure if this message is the correct property
                Console.WriteLine("[Error] Unable to query ESI for the lastest incursions. Status: {0} Error {1}", incursionResponse.StatusCode, incursionResponse.Message);
                return s_incursions;
            }

            return incursionResponse.Data;
        }

        public static async Task<List<ResolvedInfo>> GetNames(List<int> ids)
        {
            EnsureInit();

            ids = ids.Distinct().ToList();

            // Why does this method require longs, but return models with ints!!!?!?!?!??!
            // TOOD: file an issue, or just patch it
            List<long> intsToLongs = ids.Select(x => (long)x).ToList();

            EsiResponse<List<ResolvedInfo>> response = await s_client.Universe.Names(intsToLongs);

            if(response.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine("[Error] Unable to query ESI for names. Status: {0} Error {1}", response.StatusCode, response.Message);
                return null; // Proper response for cannot resolve?
            }

            var infos = response.Data;
            for(int i = 0; i < infos.Count; i++)
            {
                s_cachedNames.Add(infos[i].Id, infos[i]);
                RedisHelper.Set<ResolvedInfo>(string.Format("esi:names:{0}", infos[i].Id), infos[i]);
            }

            return infos;
        }

        public static ResolvedInfo GetNameFromId(int id)
        {
            EnsureInit();

            ResolvedInfo info;

            // Check in memory cache
            if (s_cachedNames.TryGetValue(id, out info))
            {
                return info;
            }

            // Check persistent redis
            info = RedisHelper.Get<ResolvedInfo>(string.Format("esi:names:{0}", id));

            if(info != null)
            {
                s_cachedNames[id] = info;
            }

            return info;
        }

        public static async Task<Constellation> GetConstellation(int id)
        {
            EnsureInit();

            Constellation constellation;
            if(s_cachedConstellations.TryGetValue(id, out constellation))
            {
                return constellation;
            }

            constellation = RedisHelper.Get<Constellation>(string.Format("esi:constellations:{0}", id));

            if(constellation != null)
            {
                return constellation;
            }

            // Call ESI
            EsiResponse<Constellation> response = await s_client.Universe.Constellation(id);

            if(response.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine("[Error] Unable to query ESI for constellation. Status: {0} Error {1}", response.StatusCode, response.Message);
                return null;
            }

            s_cachedConstellations[id] = response.Data;
            RedisHelper.Set<Constellation>(string.Format("esi:constellation:{0}", id), response.Data);

            return response.Data;
        }

        public static async Task<SolarSystem> GetSystem(int id)
        {
            EnsureInit();

            // TODO: I believe this can probably be simplified through some template magic
            SolarSystem system;
            if(s_cachedSystems.TryGetValue(id, out system))
            {
                return system;
            }

            system = RedisHelper.Get<SolarSystem>(string.Format("esi:systems:{0}", id));

            if(system != null)
            {
                return system;
            }

            EsiResponse<SolarSystem> response = await s_client.Universe.System(id);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine("[Error] Unable to query ESI for systems. Status: {0} Error {1}", response.StatusCode, response.Message);
                return null;
            }

            s_cachedSystems[id] = response.Data;
            RedisHelper.Set<SolarSystem>(string.Format("esi:systems:{0}", id), response.Data);

            return response.Data;
        }

        public static async Task<int[]> GetJumps(int src, int dst)
        {
            EnsureInit();


            int[] route = RedisHelper.Get<int[]>(string.Format("esi:jumps:{0}:{1}", src, dst));

            if(route != null)
            {
                return route;
            }

            EsiResponse<int[]> response = await s_client.Routes.Map(src, dst);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine("[Error] Unable to query ESI for a route. Status: {0} Error {1}", response.StatusCode, response.Message);
                return null;
            }

            RedisHelper.Set<int[]>(string.Format("esi:jumps:{0}:{1}", src, dst), response.Data);

            return response.Data;
        }

        public static async Task<Region> GetRegion(int id)
        {
            Region region = RedisHelper.Get<Region>(string.Format("esi:regions:{0}", id));

            if (region != null)
            {
                return region;
            }

            EsiResponse<Region> response = await s_client.Universe.Region(id);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine("[Error] Unable to query ESI for a region. Status: {0} Error {1}", response.StatusCode, response.Message);
                return null;
            }

            RedisHelper.Set<Region>(string.Format("esi:regions:{0}", id), response.Data);

            return response.Data;
        }
    }
}
