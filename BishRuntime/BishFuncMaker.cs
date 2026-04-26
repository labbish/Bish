namespace BishRuntime;

public class BishCodedFunc(string name, IList<BishArg> inArgs, Func<BishFrame> getInner, bool isGen, bool isAsync)
    : BishFunc(name, inArgs, args =>
    {
        var inner = getInner();
        foreach (var arg in args.Reverse()) inner.Stack.Push(arg);
        return (isGen, isAsync) switch
        {
            (false, false) => inner.Execute(),
            (true, false) => new BishGenerator(inner),
            (false, true) => new BishAsyncFunc(inner),
            (true, true) => throw new NotImplementedException(),
        };
    })
{
    public BishFrame Inner => getInner();
    public bool IsGen => isGen;
    public bool IsAsync => isAsync;

    public override BishType DefaultType => StaticType;
    public new static readonly BishType StaticType = new("Func", [BishFunc.StaticType]);

    [Builtin("hook")]
    public static BishFrame Get_frame(BishCodedFunc self) => self.Inner;

    [Builtin("hook")]
    public static BishBool Get_isGen(BishCodedFunc self) => BishBool.Of(self.IsGen);

    [Builtin("hook")]
    public static BishBool Get_isAsync(BishCodedFunc self) => BishBool.Of(self.IsAsync);
}

public class BishGenerator(BishFrame inner) : BishObject
{
    public override BishType DefaultType => StaticType;
    public new static readonly BishType StaticType = new("gen");
    public int Stage { get; private set; }

    [Iter]
    public BishObject? Next()
    {
        BishObject? value = null;
        inner.YieldHandler = result => value = result;
        inner.Execute();
        if (value is not null)
        {
            inner.Paused = false;
            Stage++;
            return value;
        }

        Stage = -1;
        return null;
    }

    [Builtin("hook")]
    public static BishInt Get_stage(BishGenerator self) => BishInt.Of(self.Stage);
}

public class BishAsyncFunc(BishFrame inner) : BishTask
{
    public override BishType DefaultType => StaticType;
    public new static readonly BishType StaticType = new("gen");
    public int Stage { get; private set; }

    [Async]
    public BishObject? Poll(BishTaskContext ctx)
    {
        BishObject? value = null;
        inner.AwaitHandler = result => value = result;
        try
        {
            var execute = inner.Execute();
            if (value is not null)
            {
                inner.Paused = false;
                Stage++;
                value.GetMember("poll").Call([ctx]);
                return null;
            }

            Stage = -1;
            return execute;
        }
        catch (BishException e)
        {
            Stage = -1;
            return new BishErrorResult(e.Error);
        }
    }

    [Builtin("hook")]
    public static BishInt Get_stage(BishGenerator self) => BishInt.Of(self.Stage);
}