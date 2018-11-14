using System;
using System.Collections.Generic;
using System.Text;

namespace jabber.src
{
    class Users
    {
        private static List<User> usersList;

        protected static List<User> UsersList { get => usersList; set => usersList = value; }

        public static List<User> getAdmins()
        {
            List<User> admins = new List<User>();
            foreach (User user in UsersList)
            {
                if (user.Role == "Admin")
                    admins.Add(user);
            }

            return admins;
        }

        public static List<User> getManagers()
        {
            List<User> managers = new List<User>();
            foreach (User user in UsersList)
            {
                if (user.Role == "Manager")
                    managers.Add(user);
            }

            return managers;
        }


        public static List<User> getUsers()
        {
            return UsersList;
        }
    }

    internal class User
    {
        private string id;
        private string name;
        private string role;
        private string type;

        public User(string id, string name, string role, string type)
        {
            this.Id = id;
            this.Name = name;
            this.Role = role;
            this.Type = type;
        }

        public string Id { get => id; set => id = value; }
        public string Name { get => name; set => name = value; }
        public string Role { get => role; set => role = value; }
        public string Type { get => type; set => type = value; }
    }
}
