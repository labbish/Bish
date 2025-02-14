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

        public BishSingleInterval(BishSingleInterval other) {
            fromPoint = other.fromPoint;
            from = other.from;
            toPoint = other.toPoint;
            to = other.to;
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
            bool left = I1.from > I2.from || (I1.from == I2.from && (I1.fromPoint || !I2.fromPoint));
            bool right = I1.to < I2.to || (I1.to == I2.to && (!I1.toPoint || I2.toPoint));
            return left && right;
        }

        public static bool operator >=(BishSingleInterval _, BishSingleInterval __) {
            return BishUtils.Error();
        }

        public bool IsEmpty() {
            return from == to && (!fromPoint || !toPoint);
        }

        public static BishSingleInterval Empty = new(false, 0, false, 0);

        public static bool operator ==(BishSingleInterval I1, BishSingleInterval I2) {
            return (I1.IsEmpty() && I2.IsEmpty())
                || (I1.from == I2.from && I1.fromPoint == I2.fromPoint
                && I1.to == I2.to && I1.fromPoint == I2.fromPoint);
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
            return $"{(fromPoint ? "[" : "(")}{from},{to}{(toPoint ? "]" : ")")}";
        }

        public static bool Intersect(BishSingleInterval i1, BishSingleInterval i2) {
            double a1 = i1.from, a2 = i2.from, b1 = i1.to, b2 = i2.to;
            if (a1 == b2 && !i1.fromPoint && !i2.toPoint) return false;
            if (a2 == b1 && !i1.toPoint && !i2.fromPoint) return false;
            if (b1 < a2 || b2 < a1) return false;
            return true;
        }

        public static BishSingleInterval operator +(BishSingleInterval i1, BishSingleInterval i2) {
            //if (!Intersect(i1, i2)) return BishUtils.Error();
            BishSingleInterval ans = new(Empty);
            if (i1.from < i2.from) {
                ans.from = i1.from;
                ans.fromPoint = i1.fromPoint;
            }
            else {
                ans.from = i2.from;
                ans.fromPoint = i2.fromPoint;
            }
            if (i1.to < i2.to) {
                ans.to = i2.to;
                ans.toPoint = i2.toPoint;
            }
            else {
                ans.to = i1.to;
                ans.toPoint = i1.toPoint;
            }
            return ans;
        }

        public static BishSingleInterval operator *(BishSingleInterval i1, BishSingleInterval i2) {
            if (!Intersect(i1, i2)) return new(Empty);
            BishSingleInterval ans = new(Empty);
            if (i2.from < i1.from) {
                ans.from = i1.from;
                ans.fromPoint = i1.fromPoint;
            }
            else {
                ans.from = i2.from;
                ans.fromPoint = i2.fromPoint;
            }
            if (i2.to < i1.to) {
                ans.to = i2.to;
                ans.toPoint = i2.toPoint;
            }
            else {
                ans.to = i1.to;
                ans.toPoint = i1.toPoint;
            }
            return ans;
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
            if (I1 is null && I2 is null) return true;
            if (I1 is null || I2 is null) return false;
            List<BishSingleInterval> i1 = [.. I1.intervals];
            List<BishSingleInterval> i2 = [.. I2.intervals];
            (BishSingleInterval, BishSingleInterval) r
                = (new(BishSingleInterval.Empty), new(BishSingleInterval.Empty));
            bool did;
            do {
                did = false;
                foreach (var l1 in i1) {
                    foreach (var l2 in i2) {
                        if (l1 == l2) {
                            r = (l1, l2);
                            did = true;
                            break;
                        }
                    }
                    if (did) break;
                }
                if (did) {
                    i1.Remove(r.Item1);
                    i2.Remove(r.Item2);
                }
            } while (did);
            return i1.Count == 0 && i2.Count == 0;
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

        public static BishInterval operator +(BishInterval i1, BishInterval i2) {
            BishInterval ans = new([.. i1.intervals, .. i2.intervals]);
            ans.Simplify();
            return ans;
        }

        public static BishInterval operator *(BishInterval i1, BishInterval i2) {
            BishInterval ans = new();
            foreach (var j1 in i1.intervals)
                foreach (var j2 in i2.intervals)
                    ans.intervals.Add(j1 * j2);
            ans.Simplify();
            return ans;
        }

        private void Simplify() {
            bool did;
            do {
                did = false;
                (BishSingleInterval, BishSingleInterval) remove
                    = (new(BishSingleInterval.Empty), new(BishSingleInterval.Empty));
                foreach (var i in intervals) {
                    foreach (var j in intervals.Except([i])) {
                        if (BishSingleInterval.Intersect(i, j)) {
                            remove = (i, j);
                            did = true;
                            break;
                        }
                    }
                    if (did) break;
                }
                var (x, y) = remove;
                intervals.Remove(x);
                intervals.Remove(y);
                intervals.Add(x + y);
            } while (did);
            intervals.Sort((x, y) => x.from.CompareTo(y.from));
            intervals.RemoveAll(i => i.IsEmpty());
        }
    }
}