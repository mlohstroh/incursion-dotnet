using System;
using ESI.NET.Models.Incursions;
using System.Collections.Generic;
using ESI.NET.Models.Universe;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Jabber;
using System.Linq;
using jabber;

namespace Jabber
{
    public class Incursions
    {
        private const string WaitlistRedisKey = "waitlist:incursions";
        private const string broadcastChannel = "incursion_bot_testing@conference.goonfleet.com";
        
        //New Vars
        [JsonProperty]
        private Dictionary<int, IncursionFocus> m_activeIncursions = new Dictionary<int, IncursionFocus>();
        [JsonProperty]
        private DateTime m_lastChecked;//Timestamp of the last ESI check! Don't do the ESI check if we did it in the last 5 minutes.

        public static Incursions Get()
        {
            var m_incursions = Jabber.RedisHelper.Get<Incursions>(WaitlistRedisKey);

            if (m_incursions == null)
            {
                m_incursions = new Incursions();
            }

            return m_incursions;
        }

        public void Set()
        {
            Jabber.RedisHelper.Set<Incursions>(WaitlistRedisKey, this);
        }

        public async Task CheckIncursions()
        {
            // If we haven't checked incursions in the last five minutes
            // run the UpdateIncursions method.
            if(true)
            {
                await UpdateIncursionsAsync();
                //m_lastChecked = DateTime.UtcNow;
                //this.Set();
            }

        }

        /// <summary>
        /// Gets incursions using ESI and updates our list.
        /// </summary>
        public async Task UpdateIncursionsAsync()
        {
            //ESI Lookup
            List<Incursion> incursions = await EsiWrapper.GetIncursions();
            Config.GetFloat("SEC_STATUS_CEILING", out float max_sec);

            foreach (var Incursion in incursions)
            {
                if(m_activeIncursions.ContainsKey(Incursion.ConstellationId))
                {
                    IncursionFocus inc = m_activeIncursions[Incursion.ConstellationId];

                    //Update Influence.
                    inc.Influence = Incursion.Influence;

                    //Status
                    if(inc.State != Incursion.State)
                    {
                        inc.State = Incursion.State;
                        await JabberClient.Instance.SendGroupMessage(broadcastChannel,
                            string.Format("{0} incursion in {1} (Region: {2}) is now {3}.", inc.GetSecType(), inc.Constellation.Name, inc.RegionName, Incursion.State)
                        );
                    }

                    //Boss spotted?
                    if(!inc.HasBoss && Incursion.HasBoss)
                    {
                        inc.HasBoss = Incursion.HasBoss;
                        await JabberClient.Instance.SendGroupMessage(broadcastChannel,
                            string.Format("{0} incursion in {1} (Region: {2}) - The mothership has been spotted!", inc.GetSecType(), inc.Constellation.Name, inc.RegionName, Incursion.State)
                        );
                    }
                }
                else
                {
                    IncursionFocus new_incursion = new IncursionFocus()
                    {
                        HasBoss = Incursion.HasBoss,
                        Influence = Incursion.Influence,
                        State = Incursion.State,
                    };

                    await new_incursion.SetConstellation(Incursion.ConstellationId);
                    await new_incursion.SetInfestedSystems(Incursion.InfestedSystems);
                    
                    //Store Incursion
                    m_activeIncursions.Add(new_incursion.Constellation.Id, new_incursion);

                    if (Math.Round(IncursionFocus.GetTrueSec(new_incursion.GetSecStatus()), 1) < max_sec)
                    {
                        await JabberClient.Instance.SendGroupMessage(broadcastChannel,
                            string.Format("New {0} Incursion Detected {1} (Region: {2}) - {3} estimated jumps from staging - {4}",
                               new_incursion.GetSecType().ToLower(), new_incursion.Constellation.Name, new_incursion.RegionName, await new_incursion.GetDistanceFromStaging(), new_incursion.Dotlan())
                        );
                    }
                }               
            }

            //Compare incursions and look for changes. Push strings to a list of changes.
        }

