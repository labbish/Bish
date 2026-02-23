namespace BishRuntime;

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
    public static void Init(BishRange self, BishInt a, [DefaultNull] BishInt? b, [DefaultNull] BishInt? step)
    {
        if (b is null) (self.Start, self.End) = (0, a.Value);
        else (self.Start, self.End) = (a.Value, b.Value);
        self.Step = step?.Value ?? 1;
        if (self.Step == 0) throw BishException.OfArgument("Range step cannot be 0", []);
    }

    [Iter]
    public BishInt? Next()
    {
        Current ??= Start;
        if (Current * Step >= End * Step) return null;
        var result = Current;
        Current += Step;
        return new BishInt(result.Value);
    }

    public BishRange Regularize(int length) =>
        new(Start.Regularize(length), End.Regularize(length, check: false), Step);

    [Builtin(special: false)]
    public static BishRange Regularize(BishRange self, BishInt length) => self.Regularize(length.Value);

    public IEnumerable<BishInt> ToInts() => this.ToEnumerable().Select(value => value.ExpectToBe<BishInt>(""));

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
}

internal static class IndexHelper
{
    extension(int index)
    {
        internal int Regularize(int length, bool check = true)
        {
            if ((index >= -length && index < length) || !check) return index + (index < 0 ? length : 0);
            throw BishException.OfArgument_IndexOutOfBound(length, index);
        }
    }
}

public static class IteratorHelper
{
    public static IEnumerable<BishObject> Enumerate(BishObject iterator)
    {
        while (true)
        {
            BishObject current;
            try
            {
                current = iterator.GetMember("next").Call([]);
            }
            catch
            {
                yield break;
            }

            yield return current;
        }
    }

    extension(BishObject obj)
    {
        internal BishObject Iterator() => BishOperator.Call("op_Iter", [obj]);
        internal IEnumerable<BishObject> ToEnumerable() => Enumerate(obj.Iterator());
    }
}