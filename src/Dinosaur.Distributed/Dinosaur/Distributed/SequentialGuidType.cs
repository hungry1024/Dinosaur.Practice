namespace Dinosaur.Distributed
{
    /// <summary>
    /// 有序Guid排序类型
    /// </summary>
    public enum SequentialGuidType
    {
        /// <summary>
        /// 作为字符串排序，用于char(36)类型
        /// </summary>
        AsString,

        /// <summary>
        /// 末尾排序，适用于mssql的uniqueidentifier类型
        /// </summary>
        AtEnd
    }
}
