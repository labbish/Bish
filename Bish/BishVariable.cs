namespace Bish {

    internal class BishVariable(string? name, dynamic? value = null, string? type = null, bool? nullable = null, bool isConst = false) {
        public string? name = name;
        public dynamic? value = value;
        public string? type = type ?? GetTypeName(value);
        public bool nullable = nullable ?? false;
        public bool isConst = isConst;

        public static readonly Dictionary<Type, string> TypeNames = [];

        static BishVariable() {
            TypeNames[typeof(int)] = "int";
            TypeNames[typeof(double)] = "num";
            TypeNames[typeof(string)] = "string";
            TypeNames[typeof(bool)] = "bool";
            TypeNames[typeof(BishInterval)] = "interval";
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

        public static BishVariable operator %(BishVariable a, BishVariable b) {
            return new BishVariable(null, a.value % b.value);
        }

        public static BishVariable operator ^(BishVariable a, BishVariable b) {
            if (a.value is int && b.value is int) {
                return new BishVariable(null, (int)Math.Pow(a.value, b.value));
            }
            return new BishVariable(null, Math.Pow(a.value, b.value));
        }

        public static BishVariable TriCompare(BishVariable a, BishVariable b) {
            if (a.value == null && b.value == null)
                return new BishVariable(null, 0);
            if (a.value == null || b.value == null)
                return BishUtils.Error("Cannot Compare Between Null");
            return new BishVariable(null, a.value!.CompareTo(b.value!));
        }

        public static BishVariable operator ==(BishVariable a, BishVariable b) {
            return new BishVariable(null, a.value == b.value);
        }

        public static BishVariable operator !=(BishVariable a, BishVariable b) {
            return new BishVariable(null, a.value != b.value);
        }

        public static BishVariable operator <(BishVariable a, BishVariable b) {
            return new BishVariable(null, a.value < b.value);
        }

        public static BishVariable operator <=(BishVariable a, BishVariable b) {
            return new BishVariable(null, a.value <= b.value);
        }

        public static BishVariable operator >(BishVariable a, BishVariable b) {
            return new BishVariable(null, a.value > b.value);
        }

        public static BishVariable operator >=(BishVariable a, BishVariable b) {
            return new BishVariable(null, a.value >= b.value);
        }

        public static BishVariable operator ++(BishVariable a) {
            BishUtils.Assert(!a.isConst, $"Cannot modify const var: {a.name}");
            return new(null, a.value++);
        }

        public static BishVariable operator --(BishVariable a) {
            BishUtils.Assert(!a.isConst, $"Cannot modify const var: {a.name}");
            return new(null, a.value--);
        }

        public static BishVariable operator !(BishVariable a) {
            return new BishVariable(null, !a.value);
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
                + $" type <{(isConst ? "const " : "")}{type ?? "(?)"}{(nullable ? "?" : "")}>";
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