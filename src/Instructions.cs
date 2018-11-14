using System;

namespace Jabber
{
    /// <summary>
    /// Basic Instructions 
    /// </summary>
    public class Instructions
    {
        private const string WaitlistRedisKey = "waitlist:instructions";

        public static Instructions Get()
        {
            return RedisHelper.Get<Instructions>(WaitlistRedisKey);
        }

        public string Text { get; set; }
        public DateTime SetAt { get; set; }
        public string SetBy { get; set; }

        public void Set()
        {
            RedisHelper.Set<Instructions>(WaitlistRedisKey, this);
        }

        public override string ToString()
        {
            return string.Format("{0}\nSet by: {1} @ {2}", Text, SetBy, SetAt);
        }
    }
}
