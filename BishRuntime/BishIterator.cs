namespace BishRuntime;

public static class BishIterator
{
    public static readonly BishType Type = new("Iterator");
    public static readonly BishType AsyncType = new("AsyncIterator");
}

public class BishIteratorStop : BishObject
{
    public override BishType DefaultType => StaticType;
    public new static readonly BishType StaticType = new("IteratorStop");
    
    public static readonly BishIteratorStop Instance = new();

    private BishIteratorStop()
    {
    }

    [Builtin]
    public static BishString Repr(BishIteratorStop _, BishReprContext __) => new("IteratorStop");
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
    extension(BishObject obj)
    {
        public IEnumerable<BishObject> ToEnumerable()
        {
            var iterator = BishOperator.Call("iter", new BishArgs([obj]));
            while (true)
            {
                var result = iterator.GetMember("next").Call(new BishArgs([]));
                if (result is BishIteratorStop) yield break;
                yield return result;
            }
        }
    }
}

public class BishNativeIterator(IEnumerable<BishObject> enumerable) : BishObject
{
    public readonly IEnumerator<BishObject> Enumerator = enumerable.GetEnumerator();

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Iterator.native");

    [Iter]
    public BishObject? Next() => Enumerator.MoveNext() ? Enumerator.Current : null;
}