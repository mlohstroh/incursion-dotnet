using System;
using System.Collections;
using System.IO;

namespace Jabber
{
    /// <summary>
    /// Static class that reads from .env files and the basic Environment class
    /// </summary>
    public static class Config
    {
        private static bool _initialized = false;
       
        /// <summary>
        /// Reads in .env files and creates a cache from the environment
        /// </summary>
        private static void Initialize()
        {
            if (_initialized)
                return;

            // From wherever our CWD is
            if(File.Exists(".env"))
            {
                string[] lines = File.ReadAllLines(".env");
                
                for(int i = 0; i < lines.Length; i++)
                {
                    string trimmed = lines[i].Trim();

                    if (trimmed.Length == 0)
                        continue;

                    // Is this a comment line?
                    if (trimmed[0] == '#')
                        continue;

                    var tuple = GetEnvPairFromLine(lines[i]);

                    if(tuple == null)
                    {
                        // TOOD: Nice logs
                        Console.WriteLine("[Error] Malformed .env file at line {0}. Stopping read", i + 1);
                        Console.WriteLine("Line Content: {0}", lines[i]);
                        _initialized = true;
                        return;
                    }

                    Environment.SetEnvironmentVariable(tuple.Item1, tuple.Item2);
                }
            }
        }

        private static Tuple<string, string> GetEnvPairFromLine(string line)
        {
            var split = line.Split("=");

            if(split.Length != 2)
            {
                return null;
            }

            return new Tuple<string, string>(split[0], split[1]);
        }

        public static bool GetInt(string key, out int val)
        {
            Initialize();

            val = 0;
            string found = Environment.GetEnvironmentVariable(key);

            if (found == null)
                return false;

            val = Convert.ToInt32(found);

            return true;
        }

        public static bool GetString(string key, out string val)
        {
            Initialize();

            val = Environment.GetEnvironmentVariable(key);

            return val != null;
        }

        public static bool GetFloat(string key, out float val)
        {
            Initialize();

            val = 0;
            string found = Environment.GetEnvironmentVariable(key);

            if (found == null)
                return false;

            val = float.Parse(found);

            return true;
        }
    }
}