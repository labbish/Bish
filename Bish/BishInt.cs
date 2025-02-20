﻿namespace Bish {

    internal class BishInt(int value, bool isInf = false, bool isNaN = false) {
        public static readonly BishInt Inf = new(+1, isInf: true);
        public static readonly BishInt NaN = new(0, isNaN: true);
        public bool isInf = isInf;
        public bool isNaN = isNaN;
        public int value = value;

        private static int Sign(int x) {
            if (x > 0) return 1;
            if (x == 0) return 0;
            if (x < 0) return -1;
            return BishUtils.Error();
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
            if (a.isInf && b.value == 0) return NaN;
            if (b.isInf && a.value == 0) return NaN;
            if (a.isInf || b.isInf) return new(Sign(a.value * b.value), isInf: true);
            return new(a.value * b.value);
        }

        public static double operator /(BishInt a, BishInt b) {
            return BishUtils.NotImplemented();
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
    }
}