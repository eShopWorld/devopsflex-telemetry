using Kusto.Data.Common;

namespace Eshopworld.Telemetry
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

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
        public static async Task<string> GenerateTableFromType(this ICslAdminProvider client, Type type)
        {
            var tableName = type.Name;
            var tables = new List<string>();
            var command = CslCommandGenerator.GenerateTablesShowCommand();

            var reader = await client.ExecuteControlCommandAsync(client.DefaultDatabaseName, command);

            while (reader.Read())
            {
                tables.Add(reader.GetString(0));
            }

            if (tables.Contains(tableName)) return tableName;

            var columns = type.GetProperties().Select(property => Tuple.Create(property.Name, property.PropertyType.FullName)).ToList();
            command = CslCommandGenerator.GenerateTableCreateCommand(tableName, columns);
            await client.ExecuteControlCommandAsync(client.DefaultDatabaseName, command);

            return tableName;
        }
       
        /// <summary>
        /// Generates a Kusto table mapping for a specific <see cref="Type"/>, by mapping it's properties to column mappings.
        /// </summary>
        /// <param name="client">The <see cref="ICslAdminProvider"/> client that we are extending.</param>
        /// <param name="type">The <see cref="Type"/> that we are generating the JSON mapping for.</param>
        /// <returns>The name of the mapping created.</returns>
        public static async Task<string> GenerateTableJsonMappingFromType(this ICslAdminProvider client, Type type)
        {
            var tableName = type.Name;
            var mappingName = $"{tableName}_mapping";
            var tableMappings = new List<string>();
            var command = CslCommandGenerator.GenerateTableJsonMappingsShowCommand(tableName);

            var reader = await client.ExecuteControlCommandAsync(client.DefaultDatabaseName, command);

            while (reader.Read())
            {
                tableMappings.Add(reader.GetString(0));
            }

            if (tableMappings.Contains(mappingName)) return mappingName;

            var mappings = type.GetProperties().Select(property => new JsonColumnMapping { ColumnName = property.Name, JsonPath = $"$.{property.Name}" }).ToList();
            command = CslCommandGenerator.GenerateTableJsonMappingCreateCommand(tableName, mappingName, mappings);
            await client.ExecuteControlCommandAsync(client.DefaultDatabaseName, command);

            return mappingName;
        }

    }
}
