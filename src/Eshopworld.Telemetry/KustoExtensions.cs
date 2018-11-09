namespace Eshopworld.Telemetry
{
    using System;
    using System.Linq;
    using Kusto.Data.Common;
    using Kusto.Data.Exceptions;

    public static class KustoExtensions
    {
        public static string GenerateTableFromType(this ICslAdminProvider client, Type type)
        {
            var tableName = type.Name;
            var exists = true;
            var command = CslCommandGenerator.GenerateTableShowCommand(tableName);

            try
            {
                client.ExecuteControlCommand(command);
            }
            catch (KustoBadRequestException ex)
            {
                if (ex.ErrorMessage.Contains("'Table' was not found"))
                    exists = false;
                else
                    throw;
            }

            if (exists) return tableName;

            var columns = type.GetProperties().Select(property => new Tuple<string, string>(property.Name, property.PropertyType.FullName)).ToList();
            command = CslCommandGenerator.GenerateTableCreateCommand(tableName, columns);
            client.ExecuteControlCommand(command);

            return tableName;
        }

        public static string GenerateTableJsonMappingFromType(this ICslAdminProvider client, Type type)
        {
            var tableName = type.Name;
            var mappingName = $"{tableName}_mapping";
            var exists = true;
            var command = CslCommandGenerator.GenerateTableJsonMappingShowCommand(tableName, mappingName);

            try
            {
                client.ExecuteControlCommand(command);
            }
            catch (KustoBadRequestException ex)
            {
                if (ex.ErrorMessage.Contains("'JsonMappingPersistent' was not found"))
                    exists = false;
                else
                    throw;
            }

            if (exists) return mappingName;

            var mappings = type.GetProperties().Select(property => new JsonColumnMapping { ColumnName = property.Name, JsonPath = $"$.{property.Name}" }).ToList();
            command = CslCommandGenerator.GenerateTableJsonMappingCreateCommand(tableName, mappingName, mappings);
            client.ExecuteControlCommand(command);

            return mappingName;
        }

    }
    public class KustoEvent { }
}
