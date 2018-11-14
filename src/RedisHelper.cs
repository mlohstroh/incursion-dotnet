using System;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Jabber
{
    class RedisHelper
    {
        private const string LocalRedisUrl = "localhost:6379";

        private static ConnectionMultiplexer s_connection;

        private static void EnsureConnect()
        {
            if(!Config.GetString("REDIS_URL_PORT", out string connectionInfo))
            {
                connectionInfo = LocalRedisUrl;
            }

            s_connection = ConnectionMultiplexer.Connect(connectionInfo);
        }

        private static void Cleanup()
        {
            if(s_connection != null && s_connection.IsConnected)
            {
                s_connection.Close(false);
                s_connection = null;
            }
        }

        public static void Set<T>(string key, T data)
        {
            try
            {
                EnsureConnect();
                IDatabase db = s_connection.GetDatabase();
                db.StringSet(key, JsonConvert.SerializeObject(data));
            }
            catch(Exception ex)
            {
                Console.WriteLine("[Error] Exception setting data inside redis. {0}", ex);
            }
        }

        public static T Get<T>(string key)
        {
            try
            {
                EnsureConnect();
                IDatabase db = s_connection.GetDatabase();
                var res = db.StringGet(key);

                if (res.IsNull)
                    return default(T);
                else
                    return JsonConvert.DeserializeObject<T>(res);
            }
            catch
            {
                return default(T);
            }
        }
    }
}
