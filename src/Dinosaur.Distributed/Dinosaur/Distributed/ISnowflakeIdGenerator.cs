using System;

namespace Dinosaur.Distributed
{
    /// <summary>
    /// 雪花Id生成器
    /// </summary>
    public interface ISnowflakeIdGenerator
    {
        /// <summary>
        /// 生成一个新的Id
        /// </summary>
        /// <returns>新的Id</returns>
        long NewId();

        /// <summary>
        /// 根据时间生成一个新的Id
        /// </summary>
        /// <param name="time">时间</param>
        /// <returns>新的Id</returns>
        long NewId(DateTime time);

        /// <summary>
        /// 得到仅有时间的Id，可用于范围查询
        /// </summary>
        /// <param name="time">时间</param>
        /// <returns>时间Id</returns>
        long GetIdOnlyTime(DateTime time);

        /// <summary>
        /// 解析
        /// </summary>
        /// <param name="id">Id</param>
        /// <returns>来源：时间、节点、顺序值</returns>
        (DateTime time, int nodeId, int sequence) Parse(long id);
    }
}
