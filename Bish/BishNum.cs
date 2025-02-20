namespace Bish {

    internal class BishNum(double value) {
        public double value = value;

        public static implicit operator BishNum(double value) {
            return new(value);
        }

        public static implicit operator BishNum(BishInt value) {
            if (value.isNaN) return new(double.NaN);
            if (value.isInf && value.value > 0) return new(double.PositiveInfinity);
            if (value.isInf && value.value < 0) return new(double.NegativeInfinity);
            return new(value.value);
        }

        public static bool operator ==(BishNum a, BishNum b) {
            return a.value == b.value;
        }

        public static bool operator !=(BishNum a, BishNum b) {
            return a.value != b.value;
        }

        public static BishNum operator +(BishNum a, BishNum b) {
            return new(a.value + b.value);
        }

        public static BishNum operator -(BishNum a, BishNum b) {
            return new(a.value - b.value);
        }

        public static BishNum operator *(BishNum a, BishNum b) {
            return new(a.value * b.value);
        }

        public static BishNum operator /(BishNum a, BishNum b) {
            return new(a.value / b.value);
        }

        public static BishNum operator %(BishNum a, BishNum b) {
            return new(a.value % b.value);
        }

        public static BishNum operator ^(BishNum a, BishNum b) {
            return new(Math.Pow(a.value, b.value));
        }

        public static BishNum operator +(BishNum a) {
            return new(+a.value);
        }

        public static BishNum operator -(BishNum a) {
            return new(-a.value);
        }

        public static bool operator <(BishNum a, BishNum b) {
            return a.value < b.value;
        }

        public static bool operator >(BishNum a, BishNum b) {
            return a.value > b.value;
        }

        public static bool operator <=(BishNum a, BishNum b) {
            return a.value <= b.value;
        }

        public static bool operator >=(BishNum a, BishNum b) {
            return a.value >= b.value;
        }

        public static BishNum Parse(string str) {
            return new(double.Parse(str));
        }

        public override bool Equals(object? obj) {
            if (ReferenceEquals(this, obj)) return true;
            if (obj is null) return false;
            if (obj is BishNum num) return num.value == value;
            if (obj is double d) return d == value;
            return false;
        }

        public override int GetHashCode() {
            return value.GetHashCode() ^ 0x0D000721;
        }

        public override string ToString() {
            if (double.IsNaN(value)) return "NaN";
            if (double.IsPositiveInfinity(value)) return "+Inf";
            if (double.IsNegativeInfinity(value)) return "-Inf";
            return value.ToString();
        }

        public int CompareTo(BishNum? other) {
            return value.CompareTo(other?.value);
        }

        public int CompareTo(object? obj) {
            return value.CompareTo(obj);
        }
    }
}