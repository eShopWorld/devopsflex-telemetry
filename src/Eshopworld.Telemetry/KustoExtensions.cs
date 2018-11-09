namespace Eshopworld.Telemetry
{
    using Kusto.Data.Common;
    using Kusto.Data.Exceptions;
    using System;
    using System.Linq;

    public static class KustoExtensions
    {
        public static string GenerateTableFromType(this ICslAdminProvider client, Type type)
        {
            var tableName = type.Name;
            var command = CslCommandGenerator.GenerateTableShowCommand(tableName);

            try
            {
                client.ExecuteControlCommand(command);
                return tableName;
            }
            catch (KustoBadRequestException ex)
            {
                if (!ex.ErrorMessage.Contains("'Table' was not found"))
                    throw;
            }

            var columns = type.GetProperties().Select(property => new Tuple<string, string>(property.Name, property.PropertyType.FullName)).ToList();
            command = CslCommandGenerator.GenerateTableCreateCommand(tableName, columns);
            client.ExecuteControlCommand(command);

            return tableName;
        }

        public static string GenerateTableJsonMappingFromType(this ICslAdminProvider client, Type type)
        {
            var tableName = type.Name;
            var mappingName = $"{tableName}_mapping";
            var command = CslCommandGenerator.GenerateTableJsonMappingShowCommand(tableName, mappingName);

            try
            {
                client.ExecuteControlCommand(command);
                return mappingName;
            }
            catch (KustoBadRequestException ex)
            {
                if (!ex.ErrorMessage.Contains("'JsonMappingPersistent' was not found"))
                    throw;
            }

            var mappings = type.GetProperties().Select(property => new JsonColumnMapping { ColumnName = property.Name, JsonPath = $"$.{property.Name}" }).ToList();
            command = CslCommandGenerator.GenerateTableJsonMappingCreateCommand(tableName, mappingName, mappings);
            client.ExecuteControlCommand(command);

            return mappingName;
        }

    }
}
