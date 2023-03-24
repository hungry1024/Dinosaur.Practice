using Dinosaur.SqlServerToMySql.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using static Dapper.SqlMapper;

namespace Dinosaur.SqlServerToMySql
{
    public class MySqlStructure
    {
        private static readonly object _lockobj = new object();

        private readonly int CharsetMaxLen;

        private readonly IEnumerable<TableInfo> tables;

        private string[] createdTables;

        private readonly ITransferDataSource _source;
        private readonly ILogger<MySqlStructure> _logger;

        public MySqlStructure(ITransferDataSource dataSource, ILogger<MySqlStructure> logger)
        {
            _source = dataSource;
            _logger = logger;

            CharsetMaxLen = GetCharsetMaxLen();

            string[] exceptionTables = Settings.Configuration.GetSection("MySql:ExceptionTables").Get<string[]>() ?? Array.Empty<string>();

            createdTables = GetCreated();

            _logger.LogInformation("获取源数据库表架构 ...");

            tables = _source.GetTables()
               .Where(t => !exceptionTables.Contains(t.Name, StringComparer.OrdinalIgnoreCase))
               .ToList();

            foreach (var table in tables)
            {
                foreach (var column in table.Columns)
                {
                    ColumnTypeConvert(column);
                }

                HandleRowSizeTooLarge(table);
            }

        }

        public void Create(bool removeLog = false)
        {
            if (removeLog && File.Exists("created.txt"))
            {
                File.Delete("created.txt");
                createdTables = new string[0];
            }

            foreach (var table in tables.OrderBy(t => t.Name))
            {
                if (Exceptive.Tables.Contains(table.Name, StringComparer.OrdinalIgnoreCase))
                    continue;

                if (!CreateTable(table))
                {
                    break;
                }
            }

            if (!string.IsNullOrEmpty(Exceptive.ExTables))
            {
                ExecuteSql(Exceptive.ExTables);
                var sb = new StringBuilder();
                foreach (var tb in Exceptive.Tables)
                {
                    sb.AppendLine(tb);
                }
                LogCreated(sb.ToString());
            }
        }

