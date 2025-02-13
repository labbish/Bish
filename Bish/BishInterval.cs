using System.Text;

namespace Bish {

    internal class BishSingleInterval {
        public bool fromPoint; //is close
        public double from;
        public bool toPoint; //is close
        public double to;

        public BishSingleInterval(bool fromP, double from, bool toP, double to) {
            BishUtils.Assert(from <= to, "Interval Length cannot be Negative");
            fromPoint = fromP;
            this.from = from;
            toPoint = toP;
            this.to = to;
        }

        public static bool operator <(double x, BishSingleInterval I) {
            if (I.from < x && x < I.to) return true;
            if (x == I.from && I.fromPoint) return true;
            if (x == I.to && I.toPoint) return true;
            return false;
        }

        public static bool operator >(double _, BishSingleInterval __) {
            return BishUtils.Error();
        }

        public static bool operator <=(BishSingleInterval I1, BishSingleInterval I2) {
            bool left = I1.from > I2.from || (I1.from == I2.from && (!I1.fromPoint || I2.fromPoint));
            bool right = I1.to < I2.to || (I1.to == I2.to && (I1.toPoint || !I2.toPoint));
            return left && right;
        }

        public static bool operator >=(BishSingleInterval _, BishSingleInterval __) {
            return BishUtils.Error();
        }

        public static bool operator ==(BishSingleInterval I1, BishSingleInterval I2) {
            return I1.from == I2.from && I1.fromPoint == I2.fromPoint
                && I1.to == I2.to && I1.fromPoint == I2.fromPoint;
        }

        public static bool operator !=(BishSingleInterval I1, BishSingleInterval I2) {
            return !(I1 == I2);
        }

        public override bool Equals(object? obj) {
            if (ReferenceEquals(this, obj)) {
                return true;
            }

            if (obj is null) {
                return false;
            }

            return this == (BishSingleInterval)obj;
        }

        public override int GetHashCode() {
            return HashCode.Combine(from, fromPoint, to, toPoint);
        }

        public override string ToString() {
            return $"{(fromPoint ? "[" : "(")}{from}, {to}{(toPoint ? "]" : ")")}";
        }
    }

    internal class BishInterval {
        private List<BishSingleInterval> intervals;

        public BishInterval() {
            intervals = [];
        }

        public BishInterval(BishSingleInterval interval) {
            intervals = [interval];
        }

        public BishInterval(bool fromP, double from, bool toP, double to) {
            intervals = [new(fromP, from, toP, to)];
        }

        public BishInterval(List<BishSingleInterval> intervals) {
            this.intervals = [.. intervals];
        }

        public static implicit operator BishInterval(BishSingleInterval interval) {
            return new BishInterval(interval);
        }

        public static bool operator <(double x, BishInterval I) {
            foreach (var interval in I.intervals) if (x < interval) return true;
            return false;
        }

        public static bool operator >(double _, BishInterval __) {
            return BishUtils.Error("operator > is not implemented for (num, interval)");
        }

        public static bool operator <=(BishSingleInterval I1, BishInterval I2) {
            foreach (var i2 in I2.intervals) if (I1 <= i2) return true;
            return false;
        }

        public static bool operator >=(BishSingleInterval _, BishInterval __) {
            return BishUtils.Error();
        }

        public static bool operator <=(BishInterval I1, BishInterval I2) {
            foreach (var i1 in I1.intervals) if (!(i1 <= I2)) return false;
            return true;
        }

        public static bool operator >=(BishInterval _, BishInterval __) {
            return BishUtils.Error("operator >= is not implemented for (interval, interval)");
        }

        public static bool operator ==(BishInterval I1, BishInterval I2) {
            List<BishSingleInterval> i1 = [.. I1.intervals];

            return true;
        }

        public static bool operator !=(BishInterval I1, BishInterval I2) {
            return !(I1 == I2);
        }

        public override bool Equals(object? obj) {
            if (ReferenceEquals(this, obj)) {
                return true;
            }

            if (obj is null) {
                return false;
            }

            return this == (BishInterval)obj;
        }

        public override int GetHashCode() {
            return HashCode.Combine(intervals);
        }

        public override string ToString() {
            return string.Join("+", intervals.Select(interval => interval.ToString()));
        }
    }
}