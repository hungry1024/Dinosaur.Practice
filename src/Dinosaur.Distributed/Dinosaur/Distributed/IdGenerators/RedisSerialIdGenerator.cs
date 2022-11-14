using Dinosaur.Distributed.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Dinosaur.Distributed.IdGenerators
{
    internal class RedisSerialIdGenerator : ISerialIdGenerator
    {
        private static readonly string CacheNamespace = "dinosaur:serial:";

        public bool Dialback(string key, long value, ConditionWhen when, TimeSpan? expiry = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            switch (when)
            {
                case ConditionWhen.NotExists:
                    return RedisProxy.Database.StringSet(CacheNamespace + key, value, expiry ?? TimeSpan.FromDays(31), StackExchange.Redis.When.NotExists);
                case ConditionWhen.NotEqualToOldValue:
                    var oldValue = RedisProxy.Database.StringGet(CacheNamespace + key);
                    if(oldValue.HasValue && (long)oldValue == value)
                    {
                        return true;
                    }
                    break;
            }

            return RedisProxy.Database.StringSet(CacheNamespace + key, value, expiry ?? TimeSpan.FromDays(31));
        }

        public Task<bool> DialbackAsync(string key, long value, ConditionWhen when, TimeSpan? expiry = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            switch (when)
            {
                case ConditionWhen.NotExists:
                    return RedisProxy.Database.StringSetAsync(CacheNamespace + key, value, expiry ?? TimeSpan.FromDays(31), StackExchange.Redis.When.NotExists);
                case ConditionWhen.NotEqualToOldValue:
                    var oldValue = RedisProxy.Database.StringGet(CacheNamespace + key);
                    if (oldValue.HasValue && (long)oldValue == value)
                    {
                        return Task.FromResult(true);
                    }
                    break;
            }

            return RedisProxy.Database.StringSetAsync(CacheNamespace + key, value, expiry ?? TimeSpan.FromDays(31));
        }

        public long Increment(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            return RedisProxy.Database.StringIncrement(CacheNamespace + key);
        }

        public Task<long> IncrementAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            return RedisProxy.Database.StringIncrementAsync(CacheNamespace + key);
        }
    }
}