        public void CreateTrigger4FuncDdefault()
        {
            foreach (var table in tables)
            {
                if (Exceptive.Tables.Contains(table.Name, StringComparer.OrdinalIgnoreCase))
                    continue;

                string triggerName = GenerateTriggerName(table.Name);
                if (ExistsTrigger(triggerName))
                {
                    continue;
                }

                var sql = new StringBuilder();
                sql.AppendLine($@"CREATE TRIGGER `{triggerName}` BEFORE INSERT ON `{table.Name}`
FOR EACH ROW 
BEGIN");
                bool flag = false;
                foreach (var col in table.Columns)
                {
                    switch (col.DefaultValue)
                    {
                        case "(newid())":
                        case "('(newid())')":
                            flag = true;
                            sql.AppendLine($@"
  IF (new.`{col.Name}` IS NULL) THEN
    SET new.`{col.Name}`=UUID();
  END IF;");
                            break;

                        case "(getdate())":
                            if (col.Type == "date")
                            {
                                col.Type = "datetime";
                                col.Type2 = "datetime";
                            }

                            if (col.Type != "datetime")
                            {
                                flag = true;
                                sql.AppendLine($@"
  IF (new.`{col.Name}` IS NULL) THEN
    SET new.`{col.Name}`=DATE_FORMAT(now(),'%Y-%m-%d %H:%I:%S');
  END IF;");
                            }
                            break;

                        default:
                            break;
                    }
                }

                if (!flag) continue;

                sql.AppendLine("END");
                if (ExecuteSql(sql.ToString()))
                {
                    _logger.LogInformation($"已为表{table.Name}创建触发器{triggerName}");
                }
                else
                {
                    _logger.LogError($"为表{table.Name}创建触发器{triggerName}失败\r\n{sql.ToString()}");
                }
            }

            if (!string.IsNullOrEmpty(Exceptive.ExConstraints))
            {
                ExecuteSql(Exceptive.ExConstraints);
            }
        }

        public void SetEngine(MySqlEngine engine)
        {
            string[] exceptions = { "pb_config", "Sky_Config" };
            foreach (var tbn in tables.Where(t => !exceptions.Contains(t.Name, StringComparer.OrdinalIgnoreCase)).Select(t => t.Name))
            {
                _logger.LogInformation($"正在设置表`{tbn}`的引擎为{engine}");
                if (!string.Equals(GetTableEngine(tbn), engine.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    string sql = $"ALTER TABLE `{tbn}` ENGINE={engine};";
                    ExecuteSql(sql, 7200);
                }
            }
        }

        public void Transfer(bool removeLog = false)
        {
            _logger.LogInformation("开始传输数据 ...");

            string[] transfered;
            if (removeLog && File.Exists("transfered.txt"))
            {
                File.Delete("transfered.txt");
                transfered = Array.Empty<string>();
            }
            else
            {
                transfered = GetTransfered();
            }

            while (tables.Any(t => !transfered.Contains(t.Name)))
            {
                try
                {
                    Parallel.ForEach(tables.Where(t => !transfered.Contains(t.Name)), (table, loopState) =>
                    {
                        Transfer(table);
                    });
                }
                catch (AggregateException ae)
                {
                    foreach (var ex in ae.Flatten().InnerExceptions)
                    {
                        _logger.LogError(ex, $"{ex.Message}，即将重新尝试传输数据");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"{ex.Message}，即将重新尝试传输数据");
                }
                finally
                {
                    _logger.LogWarning($"先歇一下");
                    Thread.Sleep(3000);
                    transfered = GetTransfered();
                }
            }

            _logger.LogInformation("传输数据已完成\n");
        }

        public void Verify()
        {
            _logger.LogInformation("开始验证传输，验证数据行数是否相等 ...");
            Parallel.ForEach(tables, (table, pls) =>
            {
                var total = _source.GetRowCount(table.Name);
                var total2 = GetRowCount(table.Name);
                if (total == total2)
                {
                    //_logger.LogInformation("{0} 传输完成，验证通过", table.Name);
                }
                else
                {
                    _logger.LogWarning($"{table.Name} 传输的数据量不相等，数据源：{total}，已传输：{total2}");
                    //ExecuteSql($"TRUNCATE TABLE `{table.Name}`");
                    //Transfer(table);
                }
            });
        }

        public void CreateIndex()
        {
            _logger.LogInformation("创建索引，大表创建索引会比较久，请耐心等待 ...");
            var sw = new System.Diagnostics.Stopwatch();

            var action1 = () =>
            {
                foreach (var table in tables.Where(t => !Exceptive.Tables.Contains(t.Name, StringComparer.OrdinalIgnoreCase)))
                {
                    if (table.Indices == null || !table.Indices.Any())
                        continue;

                    _logger.LogInformation($"正在创建 {table.Name} 的索引，{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                    sw.Restart();

                    var created = GetIndices(table.Name.ToLower());

                    var sql = new StringBuilder();
                    sql.Append("ALTER TABLE `");
                    sql.Append(table.Name.ToLower());
                    sql.AppendLine("`");

                    int i = 0;
                    foreach (var item in table.Indices.Where(k => !created.Contains(k.IndexName))
                        .GroupBy(ki => new { ki.IndexName, ki.IsUnique, ki.IsUniqueConstraint })
                        .Select(m => m.Key))
                    {
                        if (i > 0)
                        {
                            sql.AppendLine(",");
                        }
                        string indexType = item.IsUnique || item.IsUniqueConstraint ? "UNIQUE INDEX" : "INDEX";
                        var indexColumns = table.Indices.Where(c => c.IndexName == item.IndexName).OrderBy(c => c.KeyOrdinal).Select(c => $"`{c.ColumnName}`");
                        sql.Append($"  ADD {indexType} `{item.IndexName}`({string.Join(",", indexColumns)}) USING BTREE");

                        i++;
                    }

                    if (i == 0)
                    {
                        _logger.LogInformation($"已创建 {table.Name} 的索引");
                        continue;
                    }

                    sql.Append(";");

                    bool flag = ExecuteSql(sql.ToString(), 3600);
                    sw.Stop();

                    if (flag)
                    {
                        _logger.LogInformation($"已创建 {table.Name} 的索引，耗时：{sw.ElapsedMilliseconds}");
                    }
                    else
                    {
                        _logger.LogInformation($"创建 {table.Name} 的索引失败，耗时：{sw.ElapsedMilliseconds}");
                        break;
                    }
                }
            };

            var action2 = () =>
            {
                foreach (var idx in Exceptive.ExIndies)
                {
                    if (GetIndices(idx.Key).Any()) continue;

                    _logger.LogInformation($"正在创建表{idx.Key}的索引... ，{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                    ExecuteSql(idx.Value, 7200);
                    _logger.LogInformation($"已创建表{idx.Key}的索引...，{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                }
            };

            Parallel.Invoke(action1, action2);

            _logger.LogInformation("创建索引全部已完成\n");
        }

        public void CreateForeign()
        {
            _logger.LogInformation("创建外键约束 ...");
            var sw = new System.Diagnostics.Stopwatch();

            foreach (var table in tables)
            {
                if (Exceptive.Tables.Contains(table.Name, StringComparer.OrdinalIgnoreCase))
                    continue;

                if (table.Foreigns == null || !table.Foreigns.Any())
                    continue;

                _logger.LogInformation($"正在创建 {table.Name} 的外键约束 ...");
                sw.Restart();

                var created = GetConstraints(table.Name);

                var sql = new StringBuilder();
                sql.Append("ALTER TABLE `");
                sql.Append(table.Name);
                sql.AppendLine("`");

                int i = 0;
                foreach (var item in table.Foreigns.Where(f => !created.Contains(f.ForeignName))
                    .GroupBy(fi => new { fi.ForeignName, fi.KeyTable, fi.ForeignTable })
                    .Select(m => m.Key))
                {
                    if (i > 0)
                    {
                        sql.AppendLine(",");
                    }
                    var foreign = table.Foreigns.Where(c => c.ForeignName == item.ForeignName)
                        .Select(c => new { kc = $"`{c.KeyColumn}`", fc = $"`{c.ForeignKey}`" });
                    sql.Append($" ADD CONSTRAINT `{item.ForeignName}` FOREIGN KEY({string.Join(",", foreign.Select(o => o.kc))}) REFERENCES `{item.ForeignTable}`({string.Join(",", foreign.Select(o => o.fc))})");

                    i++;
                }

                if (i == 0)
                {
                    _logger.LogInformation($"已创建 {table.Name} 的外键约束");
                    continue;
                }

                sql.Append(";");

                bool flag = ExecuteSql(sql.ToString(), 1200);
                sw.Stop();

                if (flag)
                {
                    _logger.LogInformation($"已创建 {table.Name} 的外键约束，耗时：{sw.ElapsedMilliseconds}");
                }
                else
                {
                    _logger.LogInformation($"创建 {table.Name} 的外键约束失败");
                    break;
                }
            }

            _logger.LogInformation("创建外键约束全部已完成\n");
        }

        private bool CreateTable(TableInfo table)
        {
            if (createdTables.Contains(table.Name))
                return true;

            _logger.LogInformation($"正在创建表 {table.Name}");

            var sql = new StringBuilder();
            //sql.AppendLine("SET NAMES utf8mb4;");
            sql.AppendLine("SET FOREIGN_KEY_CHECKS = 0;");
            sql.AppendLine($"DROP TABLE IF EXISTS `{table.Name}`;");
            sql.AppendLine($"CREATE TABLE `{table.Name}` (");

            int columnsCount = table.Columns.Count();
            int i = 0;
            foreach (var col in table.Columns)
            {
                string nullable = col.IsNullable ? "NULL" : "NOT NULL";
                string defaultValueExpression = GetDefaultExpression(col);
                string identityExpression = col.IsIdentity ? " AUTO_INCREMENT" : string.Empty;
                string commentExpression = GetCommentExpression(col);
                sql.Append($"  `{col.Name}` {col.Type2} {nullable}{identityExpression}{defaultValueExpression}{commentExpression}");
                if (i++ < columnsCount - 1)
                {
                    sql.AppendLine(",");
                }
            }

            if (table.Columns.Any(c => c.IsIdentity))
            {
                sql.AppendLine(",");
                sql.Append($"  PRIMARY KEY (`{table.Columns.First(c => c.IsIdentity).Name}`) USING BTREE");
                if (table.Columns.Any(c => c.IsKey && !c.IsIdentity))
                {
                    sql.AppendLine(",");
                    sql.Append($"  UNIQUE INDEX `uix_{string.Join("_", table.Columns.Where(c => c.IsKey).Select(c => c.Name))}`({string.Join(",", table.Columns.Where(c => c.IsKey).Select(c => $"`{c.Name}`"))}) USING BTREE");
                }
            }
            else if (table.Columns.Any(c => c.IsKey))
            {
                sql.AppendLine(",");
                sql.Append($"  PRIMARY KEY ({string.Join(",", table.Columns.Where(c => c.IsKey).Select(c => $"`{c.Name}`"))}) USING BTREE");
            }

            sql.AppendLine();
            sql.Append($") ENGINE = InnoDB ");
            if (!string.IsNullOrEmpty(table.Description))
            {
                sql.Append($"COMMENT = '{table.Description}' ");
            }

            if (table.RowLength > 8126)
            {
                sql.AppendLine("ROW_FORMAT = COMPRESSED KEY_BLOCK_SIZE=8;");
            }
            else
            {
                sql.AppendLine("ROW_FORMAT = DYNAMIC;");
            }
            sql.AppendLine();
            sql.AppendLine("SET FOREIGN_KEY_CHECKS = 1;");

            if (ExecuteSql(sql.ToString()))
            {
                _logger.LogInformation($"创建表 {table.Name} 成功");
                LogCreated(table.Name);
                return true;
            }
            else
            {
                _logger.LogInformation($"创建表 {table.Name} 失败");
                return false;
            }
        }

        private void Transfer(TableInfo table)
        {
            int batch = 20000;
            var sw = new System.Diagnostics.Stopwatch();
            var total = _source.GetRowCount(table.Name);
            var total2 = GetRowCount(table.Name);

            if (total > total2)
            {
                if (table.Columns.Any(c => c.IsKey || c.IsIdentity))
                {
                    object? lastKey = null;
                    string keyName = string.Empty;

                    if (table.Columns.Any(c => c.IsIdentity))
                    {
                        keyName = table.Columns.First(c => c.IsIdentity).Name;

                        lastKey = GetLastKey(table, keyName);
                    }
                    else if (table.Columns.Count(c => c.IsKey) == 1)
                    {
                        var col = table.Columns.First(c => c.IsKey);
                        keyName = col.Name;

                        if (col.Type2 == "char(36)")
                        {
                            lastKey = _source.GetLastKey4Char36(table, keyName, total2);
                        }
                        else
                        {
                            lastKey = GetLastKey(table, keyName);
                        }
                    }

                    int start = total2 / batch;

                    for (int i = start; i < total / batch + 1; i++)
                    {
                        sw.Restart();
                        var dt = lastKey == null ? _source.GetData(table, i * batch, batch) : _source.GetData(table, lastKey, batch);
                        sw.Stop();
                        _logger.LogInformation($"{table.Name}  {i}，获取数据用时: {sw.ElapsedMilliseconds}");

                        if (dt.Rows.Count > 0)
                        {
                            if (keyName.Length > 0)
                            {
                                lastKey = dt.Rows[dt.Rows.Count - 1][keyName];
                            }

                            sw.Restart();
                            var result = Import(dt, table);
                            sw.Stop();
                            _logger.LogInformation($"{table.Name} {i} 成功插入{result.RowsInserted}数据，用时: {sw.ElapsedMilliseconds}，Warning: {result.Warnings.Count}");
                        }
                    }
                }
                else
                {
                    sw.Restart();
                    var dt = _source.GetData(table, 0, batch);
                    sw.Stop();
                    _logger.LogInformation($"{table.Name}，获取数据用时 {sw.ElapsedMilliseconds} 毫秒");
                    if (dt.Rows.Count > 0)
                    {
                        sw.Restart();
                        var result = Import(dt, table);
                        sw.Stop();
                        if (result.Warnings.Count > 0)
                        {
                            foreach (var err in result.Warnings)
                            {
                                _logger.LogError($"Code={err.ErrorCode}, Level={err.Level}, Message={err.Message}");
                            }

                            throw new Exception($"{table.Name} 成功插入{result.RowsInserted}数据，用时: {sw.ElapsedMilliseconds}，Warning: {result.Warnings.Count}");
                        }

                        _logger.LogInformation($"{table.Name} 成功插入{result.RowsInserted}数据，用时: {sw.ElapsedMilliseconds}，Warning: {result.Warnings.Count}");
                    }
                }
            }
            else
            {
                _logger.LogInformation($"{table.Name}已传输，跳过");
                LogTransfered(table.Name);
            }
        }

        private void LogCreated(string tableName)
        {
            lock (_lockobj)
            {
                File.AppendAllLines("created.txt", new string[] { tableName });
            }
        }

        private string[] GetCreated()
        {
            if (!File.Exists("created.txt"))
            {
                return new string[0];
            }

            return File.ReadAllLines("created.txt");
        }

        private void LogTransfered(string tableName)
        {
            lock (_lockobj)
            {
                File.AppendAllLines("transfered.txt", new string[] { tableName });
            }
        }

        private string[] GetTransfered()
        {
            if (!File.Exists("transfered.txt"))
            {
                return new string[0];
            }

            return File.ReadAllLines("transfered.txt");
        }

        private MySqlBulkCopyResult Import(DataTable dt, TableInfo table)
        {
            using var conn = new MySqlConnection(Settings.MySqlConnectionString);
            conn.Open();
            var bulkCopy = new MySqlBulkCopy(conn)
            {
                DestinationTableName = table.Name
            };

            return bulkCopy.WriteToServer(dt);
        }

        private void ColumnTypeConvert(Column column)
        {
            column.Name = column.Name.Trim();

            switch (column.Type)
            {
                case "bigint":
                    column.Size = 8;
                    break;
                case "binary":
                    column.Size = 0;
                    break;
                case "bit":
                    column.Size = 1;
                    if (column.IsNullable) column.Size++;
                    column.Type = "tinyint";
                    column.Type2 = "tinyint(1)";
                    break;
                case "nchar":
                case "char":
                    if (column.Length > 255)
                    {
                        column.Type = "varchar";
                        column.Size = column.Length * CharsetMaxLen + 2;
                        column.Type2 = $"varchar({column.Length})";
                        if (column.IsNullable) column.Size++;
                    }
                    else
                    {
                        column.Type = "char";
                        column.Type2 = $"char({column.Length})";
                        column.Size = column.Length * CharsetMaxLen;
                        if (column.IsNullable) column.Size++;
                    }
                    break;
                case "date":
                    column.Size = 3;
                    if (column.IsNullable) column.Size++;
                    break;
                case "datetime":
                    column.Size = 8;
                    if (column.IsNullable) column.Size++;
                    break;
                case "decimal":
                    column.Size = column.Length >= column.Scale ? column.Length + 2 : column.Scale + 2;
                    if (column.IsNullable) column.Size++;
                    break;
                case "float":
                    column.Size = 4;
                    column.Type = "float";
                    if (column.IsNullable) column.Size++;
                    break;
                case "double":
                    column.Size = 8;
                    column.Type = "double";
                    if (column.IsNullable) column.Size++;
                    break;
                case "longblob":
                case "image":
                    column.Size = 0;
                    column.Length = 0;
                    column.Type = "longblob";
                    column.Type2 = "longblob";
                    break;
                case "int":
                    column.Size = 4;
                    column.Type = "int";
                    if (column.IsNullable) column.Size++;
                    break;
                case "money":
                    column.Size = 20;
                    column.Type = "decimal";
                    column.Length = 18;
                    column.Scale = 4;
                    column.Type2 = "decimal(18,4)";
                    if (column.IsNullable) column.Size++;
                    break;
                case "text":
                case "ntext":
                    column.Size = 0;
                    column.Length = 0;
                    column.Type = "text";
                    column.Type2 = "text";
                    break;
                case "longtext":
                    column.Size = 0;
                    column.Length = 0;
                    column.Type = "longtext";
                    break;
                case "numeric":
                    column.Size = column.Length >= column.Scale ? column.Length + 2 : column.Scale + 2;
                    column.Type = "decimal";
                    column.Type2 = $"decimal({column.Length},{column.Scale})";
                    if (column.IsNullable) column.Size++;
                    break;
                case "varchar":
                case "nvarchar":
                    if (column.Length == -1)
                    {
                        column.Size = 0;
                        column.Type = "text";
                        column.Type2 = "text";
                    }
                    else
                    {
                        column.Size = column.Length * CharsetMaxLen + 2;
                        column.Type = "varchar";
                        column.Type2 = $"varchar({column.Length})";
                        if (column.IsNullable) column.Size++;
                    }
                    break;
                case "smallint":
                    column.Size = 2;
                    if (column.IsNullable) column.Size++;
                    break;
                case "timestamp":
                    column.Size = 8;
                    column.Type = "binary";
                    column.Type2 = "binary(8)";
                    if (column.IsNullable) column.Size++;
                    break;
                case "tinyint":
                    column.Size = 1;
                    if (column.IsNullable) column.Size++;
                    break;
                case "uniqueidentifier":
                    column.Size = 36 * CharsetMaxLen;
                    column.Length = 36;
                    column.Type = "char";
                    column.Type2 = "char(36)";
                    if (column.IsNullable) column.Size++;
                    break;
                case "varbinary":
                    column.Size = 0;
                    column.Length = 0;
                    break;
                case "xml":
                    column.Size = 0;
                    column.Type = "text";
                    column.Type2 = "text";
                    column.Length = 0;
                    break;
                default:
                    throw new NotSupportedException($"不支持的数据类型：{column.Entity}.{column.Name} {column.Type2}");
            }
        }

        private void HandleRowSizeTooLarge(TableInfo table, int recursion = 0)
        {
            if (table.RowSize < 65535)
                return;

            foreach (var col in table.Columns.Where(c => c.Type == "varchar" && c.Length >= 4000 - recursion * 100))
            {
                col.Size = 0;
                col.Type = "text";
                col.Type2 = "text";
            }

            HandleRowSizeTooLarge(table, ++recursion);
        }

        private bool ExecuteSql(string sql, int timeout = 30)
        {
            var conn = new MySqlConnection(Settings.MySqlConnectionString);
            try
            {
                conn.Execute(sql, param: null, transaction: null, commandTimeout: timeout);

                return true;
            }
            catch (MySqlException ex)
            {
                _logger.LogError($"执行sql: \r\n{sql}");
                throw;
            }
            finally
            {
                if (conn != null)
                {
                    conn.Dispose();
                }
            }
        }

        private object? GetLastKey(TableInfo table, string keyName)
        {
            var conn = new MySqlConnection(Settings.MySqlConnectionString);
            var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT MAX(`{keyName}`) FROM `{table.Name}`;";
            conn.Open();
            var obj = cmd.ExecuteScalar();
            if (obj == DBNull.Value) return null;

            return obj;
        }

        private string GetDefaultExpression(Column col)
        {
            if (col.DefaultValue == null || col.DefaultValue.Length < 1)
                return string.Empty;

            if (_source is MySqlDataSource)
            {
                switch (col.Type)
                {
                    case "char":
                    case "varchar":
                        return $" DEFAULT '{col.DefaultValue}'";

                    case "binary":
                        if (col.Length == 8)
                        {
                            return " DEFAULT '\0\0\0\0\0\0\0\0'";
                        }
                        break;

                    case "datetime":
                        return col.DefaultValue == "CURRENT_TIMESTAMP" ?
                            $" DEFAULT CURRENT_TIMESTAMP" :
                            $" DEFAULT '{col.DefaultValue}'";
                    default:
                        return $" DEFAULT {col.DefaultValue}";
                }

                return string.Empty;
            }

            string defaultValue;
            switch (col.DefaultValue)
            {
                case "(newid())":
                case "('(newid())')":
                    //defaultValue = "(UUID())"; mysql 8.0
                    defaultValue = string.Empty; // mysql 5.7
                    break;

                case "(getdate())":
                    if (col.Type == "date")
                    {
                        col.Type = "datetime";
                        col.Type2 = "datetime";
                    }

                    if (col.Type == "datetime")
                    {
                        defaultValue = " CURRENT_TIMESTAMP";
                    }
                    else
                    {
                        //defaultValue = "(CURRENT_TIMESTAMP())"; mysql 8.0
                        defaultValue = string.Empty; // mysql 5.7
                    }
                    break;

                default:
                    defaultValue = col.DefaultValue.Contains("N'") ? col.DefaultValue.Replace("N'", "'") : col.DefaultValue;

                    if (defaultValue.StartsWith("(") && defaultValue.EndsWith(")"))
                    {
                        defaultValue = defaultValue.TrimStart('(').TrimEnd(')');
                    }
                    if (defaultValue.StartsWith("(") && defaultValue.EndsWith(")"))
                    {
                        defaultValue = defaultValue.TrimStart('(').TrimEnd(')');
                    }
                    break;
            }

            return string.IsNullOrEmpty(defaultValue) ? string.Empty : $" DEFAULT {defaultValue}";
        }

        private string GetCommentExpression(Column col)
        {
            if (col.Description.Length == 0)
                return string.Empty;

            return $" COMMENT '{col.Description.Replace("'", "\\'")}'";
        }

        private int GetRowCount(string tableName)
        {
            using var conn = new MySqlConnection(Settings.MySqlConnectionString);
            return conn.ExecuteScalar<int>($"SELECT COUNT(*) FROM `{tableName}`", param: null, transaction: null, commandTimeout: 600);
        }

        private string[] GetConstraints(string tableName)
        {
            using var conn = new MySqlConnection(Settings.MySqlConnectionString);

            string sql = $"SELECT CONSTRAINT_NAME from INFORMATION_SCHEMA.KEY_COLUMN_USAGE where TABLE_NAME = @TableName AND TABLE_SCHEMA='{conn.Database}'";

            var result = conn.Query<string>(sql, new { TableName = tableName });
            return result == null ? Array.Empty<string>() : result.Where(n => !"PRIMARY".Equals(n, StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        private string[] GetIndices(string tableName)
        {
            using var conn = new MySqlConnection(Settings.MySqlConnectionString);
            string sql = $"SELECT INDEX_NAME FROM information_schema.statistics WHERE TABLE_NAME = @TableName AND TABLE_SCHEMA='{conn.Database}'";
            var result = conn.Query<string>(sql, new { TableName = tableName });
            return result == null ? Array.Empty<string>() : result.Where(n => !"PRIMARY".Equals(n, StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        private int GetCharsetMaxLen()
        {
            string setName = GetDefaultCharacterSetName();

            switch (setName)
            {
                case "gbk":
                case "gb2312":
                    return 2;
                case "utf8":
                    return 3;
                case "utf16":
                case "utf16le":
                case "utf32":
                case "utf8mb4":
                    return 4;
                default:
                    throw new NotSupportedException($"不支持的默认字符集: {setName}");
            }
        }

        private string GetDefaultCharacterSetName()
        {
            using (var conn = new MySqlConnection(Settings.MySqlConnectionString))
            {
                string setName = conn.ExecuteScalar<string>($"select DEFAULT_CHARACTER_SET_NAME from information_schema.SCHEMATA where SCHEMA_NAME='{conn.Database}' limit 1");

                if (string.IsNullOrEmpty(setName))
                {
                    throw new NotSupportedException("无法获取默认字符集");
                }

                return setName;
            }
        }

        private bool ExistsTrigger(string triggerName)
        {
            using (var conn = new MySqlConnection(Settings.MySqlConnectionString))
            {
                string existsSql = $@"SELECT COUNT(*) FROM information_schema.`TRIGGERS` 
WHERE TRIGGER_SCHEMA='{conn.Database}' AND TRIGGER_NAME='{triggerName}'";

                return conn.ExecuteScalar<int>(existsSql) > 0;
            }
        }

        private string GenerateTriggerName(string table, string type = "inserting")
        {
            bool needSpecialHandle = false;
            foreach (char ch in table)
            {
                if (!char.IsAscii(ch))
                {
                    needSpecialHandle = true;
                    break;
                }
            }

            if (!needSpecialHandle)
            {
                return $"{table}_{type}";
            }

            using var md5 = MD5.Create();
            byte[] s = md5.ComputeHash(Encoding.UTF8.GetBytes(table));
            var sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                int rem = (s[i] % 26) + 97;
                sb.Append((char)rem);
            }

            return $"{sb}_{type}";
        }

        private string GetTableEngine(string tableName)
        {
            string sql = $"select `engine` from information_schema.tables where table_name='{tableName}'";
            using var conn = new MySqlConnection(Settings.MySqlConnectionString);
            return conn.ExecuteScalar<string>(sql) ?? string.Empty;
        }

    }
}
