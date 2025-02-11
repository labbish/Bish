namespace Bish {

    internal class BishVariable(string? name, dynamic? value = null, string? type = null) {
        public string? name = name;
        public string? type = type;
        public dynamic? value = value;

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

        public override string ToString() {
            dynamic? value = this.value switch {
                string str => $"\"{str}\"",
                _ => this.value,
            };
            return $"var {name ?? "[TEMP]"} with value {value}, type <{"?"}>";
        }

        public override bool Equals(object? obj) {
            return obj is BishVariable variable &&
                EqualityComparer<dynamic?>.Default.Equals(value, variable.value);
        }

        public override int GetHashCode() {
            return HashCode.Combine(name, type, value);
        }

        public static bool SameVar(BishVariable a, BishVariable b) {
            return a.name == b.name && a.value == b.value;
        }
    }
}