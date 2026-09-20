using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;

namespace BishRuntime.Numerals;

public readonly struct BigNum : INumber<BigNum>
{
    public readonly BigInt Data; // Never contains extra zero
    public readonly short Exp; // within [0, MaxExp]

    public const short MaxExp = 28;

    public BigNum(BigInt data, short exp)
    {
        while ((data % 10 == 0 && exp > 0) || exp > MaxExp)
        {
            data /= 10;
            exp--;
        }

        Data = data;
        Exp = exp;
    }

    public static implicit operator BigNum(BigInt n) => new(n, 0);

    public static implicit operator BigNum(int n) => (BigInt)n;

    private static (BigInt Left, BigInt Right, short exp) Unify(BigNum left, BigNum right)
    {
        var exp = Math.Max(left.Exp, right.Exp);
        return (left.Data * BigInt.TenPow(exp - left.Exp), right.Data * BigInt.TenPow(exp - right.Exp), exp);
    }

    public int CompareTo(object? obj) => obj switch
    {
        null => 1,
        BigNum other => CompareTo(other),
        _ => throw new ArgumentException($"Cannot compare BigNum with {obj.GetType()}")
    };

    public int CompareTo(BigNum other)
    {
        var (l, r, _) = Unify(this, other);
        return l.CompareTo(r);
    }

    public bool Equals(BigNum other) => this == other;

    public override bool Equals(object? obj) => obj is BigNum other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Data, Exp);

    public override string ToString() => ToString(null);

    public string ToString(string? format, IFormatProvider? _ = null)
    {
        if (format?.ToLower().StartsWith('e') == true)
        {
            var exp = AbsLgFloor();
            return (this / BigInt.TenPow(exp)).ToString($"F{format[1..]}") + format[0] + exp;
        }

        var precision = format?.StartsWith('F') == true ? (int)BigInt.Parse(format[1..]) : -1;
        if (Exp == 0) return Data.ToString();
        var s = BigInt.Abs(Data).ToString();
        var pre = Data < 0 ? "-" : "";
        if (s.Length <= Exp) return pre + "0" + TakeFront(s.PadLeft(Exp, '0'), precision);
        return pre + s[..^Exp] + TakeFront(s[^Exp..], precision);
        static string TakeFront(string s, int n) => n == 0 ? "" : '.' + (n == -1 || n >= s.Length ? s : s[..n]);
    }

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> __, IFormatProvider? _)
    {
        var result = ToString().AsSpan();
        charsWritten = Math.Min(result.Length, destination.Length);
        result[..charsWritten].CopyTo(destination);
        return charsWritten == result.Length;
    }

    public static BigNum Parse(string s, IFormatProvider? _ = null) => Parse(s.AsSpan());

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? _, out BigNum result) =>
        TryParse(s, out result);

    public static bool TryParse([NotNullWhen(true)] string? s, out BigNum result) => TryParse(s.AsSpan(), out result);

    public static BigNum Parse(ReadOnlySpan<char> s, IFormatProvider? _ = null) =>
        TryParse(s, out var result) ? result : throw BishException.OfArgument_Parse(s.ToString(), "num");

    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? _, out BigNum result) => TryParse(s, out result);

    public static bool TryParse(ReadOnlySpan<char> s, out BigNum result)
    {
        result = Zero;
        var dot = s.IndexOf('.');
        if (dot == -1)
        {
            var b = BigInt.TryParse(s, out var r);
            result = r;
            return b;
        }

        if (!BigInt.TryParse([..s[..dot], ..s[(dot + 1)..]], out var n)) return false;
        result = new BigNum(n, (short)(s.Length - dot - 1));
        return true;
    }

    public static BigNum operator +(BigNum left, BigNum right)
    {
        var (l, r, e) = Unify(left, right);
        return new BigNum(l + r, e);
    }

    public static BigNum AdditiveIdentity => Zero;

    public static bool operator ==(BigNum left, BigNum right) => left.Exp == right.Exp && left.Data == right.Data;

    public static bool operator !=(BigNum left, BigNum right) => !(left == right);

    public static bool operator >(BigNum left, BigNum right) => left.CompareTo(right) > 0;

    public static bool operator >=(BigNum left, BigNum right) => left.CompareTo(right) >= 0;

    public static bool operator <(BigNum left, BigNum right) => left.CompareTo(right) < 0;

    public static bool operator <=(BigNum left, BigNum right) => left.CompareTo(right) <= 0;

    public static BigNum operator --(BigNum value) => value - 1;

    public static BigNum operator /(BigNum left, BigNum right)
    {
        var d = (short)(right.Exp - left.Exp);
        var e = Math.Max(MaxExp, d);
        return new BigNum(left.Data * BigInt.TenPow(e + d) / right.Data, e);
    }

    public static BigNum operator ++(BigNum value) => value + 1;

    public static BigNum operator %(BigNum left, BigNum right) => left - right * (left / right).Truncate();

    public static BigNum MultiplicativeIdentity => One;

    public static BigNum operator *(BigNum left, BigNum right) =>
        new(left.Data * right.Data, (short)(left.Exp + right.Exp));

    public static BigNum operator -(BigNum left, BigNum right)
    {
        var (l, r, e) = Unify(left, right);
        return new BigNum(l - r, e);
    }

    public static BigNum operator -(BigNum value) => new(-value.Data, value.Exp);

    public static BigNum operator +(BigNum value) => value;

    public static BigNum Abs(BigNum value) => new(BigInt.Abs(value.Data), value.Exp);

    public static bool IsCanonical(BigNum value) => true;

    public static bool IsComplexNumber(BigNum value) => false;

    public static bool IsEvenInteger(BigNum value) => IsInteger(value) && BigInt.IsEvenInteger(value.Data);

    public static bool IsFinite(BigNum value) => true;

    public static bool IsImaginaryNumber(BigNum value) => false;

    public static bool IsInfinity(BigNum value) => false;

    public static bool IsInteger(BigNum value) => value.Exp == 0;

    public static bool IsNaN(BigNum value) => false;

    public static bool IsNegative(BigNum value) => BigInt.IsNegative(value.Data);

    public static bool IsNegativeInfinity(BigNum value) => false;

    public static bool IsNormal(BigNum value) => true;

    public static bool IsOddInteger(BigNum value) => IsInteger(value) && BigInt.IsOddInteger(value.Data);

    public static bool IsPositive(BigNum value) => BigInt.IsPositive(value.Data);

    public static bool IsPositiveInfinity(BigNum value) => false;

    public static bool IsRealNumber(BigNum value) => true;

    public static bool IsSubnormal(BigNum value) => true;

    public static bool IsZero(BigNum value) => BigInt.IsZero(value.Data);

    public static BigNum MaxMagnitude(BigNum x, BigNum y) => Abs(x) >= Abs(y) ? x : y;

    public static BigNum MaxMagnitudeNumber(BigNum x, BigNum y) => MaxMagnitude(x, y);

    public static BigNum MinMagnitude(BigNum x, BigNum y) => Abs(x) >= Abs(y) ? y : x;

    public static BigNum MinMagnitudeNumber(BigNum x, BigNum y) => MinMagnitude(x, y);

    public static BigNum Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? _) => Parse(s);

    public static BigNum Parse(string s, NumberStyles style, IFormatProvider? _) => Parse(s);

    public static bool TryConvertFromChecked<TOther>(TOther value, out BigNum result)
        where TOther : INumberBase<TOther>
    {
        result = Zero;
        return false;
    }

    public static bool TryConvertFromSaturating<TOther>(TOther value, out BigNum result)
        where TOther : INumberBase<TOther>
    {
        result = Zero;
        return false;
    }

    public static bool TryConvertFromTruncating<TOther>(TOther value, out BigNum result)
        where TOther : INumberBase<TOther>
    {
        result = Zero;
        return false;
    }

    public static bool TryConvertToChecked<TOther>(BigNum value, [MaybeNullWhen(false)] out TOther result)
        where TOther : INumberBase<TOther>
    {
        result = default;
        return false;
    }

    public static bool TryConvertToSaturating<TOther>(BigNum value, [MaybeNullWhen(false)] out TOther result)
        where TOther : INumberBase<TOther>
    {
        result = default;
        return false;
    }

    public static bool TryConvertToTruncating<TOther>(BigNum value, [MaybeNullWhen(false)] out TOther result)
        where TOther : INumberBase<TOther>
    {
        result = default;
        return false;
    }

    public static bool TryParse(ReadOnlySpan<char> s, NumberStyles __, IFormatProvider? _, out BigNum result) =>
        TryParse(s, out result);

    public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles __, IFormatProvider? _,
        out BigNum result) => TryParse(s, out result);

    public static BigNum One => new(BigInt.One, 0);
    public static int Radix => BigInt.Radix;
    public static BigNum Zero => new(BigInt.Zero, 0);

    public static readonly BigNum Pi = new(BigInt.Parse("31415926535897932384626433832"), MaxExp);
    public static readonly BigNum E = new(BigInt.Parse("27182818284590452353602874713"), MaxExp);

    public BigInt Truncate() => Data / BigInt.TenPow(Exp);

    public BigInt Floor()
    {
        var div = BigInt.TenPow(Exp);
        var quotient = Data / div;
        return BigInt.IsNegative(Data) && Data % div != BigInt.Zero ? quotient - BigInt.One : quotient;
    }

    public BigInt Ceil() => Exp == 0 ? Data : Floor() + 1;

    public BigInt Round()
    {
        var up = BigInt.Abs(Data) % 10 > 5;
        return BigInt.IsNegative(Data) ? (up ? Floor() : Ceil()) : up ? Ceil() : Floor();
    }

    public BigNum Pow(BigInt n)
    {
        if (n < 0) return One / Pow(-n);
        if (n == 0) return One;
        var half = Pow(n / 2);
        var result = half * half;
        return n % 2 == 1 ? this * result : result;
    }

    private static BigInt Gcd(BigInt left, BigInt right)
    {
        while (true)
        {
            if (left < 0) left = -left;
            else if (right < 0) right = -right;
            else if (left == 0) return right;
            else if (right == 0) return left;
            else if (left < right) right %= left;
            else left %= right;
        }
    }

    public BigNum Pow(BigNum other)
    {
        var a = other.Data;
        var b = BigInt.TenPow(other.Exp);
        var d = Gcd(a, b);
        return Pow(a / d).Root(b / d);
    }

    public BigNum Root(BigInt n)
    {
        if (n == 1) return this;
        if (this < 0)
        {
            if (n % 2 == 0) throw BishException.OfArgument_InvalidPow();
            return -(-this).Root(n);
        }

        var result = One; // TODO: use a better one
        for (var i = 0; i < 10; i++)
        {
            var r = ((n - 1) * result + this / result.Pow(n - 1)) / n;
            if (r == result) return result;
            result = r;
        }

        return result;
    }

    public int AbsLgFloor() => Data.AbsLgFloor() - Exp;
}