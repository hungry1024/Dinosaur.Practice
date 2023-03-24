using Dinosaur.SqlServerToMySql.Entities;
using System.Data;

namespace Dinosaur.SqlServerToMySql
{
    public interface ITransferDataSource
    {
        IEnumerable<TableInfo> GetTables();

        IEnumerable<Column> GetColumns();

        IEnumerable<TableDescription> GetTableDescriptions();

        IEnumerable<Foreign> GetForeigns();

        IEnumerable<IndexInfo> GetIndices();

        DataTable GetData(TableInfo table);

        DataTable GetData(TableInfo table, int offset, int rows);

        DataTable GetData(TableInfo table, object baseKey, int rows);

        object? GetLastKey4Char36(TableInfo table, string keyName, int total);

        long GetRowCount(string tableName);
    }
}
