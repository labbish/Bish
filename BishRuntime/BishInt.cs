namespace BishRuntime;

public class BishInt(int value) : BishObject
{
    public int Value { get; private set; } = value;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("int");

    [Builtin("hook")]
    public static BishInt Create() => new(0);

    [Builtin("hook")]
    public static void Init(BishInt self, [DefaultNull] BishInt? other) => self.Value = other?.Value ?? 0;

    [Builtin("op")]
    public static BishInt Pos(BishInt a) => new(+a.Value);

    [Builtin("op")]
    public static BishInt Neg(BishInt a) => new(-a.Value);

    [Builtin("op")]
    public static BishInt Add(BishInt a, BishInt b) => new(a.Value + b.Value);

    [Builtin("op")]
    public static BishInt Sub(BishInt a, BishInt b) => new(a.Value - b.Value);

    [Builtin("op")]
    public static BishInt Mul(BishInt a, BishInt b) => new(a.Value * b.Value);

    [Builtin("op")]
    public static BishNum Div(BishInt a, BishInt b) => new((double)a.Value / b.Value);

    [Builtin("op")]
    public static BishInt Mod(BishInt a, BishInt b) => b.Value != 0 ? new BishInt(a.Value % b.Value) : throw BishException.OfZeroDivision();

    [Builtin("op")]
    public static BishNum Pow(BishInt a, BishInt b) => new(Math.Pow(a.Value, b.Value));

    [Builtin(special: false)]
    public static BishInt Abs(BishInt a) => new(Math.Abs(a.Value));

    [Builtin(special: false)]
    public static BishInt Sign(BishInt a) => new(Math.Sign(a.Value));

    public override string ToString() => Value.ToString();

    [Builtin("op")]
    public static BishBool Eq(BishInt a, BishInt b) => new(a.Value == b.Value);

    [Builtin("op")]
    public static BishInt Cmp(BishInt a, BishInt b) => new(a.Value.CompareTo(b.Value));

    [Builtin("op")]
    public static BishBool Bool(BishInt a) => new(a.Value != 0);

    static BishInt() => BishBuiltinBinder.Bind<BishInt>();
}

public class BishRange(int start, int end, int step) : BishObject
{
    public int Start = start;
    public int End = end;
    public int Step = step;
    public int? Current;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("range");

    [Builtin("hook")]
    public static BishRange Create() => new(0, 0, 0);

    [Builtin("hook")]
    public static void Init(BishRange self, BishInt start, BishInt end, [DefaultNull] BishInt? step)
    {
        self.Start = start.Value;
        self.End = end.Value;
        self.Step = step?.Value ?? 1;
    }

    [Iter]
    public BishInt? Next()
    {
        Current ??= Start;
        if (Current >= End) return null;
        var result = Current;
        Current += Step;
        return new BishInt(result.Value);
    }

    [Builtin("hook")]
    public static BishInt Get_start(BishRange self) => new(self.Start);

    [Builtin("hook")]
    public static BishInt Get_end(BishRange self) => new(self.End);

    [Builtin("hook")]
    public static BishInt Get_step(BishRange self) => new(self.Step);

    static BishRange()
    {
        BishBuiltinIteratorBinder.Bind<BishRange>();
        BishBuiltinBinder.Bind<BishRange>();
    }
} // TODO: indexing string / list with range