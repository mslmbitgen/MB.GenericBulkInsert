using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Data;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Reflection;
using System.Data.Common;

namespace MB.GenericBulkInsert;

public static class BulkExtensions
{
    public static async Task BulkInsertAsync<T>(this DbContext context, IEnumerable<T> entities, int batchSize = 5000) where T : class
    {
        if (entities == null || !entities.Any()) return;

        var entityType = context.Model.FindEntityType(typeof(T))
                         ?? throw new InvalidOperationException($"Entity type {typeof(T).Name} not found in the DbContext model.");

        var tableName = entityType.GetTableName()
                         ?? throw new InvalidOperationException($"Table name for {typeof(T).Name} could not be determined.");

        var schema = entityType.GetSchema() ?? "dbo";
        var fullTableName = $"{schema}.{tableName}";

        var properties = GetEntityProperties(entityType);
        var dataTable = ConvertToDataTable(entities, properties);

        await using var connection = context.Database.GetDbConnection();

        // 🎯 Microsoft.Data.SqlClient.SqlConnection Kullanımı
        if (connection is not SqlConnection sqlConnection)
            throw new InvalidOperationException($"BulkInsertAsync only supports SQL Server. Current connection type: {connection.GetType().FullName}");

        var wasClosed = connection.State == ConnectionState.Closed;
        if (wasClosed) await connection.OpenAsync();

        await using var transaction = (SqlTransaction)await sqlConnection.BeginTransactionAsync();
        try
        {
            await BulkCopyAsync(dataTable, fullTableName, sqlConnection, transaction, batchSize);
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        if (wasClosed) await connection.CloseAsync();
    }

    private static PropertyInfo[] GetEntityProperties(IEntityType entityType)
    {
        return entityType.GetProperties()
                         .Where(p => !p.IsShadowProperty() && !p.IsPrimaryKey() && p.ValueGenerated != ValueGenerated.OnAdd)
                         .Select(p => p.PropertyInfo)
                         .Where(p => p != null)
                         .ToArray();
    }

    private static DataTable ConvertToDataTable<T>(IEnumerable<T> entities, PropertyInfo[] properties)
    {
        var dataTable = new DataTable();
        foreach (var prop in properties)
            dataTable.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);

        foreach (var entity in entities)
        {
            var values = properties.Select(p => p.GetValue(entity) ?? DBNull.Value).ToArray();
            dataTable.Rows.Add(values);
        }

        return dataTable;
    }

    private static async Task BulkCopyAsync(DataTable dataTable, string tableName, SqlConnection sqlConnection, SqlTransaction transaction, int batchSize)
    {
        using var sqlBulkCopy = new SqlBulkCopy(sqlConnection, SqlBulkCopyOptions.Default, transaction)
        {
            DestinationTableName = tableName,
            BatchSize = batchSize
        };

        foreach (DataColumn column in dataTable.Columns)
            sqlBulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);

        await sqlBulkCopy.WriteToServerAsync(dataTable);
    }
}
