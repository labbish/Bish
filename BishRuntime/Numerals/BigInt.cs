using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using BishUtils;

namespace BishRuntime.Numerals;

public readonly struct BigInt : INumber<BigInt>
{
    public readonly int[] Digits; // lowest to highest, each within [0, Radix)
    public readonly bool Negative; // always false for zero

    public BigInt(int[] digits, bool negative = false)
    {
        Digits = digits;
        Negative = digits.Any(d => d != 0) && negative;
    }

    public static implicit operator BigInt(int n)
    {
        var (q, r) = long.DivRem(Math.Abs((long)n), Radix);
        return new BigInt([(int)r, (int)q], n < 0);
    }

    public static explicit operator int(BigInt n) => int.CreateChecked(n);

    public int CompareTo(object? obj) => obj switch
    {
        null => 1,
        BigInt other => CompareTo(other),
        _ => throw new ArgumentException($"Cannot compare BigInt with {obj.GetType()}")
    };

    public int CompareTo(BigInt other)
    {
        if (Negative != other.Negative) return Negative ? -1 : 1;
        var magnitude = CompareMagnitude(this, other);
        return Negative ? -magnitude : magnitude;
    }

    private static int CompareMagnitude(BigInt left, BigInt right)
    {
        foreach (var (l, r) in left.Digits.ZipLongest(right.Digits).Reverse())
        {
            var c = l.CompareTo(r);
            if (c != 0) return c;
        }

        return 0;
    }

    public bool Equals(BigInt other) => this == other;

    public override bool Equals(object? obj) => obj is BigInt other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Digits.Length);
        foreach (var digit in Digits) hash.Add(digit);
        hash.Add(Negative);
        return hash.ToHashCode();
    }

    public override string ToString() => ToString(null, null);

    public string ToString(string? __, IFormatProvider? _) => this == 0
        ? "0"
        : (Negative ? "-" : "") +
          string.Join("", Digits.Select(d => d.ToString($"D{LgRadix}")).Reverse()).TrimStart('0');

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> __, IFormatProvider? _)
    {
        var result = ToString().AsSpan();
        charsWritten = Math.Min(result.Length, destination.Length);
        result[..charsWritten].CopyTo(destination);
        return charsWritten == result.Length;
    }

    public static BigInt Parse(string s, IFormatProvider? _ = null) => Parse(s.AsSpan());

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? _, out BigInt result) =>
        TryParse(s, out result);

    public static bool TryParse([NotNullWhen(true)] string? s, out BigInt result) => TryParse(s.AsSpan(), out result);

    public static BigInt Parse(ReadOnlySpan<char> s, IFormatProvider? _ = null) =>
        TryParse(s, out var result) ? result : throw BishException.OfArgument_Parse(s.ToString(), "int");

    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? _, out BigInt result) => TryParse(s, out result);

    public static bool TryParse(ReadOnlySpan<char> s, out BigInt result)
    {
        result = Zero;
        var negative = s.StartsWith('-');
        if (negative) s = s[1..];

        var len = (s.Length - 1) % LgRadix + 1;
        if (!int.TryParse(s[..len], out var first)) return false;
        List<int> digits = [first];
        for (var i = len; i < s.Length; i += LgRadix)
        {
            if (!int.TryParse(s[i..(i + Math.Min(LgRadix, s.Length - i))], out var digit)) return false;
            digits.Add(digit);
        }

        digits.Reverse();
        result = new BigInt(digits.ToArray(), negative);
        return true;
    }

    private static BigInt AddPos(BigInt left, BigInt right)
    {
        List<int> result = [];
        var c = false;
        foreach (var (l, r) in left.Digits.ZipLongest(right.Digits))
        {
            var s = l + r + (c ? 1 : 0);
            c = s >= Radix;
            if (c) s -= Radix;
            result.Add(s);
        }

        if (c) result.Add(1);
        return new BigInt(result.ToArray());
    }

    private static BigInt SubPos(BigInt left, BigInt right)
    {
        if (left < right) return -(right - left);
        if (left == right) return Zero;
        List<int> result = [];
        var c = false;
        foreach (var (l, r) in left.Digits.ZipLongest(right.Digits))
        {
            var s = l - r - (c ? 1 : 0);
            c = s < 0;
            if (c) s += Radix;
            result.Add(s);
        }

        return new BigInt(result.ToArray());
    }

    internal BigInt Lift(int n) => new([..Enumerable.Repeat(0, n), ..Digits], Negative);

    private static BigInt MulSinglePos(int left, int right)
    {
        var (q, r) = long.DivRem((long)left * right, Radix);
        return new BigInt([(int)r, (int)q]);
    }

    private static BigInt MulPos(BigInt left, BigInt right) => left.Digits.Enumerate().SelectMany(l =>
        right.Digits.Enumerate().Select(r => MulSinglePos(l.Item, r.Item).Lift(l.Index + r.Index))).Sum();

    private static (int Quotient, BigInt Remainder) DivModPosSmall(BigInt left, BigInt right)
    {
        var min = 0;
        var max = Radix - 1;
        var q = 0;
        while (min <= max)
        {
            var mid = (min + max) / 2;
            if (right * mid <= left)
            {
                q = mid;
                min = mid + 1;
            }
            else max = mid - 1;
        }

        return (q, left - right * q);
    }

    private static (BigInt Quotient, BigInt Remainder) DivModPos(BigInt left, BigInt right)
    {
        if (right == 0) throw BishException.OfZeroDivision();
        var q = Zero;
        var r = Zero;
        foreach (var (a, i) in left.Digits.Enumerate().Reverse())
        {
            var (q1, r1) = DivModPosSmall(r * Radix + a, right);
            q += ((BigInt)q1).Lift(i);
            r = r1;
        }

        return (q, r);
    }

    public static BigInt operator +(BigInt left, BigInt right) => (left.Negative, right.Negative) switch
    {
        (false, false) => AddPos(left, right),
        (true, false) => right - -left,
        (false, true) => left - -right,
        (true, true) => -(-left + -right)
    };

    public static BigInt AdditiveIdentity => Zero;

    public static bool operator ==(BigInt left, BigInt right) =>
        left.Negative == right.Negative && left.Digits.ZipLongest(right.Digits).All(pair => pair.First == pair.Second);

    public static bool operator !=(BigInt left, BigInt right) => !(left == right);

    public static bool operator >(BigInt left, BigInt right) => left.CompareTo(right) > 0;

    public static bool operator >=(BigInt left, BigInt right) => left.CompareTo(right) >= 0;

    public static bool operator <(BigInt left, BigInt right) => left.CompareTo(right) < 0;

    public static bool operator <=(BigInt left, BigInt right) => left.CompareTo(right) <= 0;

    public static BigInt operator --(BigInt value) => value - 1;

    public static BigInt operator /(BigInt left, BigInt right) => (left.Negative, right.Negative) switch
    {
        (false, false) => DivModPos(left, right).Quotient,
        (true, false) => -(-left / right),
        (false, true) => -(left / -right),
        (true, true) => -left / -right
    };

    public static BigInt operator ++(BigInt value) => value + 1;

    public static BigInt operator %(BigInt left, BigInt right) => (left.Negative, right.Negative) switch
    {
        (false, false) => DivModPos(left, right).Remainder,
        (true, false) => -(-left % right),
        (false, true) => -(left % -right),
        (true, true) => -(-left % -right)
    };

    public static BigInt MultiplicativeIdentity => One;

    public static BigInt operator *(BigInt left, BigInt right) => (left.Negative, right.Negative) switch
    {
        (false, false) => MulPos(left, right),
        (true, false) => -(-left * right),
        (false, true) => -(left * -right),
        (true, true) => -left * -right
    };

    public static BigInt operator -(BigInt left, BigInt right) => (left.Negative, right.Negative) switch
    {
        (false, false) => SubPos(left, right),
        (true, false) => -(-left + right),
        (false, true) => left + -right,
        (true, true) => -(-left - -right)
    };

    public static BigInt operator -(BigInt value) => new(value.Digits, !value.Negative);

    public static BigInt operator +(BigInt value) => value;

    public static BigInt Abs(BigInt value) => new(value.Digits);

    public static bool IsCanonical(BigInt value) => true;

    public static bool IsComplexNumber(BigInt value) => false;

    public static bool IsEvenInteger(BigInt value) => value.Digits[0] % 2 == 0;

    public static bool IsFinite(BigInt value) => true;

    public static bool IsImaginaryNumber(BigInt value) => false;

    public static bool IsInfinity(BigInt value) => false;

    public static bool IsInteger(BigInt value) => true;

    public static bool IsNaN(BigInt value) => false;

    public static bool IsNegative(BigInt value) => value.Negative;

    public static bool IsNegativeInfinity(BigInt value) => false;

    public static bool IsNormal(BigInt value) => true;

    public static bool IsOddInteger(BigInt value) => value.Digits[0] % 2 == 1;

    public static bool IsPositive(BigInt value) => !value.Negative;

    public static bool IsPositiveInfinity(BigInt value) => false;

    public static bool IsRealNumber(BigInt value) => true;

    public static bool IsSubnormal(BigInt value) => true;

    public static bool IsZero(BigInt value) => value == 0;

    public static BigInt MaxMagnitude(BigInt x, BigInt y) => CompareMagnitude(x, y) >= 0 ? x : y;

    public static BigInt MaxMagnitudeNumber(BigInt x, BigInt y) => MaxMagnitude(x, y);

    public static BigInt MinMagnitude(BigInt x, BigInt y) => CompareMagnitude(x, y) <= 0 ? x : y;

    public static BigInt MinMagnitudeNumber(BigInt x, BigInt y) => MinMagnitude(x, y);

    public static BigInt Parse(ReadOnlySpan<char> s, NumberStyles __, IFormatProvider? _) => Parse(s);

    public static BigInt Parse(string s, NumberStyles __, IFormatProvider? _) => Parse(s);

    public static bool TryConvertFromChecked<TOther>(TOther value, out BigInt result)
        where TOther : INumberBase<TOther>
    {
        if (typeof(TOther) != typeof(int))
        {
            result = Zero;
            return false;
        }

        result = (int)(object)value;
        return true;
    }

    public static bool TryConvertFromSaturating<TOther>(TOther value, out BigInt result)
        where TOther : INumberBase<TOther> => TryConvertFromChecked(value, out result);

    public static bool TryConvertFromTruncating<TOther>(TOther value, out BigInt result)
        where TOther : INumberBase<TOther> => TryConvertFromChecked(value, out result);

    private int ToIntUnchecked()
    {
        long result = 0;
        for (var i = Digits.Length - 1; i >= 0; i--) result = result * Radix + Digits[i];
        return (int)(Negative ? -result : result);
    }

    public static bool TryConvertToChecked<TOther>(BigInt value, [MaybeNullWhen(false)] out TOther result)
        where TOther : INumberBase<TOther>
    {
        if (typeof(TOther) != typeof(int))
        {
            result = default;
            return false;
        }

        if (value > int.MaxValue || value < int.MinValue) throw BishException.OfArgument_IntOverflow(value);
        result = (TOther)(object)value.ToIntUnchecked();
        return true;
    }

    public static bool TryConvertToSaturating<TOther>(BigInt value, [MaybeNullWhen(false)] out TOther result)
        where TOther : INumberBase<TOther>
    {
        if (typeof(TOther) != typeof(int))
        {
            result = default;
            return false;
        }

        if (value >= int.MaxValue) result = (TOther)(object)int.MaxValue;
        else if (value < int.MinValue) result = (TOther)(object)int.MinValue;
        else result = (TOther)(object)value.ToIntUnchecked();
        return true;
    }

    public static bool TryConvertToTruncating<TOther>(BigInt value, [MaybeNullWhen(false)] out TOther result)
        where TOther : INumberBase<TOther>
    {
        if (typeof(TOther) != typeof(int))
        {
            result = default;
            return false;
        }

        result = (TOther)(object)(value % int.MaxValue).ToIntUnchecked();
        return true;
    }

    public static bool TryParse(ReadOnlySpan<char> s, NumberStyles __, IFormatProvider? _, out BigInt result) =>
        TryParse(s, out result);

    public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles __, IFormatProvider? _,
        out BigInt result) => TryParse(s, out result);

    public static BigInt One => new([1]);
    public static int Radix => 1000000000;
    public const int LgRadix = 9;
    public static BigInt Zero => new([0]);

    public static BigInt TenPow(int n)
    {
        switch (n)
        {
            case 0: return One;
            case > LgRadix: return TenPow(n % LgRadix).Lift(n / LgRadix);
            default:
                var half = TenPow(n / 2);
                var s = half * half;
                return n % 2 == 1 ? s * 10 : s;
        }
    }

    public int AbsLgFloor() => ToString().TrimStart('-').Length - 1;
}

public static class BigIntConvert
{
    extension(Convert)
    {
        public static BigInt ToBigInt(string s, BigInt radix)
        {
            if (radix == 10) return BigInt.Parse(s);
            var negative = s.StartsWith('-');
            var result = BigInt.Zero;
            foreach (var c in negative ? s[1..] : s)
            {
                var digit = char.IsAsciiDigit(c) ? c - '0' : char.ToLower(c) - 'a' + 10;
                if (digit < 0 || digit >= radix) throw BishException.OfArgument_InvalidDigit(c, radix);
                result = result * radix + digit;
            }

            return negative ? -result : result;
        }

        public static string ToString(BigInt value, BigInt radix)
        {
            if (radix == 10) return value.ToString();
            if (value == 0) return "0";
            var current = BigInt.Abs(value);
            var result = "";
            while (current > 0)
            {
                var digit = (int)(current % radix);
                result = (char)(digit switch
                {
                    >= 0 and <= 9 => '0' + digit,
                    _ => 'a' + digit - 10
                }) + result;
                current /= radix;
            }

            return (value < 0 ? "-" : "") + result;
        }
    }
}