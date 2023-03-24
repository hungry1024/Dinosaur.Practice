using System.Text.Json.Serialization;

namespace Dinosaur.SqlServerToMySql.Entities
{
    public class TableInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public IEnumerable<Column> Columns { get; set; }

        public IEnumerable<IndexInfo> Indices { get; set; }

        public IEnumerable<Foreign> Foreigns { get; set; }

        [JsonIgnore]
        public long RowSize => Columns.Sum(c => c.Size);

        [JsonIgnore]
        public long RowLength => Columns.Sum(c => c.Length);
    }
}