        /// <summary>
        /// Returns a list of incursions.
        /// </summary>
        public override string ToString()
        {
            if (m_activeIncursions.Count == 0)
                return "No incursions found!";

            Config.GetFloat("SEC_STATUS_CEILING", out float max_sec);

            string incursions = "";
            foreach(KeyValuePair<int, IncursionFocus> inc in m_activeIncursions)
            {
                if(Math.Round(IncursionFocus.GetTrueSec(inc.Value.GetSecStatus()), 1) < max_sec)
                    incursions += string.Format("\n{0}", inc.Value.ToString());
            }

            return incursions;
        }
    }
}

[Serializable]
public class IncursionFocus
{
    private const int DefaultStagingSystemId = 30004759;

    [JsonProperty]
    private Constellation m_constellation;
    [JsonProperty]
    private string m_regionName;
    [JsonProperty]
    private bool m_hasBoss;
    [JsonProperty]
    private double m_influence;
    [JsonProperty]
    private string m_state;
    [JsonProperty]
    private Dictionary<long, SolarSystem> infestedSystems;


    public Constellation Constellation => m_constellation;
    public string RegionName => m_regionName;
    public bool HasBoss { get; set; }
    public double Influence { get; set; }
    public string State { get; set; }
    public Dictionary<long, SolarSystem> InfestedSystems { get; set; }

    public async Task SetConstellation(int constellationID)
    {
        m_constellation = await EsiWrapper.GetConstellation(constellationID);
        m_regionName = await Constellation.GetRegionName();
    }

    public async Task SetInfestedSystems(long[] systemIDs)
    {
        if (InfestedSystems == null)
            InfestedSystems = new Dictionary<long, SolarSystem>();

        foreach (long systemID in systemIDs)
        {
            if (InfestedSystems.ContainsKey(systemID))
            {
                InfestedSystems[systemID] = await EsiWrapper.GetSystem((int)systemID);
            }
            else
            {
                InfestedSystems.Add(systemID, await EsiWrapper.GetSystem((int)systemID));
            }
        }
    }

    /// <summary>
    /// Returns the Sec status of one system.
    /// </summary>
    /// <returns>Float ranging from -1.0 to 1.0. Returns -10 for an unknown sec status</returns>
    public float GetSecStatus()
    {
        if (InfestedSystems.Count > 0)
            return (float)InfestedSystems.First().Value.SecurityStatus;

        return (float)-10.0;
    }

    /// <summary>
    /// Returns the security type of the system 
    /// from the GetSecStatus() method.
    /// </summary>
    /// <returns></returns>
    public string GetSecType()
    {
        float security_status = GetTrueSec(GetSecStatus());
        if (Math.Round(security_status, 1) >= 0.5)
        {
            return "Highsec";
        }
        else if (Math.Round(security_status, 1) <= 0.4 && Math.Round(security_status, 1) >= 0.1)
        {
            return "Lowsec";
        }

        return "Nullsec";
    }

    public async Task<int> GetDistanceFromStaging()
    {
        if (InfestedSystems.Count > 0)
        {
            int[] jumps = await EsiWrapper.GetJumps(DefaultStagingSystemId, InfestedSystems.First().Value.SystemId);
            return jumps.Length;
        }

        return 0;
    }

    public static float GetTrueSec(float SecurityStatus)
    {
        if(SecurityStatus > 0.0 || SecurityStatus < 0.05)
        {
            float temp = (float)Math.Ceiling(SecurityStatus * 100);
            return (float)temp / 100;
            
        }

        return (float)Math.Round(SecurityStatus, 2);
    }

    public string Dotlan()
    {
        return string.Format("http://evemaps.dotlan.net/map/{0}/{1}", RegionName, Constellation.Name).DotlanSafe();
    }

    public override string ToString()
    {
        return string.Format("{0} incursion in {1} (Region: {2}) {3}: {4:0.0}%  influence - {5} est. jumps from staging - {6}", GetSecType(), Constellation.Name, RegionName, State, 100 - (Influence * 100), GetDistanceFromStaging().Result, Dotlan());
    }
}