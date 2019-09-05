using System;
using System.Diagnostics.CodeAnalysis;

namespace Eshopworld.Telemetry
{
    [SuppressMessage("Major Code Smell", "S3925:\"ISerializable\" should be implemented correctly", Justification = "Never serialized")]
    public class InvalidSchemaMigrationException : Exception
    {
        public InvalidSchemaMigrationException(string message)
            : base(message)
        { }
    }
}
