namespace Bish {

    internal class BishVariable {
        public string? name;
        public dynamic? value;
        public BishTypeInfo type;

        public BishVariable(string? name, dynamic? value = null, string? typeName = null) {
            this.name = name;
            this.value = value;
            type = new(value, typeName);
        }

        public BishVariable(string? name, BishTypeInfo type, dynamic? value = null) {
            this.name = name;
            this.value = value;
            this.type = type;
        }

        public static BishVariable operator +(BishVariable a, BishVariable b) {
            return new(null, value: a.value + b.value);
        }

        public static BishVariable operator -(BishVariable a, BishVariable b) {
            return new(null, value: a.value - b.value);
        }

        public static BishVariable operator +(BishVariable a) {
            return new(null, value: +a.value);
        }

        public static BishVariable operator -(BishVariable a) {
            return new(null, value: -a.value);
        }

        public static BishVariable operator *(BishVariable a, BishVariable b) {
            return new(null, value: a.value * b.value);
        }

        public static BishVariable operator /(BishVariable a, BishVariable b) {
            return new(null, value: a.value / b.value);
        }

        public static BishVariable operator %(BishVariable a, BishVariable b) {
            return new(null, value: a.value % b.value);
        }

        public static BishVariable operator ^(BishVariable a, BishVariable b) {
            return new(null, value: a.value ^ b.value);
        }

        public static BishVariable TriCompare(BishVariable a, BishVariable b) {
            if (a.value is null && b.value is null)
                return new(null, 0);
            if (a.value is null || b.value is null)
                return BishUtils.Error("Cannot Compare Between Null");
            return new(null, value: a.value!.CompareTo(b.value!));
        }

        public static BishVariable operator ==(BishVariable a, BishVariable b) {
            return new(null, value: a.value == b.value);
        }

        public static BishVariable operator !=(BishVariable a, BishVariable b) {
            return new(null, value: a.value != b.value);
        }

        public static BishVariable operator <(BishVariable a, BishVariable b) {
            return new(null, value: a.value < b.value);
        }

        public static BishVariable operator <=(BishVariable a, BishVariable b) {
            return new(null, value: a.value <= b.value);
        }

        public static BishVariable operator >(BishVariable a, BishVariable b) {
            return new(null, value: a.value > b.value);
        }

        public static BishVariable operator >=(BishVariable a, BishVariable b) {
            return new(null, value: a.value >= b.value);
        }

        public static BishVariable operator &(BishVariable a, BishVariable b) {
            return BishUtils.Error("Bish has no Bit Operators");
        }

        public static BishVariable operator |(BishVariable a, BishVariable b) {
            if (a.value is BishTypeInfo && b.value is BishTypeInfo)
                return new(null, value: a.value | b.value);
            return BishUtils.Error("Bish has no Bit Operators");
        }

        public static BishVariable operator ++(BishVariable a) {
            BishUtils.Assert(!a.type.isConst, $"Cannot modify const var: {a.name}");
            return new(null, value: a.value++);
        }

        public static BishVariable operator --(BishVariable a) {
            BishUtils.Assert(!a.type.isConst, $"Cannot modify const var: {a.name}");
            return new(null, value: a.value--);
        }

        public static BishVariable operator !(BishVariable a) {
            return new(null, value: !a.value);
        }

        public BishVariable Exec(BishInArg[] args) {
            if (value is null) BishUtils.Error($"Cannot Execute null");
            return value!.Exec(args);
        }

        public string ValueString() {
            return value switch {
                true => "true",
                false => "false",
                null => "null",
                BishFunc => "[func]",
                _ => value.ToString(),
            };
        }

        public string DebugValueString() {
            return value switch {
                string str => $"\"{AntiEscape(str)}\"",
                _ => ValueString(),
            };
        }

        public override string ToString() {
            return $"var [{name ?? "TEMP"}] with value {DebugValueString()}, type <{type}>";
        }

        private static string AntiEscape(string str) {
            string s = str;
            s = s.Replace("\\", "\\\\");
            s = s.Replace("\a", "\\a");
            s = s.Replace("\b", "\\b");
            s = s.Replace("\e", "\\e");
            s = s.Replace("\f", "\\f");
            s = s.Replace("\n", "\\n");
            s = s.Replace("\r", "\\r");
            s = s.Replace("\t", "\\t");
            s = s.Replace("\v", "\\v");
            s = s.Replace("\'", "\\\'");
            s = s.Replace("\"", "\\\"");
            s = s.Replace("\0", "\\0");
            return s;
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
            if (type.nullable || value is not null || name is null) return this;
            return BishUtils.Error($"Var [{name ?? "TEMP"}] is Null but not Nullable");
        }
    }
}