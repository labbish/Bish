namespace BishRuntime;

public class BishRange(int? start, int? end, int step) : BishObject
{
    public readonly int? Start = start;
    public readonly int? End = end;
    public readonly int Step = step;
    public int? Current;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("range");

    private static int? ToInt(BishObject? obj) => obj switch
    {
        BishInt i => i.Value,
        BishNull or null => null,
        _ => throw BishException.OfType_Argument(obj, BishInt.StaticType)
    };

    private static BishObject ToObject(int? value) => value is null ? BishNull.Instance : BishInt.Of(value.Value);

    [Builtin("op")]
    public static BishBool Eq(BishRange self, BishRange other) =>
        BishBool.Of(self.Start == other.Start && self.End == other.End && self.Step == other.Step);

    [Builtin("hook")]
    public static BishRange New(BishObject a, [DefaultNull] BishObject? b, [DefaultNull] BishObject? step)
    {
        var (start, end) = b is null ? (0, ToInt(a)) : (ToInt(a), ToInt(b));
        var s = ToInt(step) ?? 1;
        return s == 0 ? throw BishException.OfArgument_RangeZeroStep() : new BishRange(start, end, s);
    }

    [Iter]
    public BishInt? Next()
    {
        if (Start is null || End is null) throw BishException.OfArgument_RangeNull();
        Current ??= Start;
        if (Current * Step >= End * Step) return null;
        var result = Current;
        Current += Step;
        return BishInt.Of(result.Value);
    }

    public BishRange Regularize(int length) =>
        new(Start?.Regularize(length) ?? 0, End?.Regularize(length, check: false) ?? length, Step);

    [Builtin]
    public static BishRange Regularize(BishRange self, BishInt length) => self.Regularize(length.Value);

    public IEnumerable<BishInt> ToInts() => this.ToEnumerable().Select(value => value.As<BishInt>(""));

    [Builtin("hook")]
    public static BishObject Get_start(BishRange self) => ToObject(self.Start);

    [Builtin("hook")]
    public static BishObject Get_end(BishRange self) => ToObject(self.End);

    [Builtin("hook")]
    public static BishInt Get_step(BishRange self) => BishInt.Of(self.Step);

    [Builtin]
    public static BishString Show(BishRange self) =>
        new(self.Step != 1 ? $"range({self.Start}, {self.End}, {self.Step})" :
            self.Start == 0 ? $"range({self.End})" : $"range({self.Start}, {self.End})");
}