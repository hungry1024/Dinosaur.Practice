using StackExchange.Redis;
using System;

namespace Dinosaur.Distributed.Infrastructure
{
    internal class RedisProxy
    {
        private static string ConnectionString;

        private static readonly object s_locker = new object();

        private static ConnectionMultiplexer s_multiplexer = null;

        private static ConnectionMultiplexer Connect()
        {
            if (s_multiplexer == null)
            {
                lock (s_locker)
                {
                    if (s_multiplexer == null)
                    {
                        var options = ConfigurationOptions.Parse(ConnectionString);
                        options.AllowAdmin = true;
                        s_multiplexer = ConnectionMultiplexer.Connect(options);
                    }
                }
            }

            return s_multiplexer;
        }

        public static void Initialize(string redisConnectionString)
        {
            if (string.IsNullOrEmpty(redisConnectionString))
            {
                throw new ArgumentNullException(nameof(redisConnectionString));
            }

            if (string.IsNullOrEmpty(ConnectionString))
            {
                ConnectionString = redisConnectionString;
                Connect();
            }
        }

        public static IDatabase Database => Connect().GetDatabase();
    }

    internal static class RedisDatabaseExtension
    {
        public static RedisValue StringGetOrAdd(this IDatabase database, RedisKey key, Func<RedisKey, RedisValue> aquire)
        {
            return database.StringGetOrAdd(key, aquire, TimeSpan.MaxValue);
        }

        public static RedisValue StringGetOrAdd(this IDatabase database, RedisKey key, Func<RedisKey, RedisValue> aquire, TimeSpan expiry)
        {
            if (database == null)
            {
                throw new ArgumentNullException(nameof(database));
            }

            var value = database.StringGet(key);
            if (value != RedisValue.Null)
            {
                return value;
            }

            if (aquire == null)
            {
                throw new ArgumentNullException(nameof(aquire));
            }

            if (expiry < TimeSpan.FromSeconds(1))
            {
                throw new ArgumentException(nameof(expiry));
            }

            value = aquire(key);

            database.StringSet(key, value, expiry);

            return value;
        }
    }
}
