using Dapper;
using Dinosaur.SqlServerToMySql.Entities;
using MySqlConnector;
using Newtonsoft.Json;
using System.Data;
using System.Text;

namespace Dinosaur.SqlServerToMySql
{
    public class MySqlDataSource : ITransferDataSource
    {
        private readonly string _connectionString;

        public MySqlDataSource(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IEnumerable<Column> GetColumns()
        {
            using var conn = new MySqlConnection(_connectionString);
            string sql = $@"SELECT a.TABLE_NAME AS `Entity`
	,a.COLUMN_NAME AS `Name`
	,a.ORDINAL_POSITION AS `ColOrder`
	,a.DATA_TYPE AS `Type`
	,IFNULL(a.NUMERIC_PRECISION,a.CHARACTER_MAXIMUM_LENGTH) AS `Length`
	,a.NUMERIC_SCALE AS `Scale`
	,CASE WHEN a.IS_NULLABLE='YES' THEN 1 ELSE 0 END AS `IsNullable`
	,CASE WHEN a.EXTRA='auto_increment' THEN 1 ELSE 0 END AS `IsIdentity`
	,a.COLUMN_COMMENT AS `Description`
	,a.COLUMN_DEFAULT AS `DefaultValue`
	,CASE a.COLUMN_KEY WHEN 'PRI' THEN 1 ELSE 0 END AS `IsKey`
	,a.COLUMN_TYPE AS `Type2`
FROM information_schema.COLUMNS AS a
WHERE a.TABLE_SCHEMA = '{conn.Database}'
ORDER BY `Entity`,`ColOrder`";

            return conn.Query<Column>(sql, param: null, transaction: null, buffered: true, commandTimeout: 120);
        }

        public DataTable GetData(TableInfo table)
        {
            using var conn = new MySqlConnection(_connectionString);
            var sql = new StringBuilder("SELECT * FROM `");
            sql.Append(table.Name);
            sql.Append("`");
            using var adapt = new MySqlDataAdapter(sql.ToString(), conn);
            var dt = new DataTable();
            adapt.Fill(dt);
            return dt;
        }

        public DataTable GetData(TableInfo table, int offset, int rows)
        {
            using var conn = new MySqlConnection(_connectionString);
            var sql = new StringBuilder("SELECT * FROM `");
            sql.Append(table.Name);
            sql.Append("`");

            bool hasSort = false;
            if (table.Columns.Any(c => c.IsIdentity))
            {
                sql.Append(" ORDER BY `");
                sql.Append(table.Columns.First(c => c.IsIdentity).Name);
                sql.Append("` ASC");
                hasSort = true;
            }
            else if (table.Columns.Any(c => c.IsKey))
            {
                sql.Append(" ORDER BY ");
                sql.Append(string.Join(",", table.Columns.Where(c => c.IsKey).Select(c => $"`{c.Name}` ASC")));
                hasSort = true;
            }

            if (hasSort)
            {
                sql.Append(" LIMIT ");
                sql.Append(offset);
                sql.Append(",");
                sql.Append(rows);
            }

            var cmd = new MySqlCommand(sql.ToString(), conn)
            {
                CommandTimeout = 600
            };
            using var adapt = new MySqlDataAdapter(cmd);
            var dt = new DataTable();
            adapt.Fill(dt);

            return dt;
        }

        public DataTable GetData(TableInfo table, object baseKey, int rows)
        {
            string keyName = table.Columns.First(c => c.IsKey || c.IsIdentity).Name;

            using var conn = new MySqlConnection(_connectionString);
            var sql = new StringBuilder();
            sql.Append("SELECT * FROM `");
            sql.Append(table.Name);
            sql.Append("` ");
            sql.Append("WHERE `");
            sql.Append(keyName);
            sql.Append("` > @key ORDER BY ");
            sql.Append(keyName);
            sql.Append(" LIMIT ");
            sql.Append(rows);

            var cmd = conn.CreateCommand();
            cmd.CommandText = sql.ToString();
            cmd.Parameters.Add(new MySqlParameter("@key", baseKey));
            cmd.CommandTimeout = 600;

            using var adapt = new MySqlDataAdapter(cmd);
            var dt = new DataTable();
            adapt.Fill(dt);

            return dt;
        }

        public IEnumerable<Foreign> GetForeigns()
        {
            using var conn = new MySqlConnection(_connectionString);
            string sql = $@"SELECT CONSTRAINT_NAME AS `ForeignName`
  ,TABLE_NAME AS `KeyTable`
	,COLUMN_NAME AS `KeyColumn`
	,REFERENCED_TABLE_NAME AS `ForeignTable`
	,REFERENCED_COLUMN_NAME AS `ForeignKey`
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE 
WHERE TABLE_SCHEMA = '{conn.Database}'
  AND CONSTRAINT_NAME <> 'PRIMARY'
  AND REFERENCED_TABLE_NAME IS NOT NULL";

            return conn.Query<Foreign>(sql, param: null, transaction: null, buffered: true, commandTimeout: 120);
        }

        public IEnumerable<IndexInfo> GetIndices()
        {
            using var conn = new MySqlConnection(_connectionString);
            string sql = $@"SELECT TABLE_NAME AS TableName
  ,INDEX_NAME AS IndexName
	,COLUMN_NAME AS ColumnName
	,CASE INDEX_NAME WHEN 'PRIMARY' THEN 1 ELSE 2 END AS IndexType
	,CASE NON_UNIQUE WHEN 0 THEN 1 ELSE 0 END AS IsUnique
	,CASE INDEX_NAME WHEN 'PRIMARY' THEN 1 ELSE 0 END AS IsPrimaryKey
	,CASE NON_UNIQUE WHEN 0 THEN 1 ELSE 0 END AS IsUniqueConstraint
	,SEQ_IN_INDEX AS KeyOrdinal
FROM information_schema.statistics
WHERE TABLE_SCHEMA = '{conn.Database}'";

            return conn.Query<IndexInfo>(sql, param: null, transaction: null, buffered: true, commandTimeout: 120);
        }

        public object? GetLastKey4Char36(TableInfo table, string keyName, int total)
        {
            throw new NotImplementedException();
        }

        public long GetRowCount(string tableName)
        {
            using var conn = new MySqlConnection(_connectionString);
            var cmd = conn.CreateCommand();
            cmd.CommandTimeout = 300;
            cmd.CommandText = $"SELECT COUNT(*) FROM `{tableName}`";
            conn.Open();

            object? obj = cmd.ExecuteScalar();

            if (obj == null || Equals(DBNull.Value, obj))
            {
                return 0;
            }

            return (long)obj;
        }

        public IEnumerable<TableDescription> GetTableDescriptions()
        {
            using var conn = new MySqlConnection(_connectionString);
            string sql = $@"SELECT TABLE_NAME AS TableName
  ,IFNULL(TABLE_COMMENT,'') AS Description
FROM information_schema.tables
WHERE TABLE_SCHEMA = '{conn.Database}'
  AND TABLE_TYPE = 'BASE TABLE'";

            return conn.Query<TableDescription>(sql, param: null, transaction: null, buffered: true, commandTimeout: 120);
        }

        public IEnumerable<TableInfo> GetTables()
        {
            using var conn = new MySqlConnection(_connectionString);
            string cacheFile = $"{conn.Database}-tables.json";
            if (File.Exists(cacheFile))
            {
                var json = File.ReadAllText(cacheFile);
                var data = JsonConvert.DeserializeObject<IEnumerable<TableInfo>>(json);
                if (data != null && data.Any())
                {
                    return data;
                }
            }

            var tables2 = Build().OrderBy(t => t.Name);

            var json2 = JsonConvert.SerializeObject(tables2);
            File.WriteAllText(cacheFile, json2, Encoding.UTF8);

            return tables2;
        }

        private IEnumerable<TableInfo> Build()
        {
            var tableDescriptions = GetTableDescriptions();
            var allColumns = GetColumns();
            var allForeigns = GetForeigns();
            var allIndices = GetIndices();

            foreach (var td in tableDescriptions)
            {
                yield return new TableInfo
                {
                    Name = td.TableName,
                    Description = td.Description,
                    Columns = allColumns.Where(c => c.Entity == td.TableName).OrderBy(c => c.ColOrder).ToList(),
                    Foreigns = allForeigns.Where(c => c.KeyTable == td.TableName).ToList(),
                    Indices = allIndices.Where(c => c.TableName == td.TableName && !c.IsPrimaryKey && !c.IsIncludedColumn).ToList()
                };
            }
        }

    }
}
