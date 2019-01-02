using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace jabber
{
    class EsiScopes
    {
        const string WaitlistRedisKey = "waitlist:scopes";

        [JsonProperty]
        List<string> m_squadScopes;

        public static EsiScopes Get()
        {
            var m_esiScopes = Jabber.RedisHelper.Get<EsiScopes>(WaitlistRedisKey);

            if (m_esiScopes == null)
            {
                m_esiScopes = new EsiScopes();
            }

            return m_esiScopes;
        }

        public void Set()
        {
            Jabber.RedisHelper.Set<EsiScopes>(WaitlistRedisKey, this);
        }

        public string Status()
        {
            try
            {
                using (WebClient wc = new WebClient())
                {
                    string EsiStatusUrl = "https://esi.evetech.net/status.json?version=latest";

                    var esi_status = wc.DownloadString(EsiStatusUrl);
                    EsiScope[] scopes = JsonConvert.DeserializeObject<EsiScope[]>(esi_status);
                    if (m_squadScopes == null)
                        m_squadScopes = new List<string>();
                    
                    int green = 0; int yellow = 0; int red = 0;
                    // Scopes used by the squad
                    string m_degradedScopes = "";
                    foreach (EsiScope scope in scopes)
                    {
                        if(scope.status == "green")
                        {
                            green++;
                        } 
                        else if(scope.status == "yellow")
                        {
                            yellow++;
                            if (m_squadScopes.Contains(scope.endpoint))
                                m_degradedScopes += scope.endpoint + " ";
                        }
                        else
                        {
                            red++;
                            if (m_squadScopes.Contains(scope.endpoint))
                                m_degradedScopes += scope.endpoint + " ";
                        }
                    }

                    return string.Format("ESI Status - {0}\nGreen: {1} | Yellow: {2} | Red: {3} - Degraded scopes used by the squad: {4}", EsiStatusUrl, green.ToString(), yellow.ToString(), red.ToString(), m_degradedScopes);
                }
            }
            catch (Exception e)
            {
                return string.Format("Error getting ESI Status information.");
            }
            
        }

        /// <summary>
        /// Define the ESI endpoints used by squad IT tools.
        /// </summary>
        /// <param name="scopes">Comma separated string of ESI scopes</param>
        public string SetScopes(string scopes)
        {
            // Clear the old list of scopes.
            m_squadScopes.Clear();

            // Create a list of scopes
            // Use comma separated user input
            string[] parts = scopes.Split(",");
            foreach (string scope in parts)
                m_squadScopes.Add(scope.Trim().ToLower());
            
            // Save changes
            this.Set();

            return string.Format("Scopes updated.");
        }
    }

    class EsiScope
    {
        public string endpoint { get; set; }
        public string method { get; set; }
        public string route { get; set; }
        public string status { get; set; }
        public string[] tags { get; set; }
    }
}
