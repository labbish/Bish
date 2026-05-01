using BishRuntime;

namespace BishLib;

public static class BishRandomModule
{
    public static void Initialize() => BishLib.InitializeModule("random",
        ("Random", BishRandom.StaticType),
        ("random", BishRandom.Shared)
    );
}

public class BishRandom(Random random) : BishObject
{
    public Random Random { get; private set; } = random;

    public static readonly BishRandom Shared = new(Random.Shared);

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Random");

    [Builtin("hook")]
    public static BishRandom Create(BishObject _) => new(null!);

    [Builtin("hook")]
    public static void Init(BishRandom self, [DefaultNull] BishInt? seed) =>
        self.Random = seed is null ? new Random() : new Random(seed.Value);

    [Builtin]
    public static BishNum Rand(BishRandom self) => new(self.Random.NextDouble());

    [Builtin]
    public static BishInt RandInt(BishRandom self, BishInt min, BishInt max) => min.Value > max.Value
        ? throw BishException.OfArgument($"{nameof(min)} must be less than {nameof(max)}")
        : BishInt.Of(self.Random.Next(min.Value, max.Value));

    public BishObject Choice(BishObject[] array) => array[Random.Next(array.Length)];

    [Builtin]
    public static BishObject Choice(BishRandom self, BishObject iter) => self.Choice(iter.ToEnumerable().ToArray());

    [Builtin]
    public static BishList Choices(BishRandom self, BishObject iter, BishInt count)
    {
        var array = iter.ToEnumerable().ToArray();
        return new BishList(Enumerable.Range(0, count.Value).Select(_ => self.Choice(array)).ToList());
    }

    [Builtin]
    public static BishList Sample(BishRandom self, BishObject iter, BishInt count)
    {
        var shuffled = self.Shuffled(iter);
        return shuffled.Length >= count.Value
            ? new BishList(shuffled[..count.Value])
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