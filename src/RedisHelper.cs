using Jabber;
using Newtonsoft.Json;
using StackExchange.Redis;


namespace jabber
{
    class RedisHelper
    {
        public static void SetData<T>(string key, T data)
        {
            Config.GetString("REDIS_URL_PORT", out string connectionInfo);
            using (var redis = ConnectionMultiplexer.Connect(connectionInfo))
            {
                IDatabase db = redis.GetDatabase();
                db.StringSet(key, JsonConvert.SerializeObject(data));
                redis.Close();
            }
        }

        public static T GetData<T>(string key)
        {
            Config.GetString("REDIS_URL_PORT", out string connectionInfo);
            using (var redis = ConnectionMultiplexer.Connect(connectionInfo))
            {
                try
                {
                    IDatabase db = redis.GetDatabase();
                    var res = db.StringGet(key);

                    redis.Close();
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
}
