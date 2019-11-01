using System;
using System.Collections.Generic;
using Kusto.Cloud.Platform.Utils;
using Kusto.Data.Common;

namespace Eshopworld.Telemetry
{
    internal class KustoColumn : IEquatable<KustoColumn>
    {
        private static Dictionary<string, string> NameMaps = new Dictionary<string, string>
        {
            { "i32", "int32" },
            { "stringbuffer", "string" }
        };
            
        internal KustoColumn(string name, string cslType)
        {
            Name = name;

            cslType = cslType.ToLower();
            CslType = CslType.FromCslType(NameMaps.GetOrDefault(cslType, cslType));
            ClrType = CslType.GetCorrespondingClrType();
        }

        internal KustoColumn(string name, Type type)
        {
            Name = name;
            CslType = CslType.FromClrType(type);
            ClrType = type;
        }

        public string Name { get; }

        public CslType CslType { get; }

        public Type ClrType { get; set; }

        public bool Equals(KustoColumn other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Name == other.Name;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;

            return obj.GetType() == GetType() && Equals((KustoColumn) obj);
        }

        public override int GetHashCode()
        {
                return Name != null ? Name.GetHashCode() : 0;
        }
    }
}
