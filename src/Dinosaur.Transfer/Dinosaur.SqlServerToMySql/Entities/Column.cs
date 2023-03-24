namespace Dinosaur.SqlServerToMySql.Entities
{
    public class Column
    {
        public string Entity { get; set; }

        public string Name { get; set; }

        public int ColOrder { get; set; }

        public string Type { get; set; }

        public long Length { get; set; }

        public int Scale { get; set; }

        public string Type2 { get; set; }

        public bool IsNullable { get; set; }

        public bool IsIdentity { get; set; }

        public string Description { get; set; }

        public string DefaultValue { get; set; }

        public bool IsKey { get; set; }

        public long Size { get; set; }
    }
}
