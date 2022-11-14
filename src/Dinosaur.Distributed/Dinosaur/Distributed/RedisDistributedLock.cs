using Dinosaur.Distributed.Infrastructure;
using StackExchange.Redis;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dinosaur.Distributed
{
    /// <summary>
    /// 基于Redis的分布式锁
    /// </summary>
    public class RedisDistributedLock
    {
        private static readonly string KeyPrefix = "dinosaur:lock:";

        /// <summary>
        /// 临界区
        /// 独占超时为10秒、等待超时为30秒
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="action">临界区的执行方法</param>
        public static void Lock(string key, Action action)
        {
            Lock(key, action, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));
        }

        /// <summary>
        /// 临界区
        /// 等待超时为30秒
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="action">临界区的执行方法</param>
        /// <param name="lockTimeout">独占超时</param>
        public static void Lock(string key, Action action, TimeSpan lockTimeout)
        {
            Lock(key, action, lockTimeout, TimeSpan.FromSeconds(30));
        }

        /// <summary>
        /// 临界区
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="action">临界区的执行方法</param>
        /// <param name="lockTimeout">独占超时</param>
        /// <param name="getlockWaitTimeout">等待超时</param>
        /// <exception cref="ArgumentNullException">key为空抛出异常</exception>
        public static void Lock(string key, Action action, TimeSpan lockTimeout, TimeSpan getlockWaitTimeout)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            RedisValue value = Environment.MachineName;

            RetryUntilTrue(() => RedisProxy.Database.LockTake(KeyPrefix + key, value, lockTimeout), getlockWaitTimeout);
            try
            {
                action();
            }
            finally
            {
                RedisProxy.Database.LockRelease(KeyPrefix + key, value);
            }
        }

        /// <summary>
        /// 临界区
        /// 独占超时为10秒、等待超时为30秒
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="action">临界区的执行方法</param>
        public static Task LockAsync(string key, Func<Task> funcAsnc) 
            => LockAsync(key, funcAsnc, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));

        /// <summary>
        /// 临界区
        /// 等待超时为30秒
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="action">临界区的执行方法</param>
        /// <param name="lockTimeout">独占超时</param>
        public static Task LockAsync(string key, Func<Task> funcAsnc, TimeSpan lockTimeout) 
            => LockAsync(key, funcAsnc, lockTimeout, TimeSpan.FromSeconds(30));

        /// <summary>
        /// 临界区
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="action">临界区的执行方法</param>
        /// <param name="lockTimeout">独占超时</param>
        /// <param name="getlockWaitTimeout">等待超时</param>
        /// <exception cref="ArgumentNullException">key为空抛出异常</exception>
        public static async Task LockAsync(string key, Func<Task> funcAsnc, TimeSpan lockTimeout, TimeSpan getlockWaitTimeout)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            RedisValue value = Environment.MachineName;

            await RetryUntilTrueAsync(() => RedisProxy.Database.LockTakeAsync(KeyPrefix + key, value, lockTimeout), getlockWaitTimeout);
            try
            {
                await funcAsnc();
            }
            finally
            {
                await RedisProxy.Database.LockReleaseAsync(KeyPrefix + key, value);
            }
        }


        private static void RetryUntilTrue(Func<bool> action, TimeSpan timeOut)
        {
            int num = 0;
            DateTime entryTime = DateTime.Now;
            while (DateTime.Now - entryTime < timeOut)
            {
                num++;
                if (action())
                {
                    return;
                }

                SleepBackOffMultiplier(num);
            }

            throw new TimeoutException($"Exceeded timeout of {timeOut}");
        }

        private static async Task RetryUntilTrueAsync(Func<Task<bool>> action, TimeSpan timeOut)
        {
            int num = 0;
            DateTime entryTime = DateTime.Now;
            while (DateTime.Now - entryTime < timeOut)
            {
                num++;
                if (await action())
                {
                    return;
                }

                await SleepBackOffMultiplierAsync(num);
            }

            throw new TimeoutException($"Exceeded timeout of {timeOut}");
        }

        private static void SleepBackOffMultiplier(int n) => Thread.Sleep(GetDelayMilliseconds(n));

        private static Task SleepBackOffMultiplierAsync(int n) => Task.Delay(GetDelayMilliseconds(n));

        private static int GetDelayMilliseconds(int n)
        {
            var random = new Random(Guid.NewGuid().GetHashCode());

            return random.Next((int)Math.Pow(n, 2), (int)Math.Pow(n + 1, 2) + 1);
        }

    }
}
