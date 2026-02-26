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

    private static BishObject ToObject(int? value) => value is null ? BishNull.Instance : new BishInt(value.Value);

    [Builtin("hook")]
    public static void Init(BishRange self, BishObject a, [DefaultNull] BishObject? b, [DefaultNull] BishObject? step)
    {
        if (b is null) (self.Start, self.End) = (0, ToInt(a));
        else (self.Start, self.End) = (ToInt(a), ToInt(b));
        self.Step = ToInt(step) ?? 1;
        if (self.Step == 0) throw BishException.OfArgument("Range step cannot be 0", []);
    }

    [Iter]
    public BishInt? Next()
    {
        if (Start is null || End is null)
            throw BishException.OfArgument("Cannot iterate range with start=null or end=null", []);
        Current ??= Start;
        if (Current * Step >= End * Step) return null;
        var result = Current;
        Current += Step;
        return new BishInt(result.Value);
    }

    public BishRange Regularize(int length) =>
        new(Start?.Regularize(length) ?? 0, End?.Regularize(length, check: false) ?? length, Step);

    [Builtin(special: false)]
    public static BishRange Regularize(BishRange self, BishInt length) => self.Regularize(length.Value);

    public IEnumerable<BishInt> ToInts() => this.ToEnumerable().Select(value => value.ExpectToBe<BishInt>(""));

    [Builtin("hook")]
    public static BishObject Get_start(BishRange self) => ToObject(self.Start);

    [Builtin("hook")]
    public static BishObject Get_end(BishRange self) => ToObject(self.End);

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
            catch (BishException e)
            {
                if (e.Error.Type.CanAssignTo(BishError.IteratorStopType)) yield break;
                throw;
            }

            yield return current;
        }
    }

    extension(BishObject obj)
    {
        internal BishObject Iterator() => BishOperator.Call("iter", [obj]);
        internal IEnumerable<BishObject> ToEnumerable() => Enumerate(obj.Iterator());
    }
}