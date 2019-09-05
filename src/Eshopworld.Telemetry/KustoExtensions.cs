namespace Eshopworld.Telemetry
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Kusto.Data.Common;

    /// <summary>
    /// Contains extensions to clients in the Kusto SDK.
    /// </summary>
    public static class KustoExtensions
    {
        /// <summary>
        /// Generates a Kusto table for a specific <see cref="Type"/>, by mapping it's properties to columns.
        /// </summary>
        /// <param name="client">The <see cref="ICslAdminProvider"/> that we are extending.</param>
        /// <param name="type">The <see cref="Type"/> that we are generating a table for.</param>
        /// <returns>The name of the table created.</returns>
        public static string GenerateTableFromType(this ICslAdminProvider client, Type type)
        {
            var tableName = type.Name;
            var tables = new List<string>();
            var command = CslCommandGenerator.GenerateTablesShowCommand();

            var reader = client.ExecuteControlCommand(command);

            while (reader.Read())
            {
                tables.Add(reader.GetString(0));
            }

            if (tables.Contains(tableName))
            {
                command = CslCommandGenerator.GenerateTableShowCommand(tableName);
                reader = client.ExecuteControlCommand(command);

                var existingColumns = new List<KustoColumn>();

                while (reader.Read())
                {
                    existingColumns.Add(new KustoColumn(reader.GetString(0), reader.GetString(1)));
                }

                var newColumns = type.GetProperties().Select(property => new KustoColumn(property.Name, property.PropertyType)).ToList();
                var toAddColumns = CompareAndMigrateSchema(existingColumns, newColumns).Select(c => new ColumnSchema(c.Name, null, null, c.CslType.ToString()));

                command = CslCommandGenerator.GenerateTableCreateCommand(new TableSchema(tableName, toAddColumns));
                client.ExecuteControlCommand(command);

                return tableName;
            }


            var columns = type.GetProperties().Select(property => new ColumnSchema(property.Name, property.PropertyType.FullName)).ToList();
            command = CslCommandGenerator.GenerateTableCreateCommand(new TableSchema(tableName, columns));

            client.ExecuteControlCommand(command);

            return tableName;
        }

        /// <summary>
        /// Generates a Kusto table mapping for a specific <see cref="Type"/>, by mapping it's properties to column mappings.
        /// </summary>
        /// <param name="client">The <see cref="ICslAdminProvider"/> client that we are extending.</param>
        /// <param name="type">The <see cref="Type"/> that we are generating the JSON mapping for.</param>
        /// <returns>The name of the mapping created.</returns>
        public static string GenerateTableJsonMappingFromType(this ICslAdminProvider client, Type type)
        {
            var tableName = type.Name;
            var mappingName = $"{tableName}_mapping";
            var tableMappings = new List<string>();
            var command = CslCommandGenerator.GenerateTableJsonMappingsShowCommand(tableName);

            var reader = client.ExecuteControlCommand(command);

            while (reader.Read())
            {
                tableMappings.Add(reader.GetString(0));
            }

            if (tableMappings.Contains(mappingName)) return mappingName;

            var mappings = type.GetProperties().Select(property => new JsonColumnMapping { ColumnName = property.Name, JsonPath = $"$.{property.Name}" }).ToList();
            command = CslCommandGenerator.GenerateTableJsonMappingCreateCommand(tableName, mappingName, mappings);
            client.ExecuteControlCommand(command);

            return mappingName;
        }

        /// <summary>
        /// Compares and produces a list of new columns based on a schema comparison.
        ///     It also checks if the columns that are the same, haven't changed their types, which prevents them from being migrated.
        /// </summary>
        /// <param name="existingColumns">The list of existing columns.</param>
        /// <param name="newColumns">The list of columns in the new schema.</param>
        /// <returns>The list of columns to add.</returns>
        internal static IEnumerable<KustoColumn> CompareAndMigrateSchema(IEnumerable<KustoColumn> existingColumns, IEnumerable<KustoColumn> newColumns)
        {
            var newCols = newColumns.ToList();
            var existingCols = existingColumns.ToList();

            var toAdd = newCols.Except(existingCols);
            var same = newCols.Intersect(existingCols);

            foreach (var column in same)
            {
                var existing = existingCols.First(c => c.Name == column.Name);

                if (column.CslType != existing.CslType)
                {
                    throw new InvalidSchemaMigrationException($"Can't migrate column {column.Name}' from type {column.CslType} to type {existing.CslType}");
                }
            }

            return toAdd;
        }
    }
}
