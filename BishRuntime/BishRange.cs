namespace BishRuntime;

public class BishRange(int? start, int? end, int step) : BishObject
{
    public int? Start = start;
    public int? End = end;
    public int Step = step;
    public int? Current;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("range");

    [Builtin("hook")]
    public static BishRange Create(BishObject _) => new(0, 0, 0);

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
    public static void Init(BishRange self, BishObject a, [DefaultNull] BishObject? b, [DefaultNull] BishObject? step)
    {
        if (b is null) (self.Start, self.End) = (0, ToInt(a));
        else (self.Start, self.End) = (ToInt(a), ToInt(b));
        self.Step = ToInt(step) ?? 1;
        if (self.Step == 0) throw BishException.OfArgument_RangeZeroStep();
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

    public override string ToString()
    {
        if (Step != 1) return $"range({Start}, {End}, {Step})";
        return Start == 0 ? $"range({End})" : $"range({Start}, {End})";
    }
}