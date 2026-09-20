using BishRuntime;
using BishRuntime.Numerals;

namespace BishLib;

public struct BishRandomModule : IModule
{
    public static BishObject Exports => IModule.ExportsFrom(
        ("Random", BishRandom.StaticType),
        ("random", BishRandom.Shared)
    );
}

public class BishRandom(Random random) : BishObject
{
    public readonly Random Random = random;

    public static readonly BishRandom Shared = new(Random.Shared);

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Random");

    [Builtin("hook")]
    public static BishRandom New([DefaultNull] BishInt? seed) =>
        new(seed is null ? new Random() : new Random(int.CreateTruncating(seed.Value)));

    private BigInt Rand(BigInt max)
    {
        if (max <= int.MaxValue) return Random.Next((int)max);
        var segments = max.AbsLgFloor() / BigInt.LgRadix;
        var rest = (int)((BigNum)max / BigInt.TenPow(segments * BigInt.LgRadix)).Ceil();
        while (true)
        {
            var result = new BigInt([
                ..Enumerable.Range(0, segments).Select(_ => Random.Next(BigInt.Radix)), Random.Next(rest)
            ]);
            if (result < max) return result;
        }
    }

    public BigInt Rand(BigInt min, BigInt max) => min < max
        ? min + Rand(max - min)
        : throw BishException.OfArgument($"{nameof(min)} must be less than {nameof(max)}");

    [Builtin]
    public static BishNum Rand(BishRandom self) =>
        new(new BigNum(self.Rand(BigInt.Zero, BigInt.TenPow(BigNum.MaxExp)), BigNum.MaxExp));

    [Builtin]
    public static BishInt RandInt(BishRandom self, BishInt min, BishInt max) =>
        BishInt.Of(self.Rand(min.Value, max.Value));

    public BishObject Choice(BishObject[] array) => array[Random.Next(array.Length)];

    [Builtin]
    public static BishObject Choice(BishRandom self, BishObject iter) => self.Choice(iter.ToEnumerable().ToArray());

    [Builtin]
    public static BishList Choices(BishRandom self, BishObject iter, BishInt count)
    {
        var array = iter.ToEnumerable().ToArray();
        return new BishList(Enumerable.Range(0, (int)count.Value).Select(_ => self.Choice(array)).ToList());
    }

    [Builtin]
    public static BishList Sample(BishRandom self, BishObject iter, BishInt count)
    {
        var shuffled = self.Shuffled(iter);
        return shuffled.Length >= count.Value
            ? new BishList(shuffled[..(int)count.Value])
            : throw BishException.OfArgument($"Cannot select {count.Value} samples from {shuffled.Length} items");
    }

    public BishObject[] Shuffled(BishObject iter)
    {
        var array = iter.ToEnumerable().ToArray();
        Random.Shuffle(array);
        return array;
    }

    [Builtin]
    public static BishList Shuffle(BishRandom self, BishObject iter) => new(self.Shuffled(iter));
}