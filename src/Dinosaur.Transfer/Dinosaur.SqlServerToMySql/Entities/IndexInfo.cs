namespace Dinosaur.SqlServerToMySql.Entities
{
    public class IndexInfo
    {
        public string TableName { get; set; }

        public string IndexName { get; set; }

        public string ColumnName { get; set; }

        public int IndexType { get; set; }

        public string IndexTypeDesc { get; set; }

        public bool IsUnique { get; set; }

        public bool IsPrimaryKey { get; set; }

        public bool IsUniqueConstraint { get; set; }

        public bool IsDescendingKey { get; set; }

        public int KeyOrdinal { get; set; }

        public bool IsIncludedColumn { get; set; }
    }
}
