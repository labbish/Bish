namespace Bish {

    internal class BishInt(int value, bool isInf = false, bool isNaN = false) {
        public static readonly BishInt Inf = new(+1, isInf: true);
        public static readonly BishInt NaN = new(0, isNaN: true);
        public bool isInf = isInf;
        public bool isNaN = isNaN;
        public int value = value;

        public static implicit operator BishInt(int value) {
            return new(value);
        }

        public static explicit operator BishInt(BishNum value) {
            if (double.IsPositiveInfinity(value.value)) return Inf;
            if (double.IsNegativeInfinity(value.value)) return -Inf;
            if (double.IsNaN(value.value)) return NaN;
            return new((int)value.value);
        }

        private static int Sign(int x) {
            if (x > 0) return 1;
            if (x == 0) return 0;
            if (x < 0) return -1;
            return BishUtils.Impossible();
        }

        public static bool operator ==(BishInt a, BishInt b) {
            if (a.isNaN || b.isNaN) return false;
            if (a.isInf && b.isInf && a.value > 0 && b.value > 0) return true;
            if (a.isInf && b.isInf && a.value < 0 && b.value < 0) return true;
            if (!a.isInf && !b.isInf && a.value == b.value) return true;
            return false;
        }

        public static bool operator !=(BishInt a, BishInt b) {
            return !(a == b);
        }

        public static BishInt operator +(BishInt a) {
            return a;
        }

        public static BishInt operator -(BishInt a) {
            return new(-a.value, a.isInf, a.isNaN);
        }

        public static BishInt operator +(BishInt a, BishInt b) {
            if (a == NaN || b == NaN) return NaN;
            if (a == Inf && b != -Inf) return Inf;
            if (a != -Inf && b == Inf) return Inf;
            if (a == -Inf && b != Inf) return -Inf;
            if (a != Inf && b == -Inf) return -Inf;
            if (a == Inf && b == -Inf) return NaN;
            if (a == -Inf && b == Inf) return NaN;
            return new(a.value + b.value);
        }

        public static BishInt operator -(BishInt a, BishInt b) {
            return a + (-b);
        }

        public static BishInt operator *(BishInt a, BishInt b) {
            if (a.isNaN || b.isNaN) return NaN;
            if (a.isInf && b == 0) return NaN;
            if (b.isInf && a == 0) return NaN;
            if (a.isInf || b.isInf) return new(Sign(a.value * b.value), isInf: true);
            return new(a.value * b.value);
        }

        public static dynamic operator /(BishInt a, BishInt b) {
            if (a % b == 0) return new BishInt(a.value / b.value);
            return (BishNum)a / (BishNum)b;
        }

        public static BishInt operator %(BishInt a, BishInt b) {
            if (a.isNaN || b.isNaN) return NaN;
            if (a.isInf && !b.isInf) return a;
            if (!a.isInf && b.isInf) return a;
            if (a.isInf && b.isInf) return NaN;
            return new(a.value % b.value);
        }

        public static dynamic operator ^(BishInt a, BishInt b) {
            BishNum result = (BishNum)a ^ (BishNum)b;
            if (b >= 0) return (BishInt)result;
            return result;
        }

        public static BishInt operator ++(BishInt a) {
            if (!a.isInf && !a.isNaN) a.value++;
            return a;
        }

        public static BishInt operator --(BishInt a) {
            if (!a.isInf && !a.isNaN) a.value--;
            return a;
        }

        public static bool operator <(BishInt a, BishInt b) {
            if (a == b) return false;
            if (a.isNaN || b.isNaN) return false;
            if (a == -Inf && b != -Inf) return true;
            if (b == Inf && a != Inf) return true;
            return a.value < b.value;
        }

        public static bool operator >(BishInt a, BishInt b) {
            return b < a;
        }

        public static bool operator <=(BishInt a, BishInt b) {
            return a < b || a == b;
        }

        public static bool operator >=(BishInt a, BishInt b) {
            return a > b || a == b;
        }

        public static BishInt Parse(string str) {
            return new(int.Parse(str));
        }

        public override bool Equals(object? obj) {
            if (ReferenceEquals(this, obj)) return true;
            if (obj is null) return false;
            if (obj is BishInt x) return this == x;
            return false;
        }

        public override int GetHashCode() {
            return HashCode.Combine(value, isInf, isNaN);
        }

        public override string ToString() {
            if (isNaN) return "NaN";
            if (isInf && value > 0) return "+Inf";
            if (isInf && value < 0) return "-Inf";
            return value.ToString();
        }

        public int CompareTo(dynamic? other) {
            if (other is null) return 1;
            if (this > other) return 1;
            if (this == other) return 0;
            if (this < other) return -1;
            return BishUtils.Impossible();
        }
    }
}