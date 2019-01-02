using Jabber;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace jabber
{
    /// <summary>
    /// A collection of users with permission to use special commands
    /// Owners can manage users.
    /// </summary>
    class Users
    {
        private const string WaitlistRedisKey = "waitlist:users";

        [JsonProperty]
        private Dictionary<string, User> m_usersList = new Dictionary<string, User>();


        public static Users Get()
        {
            var users = Jabber.RedisHelper.Get<Users>(WaitlistRedisKey);

            if(users == null)
            {
                users = new Users();
            }

            return users;
        }

        public void Set()
        {
            Jabber.RedisHelper.Set<Users>(WaitlistRedisKey, this);
        }

        /// <summary>
        /// Checks to see if a specific jabber resource has the permission to
        /// complete a task.
        /// </summary>
        /// <param name="jabber_resource">Example: samuel_the_terrible</param>
        /// <param name="requires_admin"></param>
        /// <returns> Boolean indicating if the user has permission.</returns>
        public bool CheckUser(string jabber_resource, bool requires_admin)
        {
            Config.GetString("JABBER_USERNAME", out string sudo_username);
            if (jabber_resource == sudo_username)
                return true;


            if (m_usersList.ContainsKey(jabber_resource.Trim()))
            {
                if (requires_admin == false || requires_admin == true && m_usersList[jabber_resource].Role == "Admin")
                    return true;
            }            

            // User not found
            return false;
        }


        public string ListAll()
        {
            string output = "";

            foreach (KeyValuePair<string, User> u in m_usersList)
            {
                output += string.Format("\n{0} - {1}", u.Value.JabberResource, u.Value.Role);
            }
        
            return  output;
        }

        /// <summary>
        /// Adds a new user to the ACL.
        /// </summary>
        /// <param name="jabber_resource">Example: samuel_the_terrible</param>
        /// <param name="is_admin"></param>
        public string AddUser(string jabber_resource, bool is_admin)
        {
            if(m_usersList == null)
            {
                m_usersList = new Dictionary<string, User>();
            }

            if(!m_usersList.ContainsKey(jabber_resource))
            {
                string role = (is_admin) ? "Admin" : "User";

                User new_user = new User
                {
                    JabberResource = jabber_resource,
                    Role = role
                };

                m_usersList.Add(jabber_resource, new_user);
                return string.Format("{0} has been added to the ACL with the role: {1}.", jabber_resource, role.ToLower());
            } else
            {
                return string.Format("{0} is already on the ACL as a {1}. If you wish to change their permission you must remove them first!", jabber_resource, m_usersList[jabber_resource].Role);
            }
        }

        /// <summary>
        /// Removes a user from the ACL.
        /// </summary>
        /// <param name="jabber_resource">Example: samuel_the_terrible</param>
        public string RemoveUser(string jabber_resource)
        {
            if(m_usersList.ContainsKey(jabber_resource))
            {
                m_usersList.Remove(jabber_resource);
                return string.Format("{0} has been removed.", jabber_resource);
            }
            else
            {
                return string.Format("{0} was not a listed user and could not be removed.", jabber_resource);
            }
        }
    }

    internal class User
    {
        public string JabberResource { get; set; }
        public string Role { get; set; }
    }
  
}
