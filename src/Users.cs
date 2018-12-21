using System;
using System.Collections.Generic;


namespace jabber
{
    /// <summary>
    /// A collection of users with permission to use special commands
    /// Owners can manage users.
    /// </summary>
    class Users
    {
        private const string WaitlistRedisKey = "waitlist:users";
        private Dictionary<string, User> usersList;


        public static Dictionary<string, User> Get()
        {
            return Jabber.RedisHelper.Get<Dictionary<string, User>>(WaitlistRedisKey);
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
            usersList = Users.Get();

            if (usersList.ContainsKey(jabber_resource.Trim()))
            {
                if (requires_admin && usersList[jabber_resource].Role != "Admin")
                {
                    return false;
                }

                return true;   
            }            

            // User not found
            return false;
        }

        /// <summary>
        /// Adds a new user
        /// </summary>
        /// <param name="jabber_resource">Example: samuel_the_terrible</param>
        /// <param name="is_admin"></param>
        public void AddUser(string jabber_resource, bool is_admin)
        {

            usersList = Users.Get();
            
            if(usersList == null)
            {
                usersList = new Dictionary<string, User>();
            }

            if(!usersList.ContainsKey(jabber_resource))
            {
                string role = (is_admin) ? "Admin" : "User";
                usersList.Add(jabber_resource, new User(jabber_resource, role));
            }
        }
    }

    internal class User
    {
        private string jabber_resource;
        private string role;

        public User(string jabber_resource, string role)
        {
            this.Jabber_resource = jabber_resource;
            this.Role = role;
        }

        public string Jabber_resource { get => jabber_resource; set => jabber_resource = value; }
        public string Role { get => role; set => role = value; }
    }
}
