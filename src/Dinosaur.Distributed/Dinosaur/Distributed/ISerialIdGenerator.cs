using System;
using System.Threading.Tasks;

namespace Dinosaur.Distributed
{
    /// <summary>
    /// 流水Id生成器
    /// </summary>
    public interface ISerialIdGenerator
    {
        /// <summary>
        /// 回拨
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">回拨的值</param>
        /// <param name="when">回拨条件</param>
        /// <param name="expiry">有效期</param>
        /// <returns>回拨是否成功</returns>
        bool Dialback(string key, long value, ConditionWhen when, TimeSpan? expiry = null);

        /// <summary>
        /// 回拨
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">回拨的值</param>
        /// <param name="when">回拨条件</param>
        /// <param name="expiry">有效期</param>
        /// <returns>回拨是否成功</returns>
        Task<bool> DialbackAsync(string key, long value, ConditionWhen when, TimeSpan? expiry = null);

        /// <summary>
        /// 增长1并返回增长后的值
        /// </summary>
        /// <param name="key">键</param>
        /// <returns>增长后的流水Id</returns>
        long Increment(string key);

        /// <summary>
        /// 增长1并返回增长后的值
        /// </summary>
        /// <param name="key">键</param>
        /// <returns>增长后的流水Id</returns>
        Task<long> IncrementAsync(string key);
    }
}
