namespace Bish {

    internal class BishVariable(string? name, dynamic? value = null) {
        public string? name = name;
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

        public override string ToString() {
            dynamic? value;
            if (this.value is string str) value = $"\"{str}\"";
            else value = this.value;
            return $"var {name ?? "[TEMP]"} with value {value}";
        }
    }
}