namespace Bish {

    internal class BishVariable(string? name, dynamic? value = null, string? type = null, bool? nullable = null) {
        public string? name = name;
        public dynamic? value = value;
        public string? type = type ?? GetTypeName(value);
        public bool nullable = nullable ?? false;

        public static readonly Dictionary<Type, string> TypeNames = [];

        static BishVariable() {
            TypeNames[typeof(int)] = "int";
            TypeNames[typeof(double)] = "num";
            TypeNames[typeof(string)] = "string";
            TypeNames[typeof(bool)] = "bool";
        }

        public static string? GetTypeName(dynamic? value) {
            return value == null ? null : TypeNames[value.GetType()];
        }

        public static BishVariable operator +(BishVariable a, BishVariable b) {
            return new BishVariable(null, a.value + b.value);
        }

        public static BishVariable operator -(BishVariable a, BishVariable b) {
            return new BishVariable(null, a.value - b.value);
        }

        public static BishVariable operator +(BishVariable a) {
            return new BishVariable(null, +a.value);
        }

        public static BishVariable operator -(BishVariable a) {
            return new BishVariable(null, -a.value);
        }

        public static BishVariable operator *(BishVariable a, BishVariable b) {
            return new BishVariable(null, a.value * b.value);
        }

        public static BishVariable operator /(BishVariable a, BishVariable b) {
            return new BishVariable(null, a.value / b.value);
        }

        public static BishVariable operator ^(BishVariable a, BishVariable b) {
            return new BishVariable(null, Math.Pow(a.value, b.value));
        }

        public static bool operator ==(BishVariable a, BishVariable b) {
            return a.value == b.value;
        }

        public static bool operator !=(BishVariable a, BishVariable b) {
            return a.value != b.value;
        }

        public static BishVariable operator ++(BishVariable a) {
            return new(null, a.value++);
        }

        public static BishVariable operator --(BishVariable a) {
            return new(null, a.value--);
        }

        public override string ToString() {
            dynamic? value = this.value switch {
                string str => $"\"{str}\"",
                true => "true",
                false => "false",
                null => "null",
                _ => this.value,
            };
            return $"var [{name ?? "TEMP"}] with value {value},"
                + $" type <{type ?? "(?)"}{(nullable ? "?" : "")}>";
        }

        public override bool Equals(object? obj) {
            return obj is BishVariable variable &&
                EqualityComparer<dynamic?>.Default.Equals(value, variable.value);
        }

        public override int GetHashCode() {
            return HashCode.Combine<string?, dynamic?, dynamic?>(name, type, value);
        }

        public static bool SameVar(BishVariable a, BishVariable b) {
            return a.name == b.name && a.value == b.value;
        }

        public BishVariable GetNullChecked() {
            if (nullable || value != null || name == null) return this;
            return BishUtils.Error($"Var [{name ?? "TEMP"}] is Null but not Nullable");
        }
    }
}