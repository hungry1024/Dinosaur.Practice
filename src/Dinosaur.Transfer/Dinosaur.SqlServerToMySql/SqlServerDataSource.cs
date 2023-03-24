using Dapper;
using Dinosaur.SqlServerToMySql.Entities;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using System.Text;

namespace Dinosaur.SqlServerToMySql
{
    public class SqlServerDataSource : ITransferDataSource
    {
        private readonly string _connectionString;

        public SqlServerDataSource(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IEnumerable<TableInfo> GetTables()
        {
            using var conn = new SqlConnection(_connectionString);
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

        public IEnumerable<Column> GetColumns()
        {
            string sql = @"select *
		,case [Type] when 'numeric' then 'numeric('+cast([Length] as varchar(10))+','+cast([Scale] as varchar(10))+')'
		             when 'decimal' then 'decimal('+cast([Length] as varchar(10))+','+cast([Scale] as varchar(10))+')'
					 when 'char' then 'char('+cast([Length] as varchar(10))+')'
					 when 'nchar' then 'nchar('+cast([Length] as varchar(10))+')'
					 when 'varchar' then 'varchar('+cast([Length] as varchar(10))+')'
					 when 'nvarchar' then 'nvarchar('+cast([Length] as varchar(10))+')'
					 when 'binary' then 'binary('+cast([Length] as varchar(10))+')'
		else [Type] end as Type2

from (
	SELECT 
		objs.[name] AS [Entity]
		,cols.[name] AS [Name]
        ,cols.colorder as [ColOrder]
		,st.[name] AS [Type]
		, COLUMNPROPERTY(cols.id,cols.name,'PRECISION') AS [Length]
		,ISNULL(COLUMNPROPERTY(cols.id,cols.name,'Scale'),0) AS [Scale]
		,cols.isnullable AS [IsNullable]
		,CASE WHEN COLUMNPROPERTY( cols.id,cols.name,'IsIdentity')=1 then cast(1 as bit) else cast(0 as bit) end as [IsIdentity]
		,ISNULL(pros.[value],'') AS [Description]
		,ISNULL(e.text,'') AS [DefaultValue]
		,CASE WHEN EXISTS(SELECT 1 FROM sysobjects where xtype='PK' and parent_obj=cols.id and name in (
						 SELECT name FROM sysindexes WHERE indid in( SELECT indid FROM sysindexkeys WHERE id = cols.id AND colid=cols.colid))) then CAST(1 as bit) else cast(0 as bit) end AS [IsKey]
	FROM sysobjects objs
		INNER JOIN syscolumns cols ON objs.id = cols.id
		INNER JOIN systypes tpes on cols.xusertype = tpes.xusertype AND tpes.[name] <> 'sysname'
        inner join master.dbo.systypes as st on tpes.xtype=st.xtype and st.[name] <> 'sysname'
		LEFT JOIN  syscomments e on  cols.cdefault=e.id
		LEFT JOIN sys.extended_properties pros ON pros.major_id = cols.id AND pros.minor_id = cols.colid
	WHERE objs.[type] = 'U' AND  objs.[name]<>'dtproperties'
) as t
ORDER BY t.entity";

            return Query<Column>(sql);
        }

        public IEnumerable<TableDescription> GetTableDescriptions()
        {
            string sql = @"select obj.[name] as TableName
,ISNULL(pro.value, '') as [Description]
from sysobjects as obj
left join sys.extended_properties pro on pro.major_id=obj.id and pro.minor_id=0
where obj.xtype='U'
order by TableName";

            return Query<TableDescription>(sql);
        }

        public IEnumerable<Foreign> GetForeigns()
        {
            string sql = @"SELECT OBJECT_NAME(con.constid) AS [ForeignName]
  ,OBJECT_NAME(sf.fkeyid) AS [KeyTable]
  ,fcol.[name] AS [KeyColumn]
  ,OBJECT_NAME(sf.rkeyid) AS [ForeignTable]
  ,rcol.[name] AS [ForeignKey]
FROM sysforeignkeys AS sf
INNER JOIN sysconstraints AS con ON sf.constid = con.constid
INNER JOIN sys.syscolumns AS fcol ON fcol.id = sf.fkeyid AND fcol.colid = sf.fkey
INNER JOIN sys.syscolumns AS rcol ON rcol.id = sf.rkeyid AND rcol.colid = sf.rkey";

            return Query<Foreign>(sql);
        }

        public IEnumerable<IndexInfo> GetIndices()
        {
            string sql = @"SELECT i.[object_id]
   ,i.index_id
   ,OBJECT_NAME(i.[object_id]) AS TableName
   ,i.[name] AS IndexName
   ,ocol.[name] AS ColumnName
   ,i.[type] AS IndexType
   ,i.[type_desc] AS IndexTypeDesc
   ,i.is_unique as IsUnique
   ,i.is_primary_key as IsPrimaryKey
   ,i.is_unique_constraint as IsUniqueConstraint
   ,i.fill_factor
   ,icol.key_ordinal AS KeyOrdinal
   ,icol.is_descending_key as IsDescendingKey
   ,icol.is_included_column as IsIncludedColumn
   FROM sys.indexes i ,
	   sys.index_columns icol ,
	   sys.columns ocol
  WHERE i.object_id = icol.object_id
		AND i.index_id = icol.index_id
		AND icol.object_id = ocol.object_id
		AND icol.column_id = ocol.column_id            
		AND EXISTS ( SELECT 1 FROM sys.objects o WHERE o.object_id = i.object_id AND o.type = 'U' )";

            return Query<IndexInfo>(sql);
        }

        public DataTable GetData(TableInfo table)
        {
            using var conn = new SqlConnection(_connectionString);
            var sql = new StringBuilder("SELECT * FROM [");
            sql.Append(table.Name);
            sql.Append("] WITH(NOLOCK)");
            using var adapt = new SqlDataAdapter(sql.ToString(), conn);
            var dt = new DataTable();
            adapt.Fill(dt);
            return dt;
        }

        //public DataTable GetData(TableInfo table, int offset, int rows)
        //{
        //	using var conn = new SqlConnection(_connectionString);
        //	var sql = new StringBuilder("SELECT * FROM [");
        //	sql.Append(table.Name);
        //	sql.Append("] WITH(NOLOCK)");

        //	bool hasSort = false;
        //	if (table.Columns.Any(c => c.IsIdentity))
        //	{
        //		sql.Append(" ORDER BY ");
        //		sql.Append(table.Columns.First(c => c.IsIdentity).Name);
        //		sql.Append(" ASC");
        //		hasSort = true;
        //	}
        //	else if (table.Columns.Any(c => c.IsKey))
        //	{
        //		sql.Append(" ORDER BY ");
        //		sql.Append(string.Join(",", table.Columns.Where(c => c.IsKey).Select(c => $"{c.Name} ASC")));
        //		hasSort = true;
        //	}

        //	if (hasSort)
        //	{
        //		sql.Append(" OFFSET ");
        //		sql.Append(offset);
        //		sql.Append(" ROWS FETCH NEXT ");
        //		sql.Append(rows);
        //		sql.Append(" ROWS ONLY");
        //	}

        //          var cmd = new SqlCommand(sql.ToString(), conn)
        //          {
        //              CommandTimeout = 600
        //          };
        //          using var adapt = new SqlDataAdapter(cmd);
        //	var dt = new DataTable();
        //	adapt.Fill(dt);

        //	return dt;
        //}

        public DataTable GetData(TableInfo table, int offset, int rows)
        {
            using var conn = new SqlConnection(Settings.SourceConnectionString);
            string sql;

            string orderby;

            if (table.Columns.Any(c => c.IsIdentity))
            {
                string keys = $"[{table.Columns.First(c => c.IsIdentity).Name}]";
                orderby = $" ORDER BY {keys} ASC";
            }
            else if (table.Columns.Any(c => c.IsKey))
            {
                orderby = $" ORDER BY {string.Join(",", table.Columns.Where(c => c.IsKey).Select(c => $"[{c.Name}] ASC"))}";
            }
            else
            {
                orderby = string.Empty;
            }

            if (orderby.Length > 0)
            {
                sql = $@"SELECT *
  FROM (SELECT *,ROW_NUMBER() OVER ({orderby}) AS __n FROM {table.Name}) AS T
 WHERE T.__n BETWEEN {offset + 1} AND {offset + rows}";
            }
            else
            {
                sql = $"SELECT * FROM [{table.Name}] WITH(NOLOCK)";
            }

            var cmd = new SqlCommand(sql, conn)
            {
                CommandTimeout = 600
            };
            using var adapt = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            adapt.Fill(dt);

            if (orderby.Length > 0)
            {
                dt.Columns.Remove("__n");
            }

            return dt;
        }

        public DataTable GetData(TableInfo table, object baseKey, int rows)
        {
            string keyName = table.Columns.First(c => c.IsKey || c.IsIdentity).Name;

            using var conn = new SqlConnection(_connectionString);
            var sql = new StringBuilder("SELECT TOP (");
            sql.Append(rows);
            sql.Append(") * FROM [");
            sql.Append(table.Name);
            sql.Append("] WITH(NOLOCK) ");
            sql.Append("WHERE [");
            sql.Append(keyName);
            sql.Append("] > @key ORDER BY ");
            sql.Append(keyName);

            var cmd = conn.CreateCommand();
            cmd.CommandText = sql.ToString();
            cmd.Parameters.Add(new SqlParameter("@key", baseKey));
            cmd.CommandTimeout = 600;

            using var adapt = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            adapt.Fill(dt);

            return dt;
        }

        public long GetRowCount(string tableName)
        {
            using var conn = new SqlConnection(_connectionString);
            var cmd = conn.CreateCommand();
            cmd.CommandTimeout = 300;
            cmd.CommandText = $"SELECT COUNT(*) FROM [{tableName}]";
            conn.Open();
            object obj = cmd.ExecuteScalar();

            return Convert.ToInt64(obj);
        }

        public object? GetLastKey4Char36(TableInfo table, string keyName, int total)
        {
            var conn = new SqlConnection(Settings.SourceConnectionString);
            var cmd = conn.CreateCommand();
            cmd.CommandText = $@"select [{keyName}] from (select [{keyName}],ROW_NUMBER() over(order by [{keyName}] asc) as RowIndex from [{table.Name}]
) as t
where RowIndex={total}";
            conn.Open();
            var obj = cmd.ExecuteScalar();
            if (obj == DBNull.Value) return null;

            return obj;
        }

        private IEnumerable<T> Query<T>(string sql)
        {
            using var conn = new SqlConnection(_connectionString);
            return conn.Query<T>(sql, param: null, transaction: null, buffered: true, commandTimeout: 120);
        }

    }
}
