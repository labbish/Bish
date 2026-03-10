using BishRuntime;

namespace BishLib;

public static class BishRandomModule
{
    public static BishObject Module => new BishObject
    {
        Members = new Dictionary<string, BishObject>
        {
            ["Random"] = BishRandom.StaticType,
            ["random"] = BishRandom.Shared
        }
    };
}

public class BishRandom(Random random) : BishObject
{
    public Random Random { get; private set; } = random;

    public static readonly BishRandom Shared = new(Random.Shared);

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Reader");

    [Builtin("hook")]
    public static BishRandom Create(BishObject _) => new(null!);

    [Builtin("hook")]
    public static void Init(BishRandom self, [DefaultNull] BishInt? seed) =>
        self.Random = seed is null ? new Random() : new Random(seed.Value);

    [Builtin(special: false)]
    public static BishNum Rand(BishRandom self) => new(self.Random.NextDouble());

    [Builtin(special: false)]
    public static BishNum RandInt(BishRandom self, BishInt min, BishInt max) => min.Value > max.Value
        ? throw BishException.OfArgument($"{nameof(min)} must be less than {nameof(max)}", [])
        : new BishNum(self.Random.Next(min.Value, max.Value));

    public BishObject Choice(BishObject iter)
    {
        var array = iter.ToEnumerable().ToArray();
        return array[Random.Next(array.Length)];
    }

    [Builtin(special: false)]
    public static BishObject Choice(BishRandom self, BishObject iter) => self.Choice(iter);

    [Builtin(special: false)]
    public static BishList Choices(BishRandom self, BishObject iter, BishInt count) =>
        new(Enumerable.Range(0, count.Value).Select(_ => self.Choice(iter)).ToList());

    [Builtin(special: false)]
    public static BishList Sample(BishRandom self, BishObject iter, BishInt count)
    {
        var shuffled = self.Shuffled(iter);
        return shuffled.Length >= count.Value
            ? new BishList(shuffled)
            : throw BishException.OfArgument($"Cannot select {count.Value} samples from {shuffled.Length} items", []);
    }

    public BishObject[] Shuffled(BishObject iter)
    {
        var array = iter.ToEnumerable().ToArray();
        Random.Shuffle(array);
        return array;
    }

    [Builtin(special: false)]
    public static BishList Shuffle(BishRandom self, BishObject iter) => new(self.Shuffled(iter));

    static BishRandom() => BishBuiltinBinder.Bind<BishRandom>();
}