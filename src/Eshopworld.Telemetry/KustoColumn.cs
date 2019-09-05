namespace Eshopworld.Telemetry
{
    using System;
    using Kusto.Data.Common;

    internal class KustoColumn : IEquatable<KustoColumn>
    {
        internal KustoColumn(string name, string cslType)
        {
            Name = name;
            CslType = CslType.FromCslType(cslType);
        }

        internal KustoColumn(string name, Type type)
        {
            Name = name;
            CslType = CslType.FromClrType(type);
        }

        public string Name { get; }

        public CslType CslType { get; }

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
