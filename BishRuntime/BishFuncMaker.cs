namespace BishRuntime;

public class BishCodedFunc(string name, IList<BishArg> inArgs, BishFrame inner, bool isGen, bool isAsync)
    : BishFunc(name, inArgs)
{
    public readonly BishFrame Inner = inner;
    public readonly bool IsGen = isGen;
    public readonly bool IsAsync = isAsync;

    public override BishType DefaultType => StaticType;
    public new static readonly BishType StaticType = new("Func", [BishFunc.StaticType]);

    public override BishObject CallRaw(IList<BishObject> args)
    {
        var frame = Inner.Clone();
        foreach (var arg in args.Reverse()) frame.Stack.Push(arg);
        return (IsGen, IsAsync) switch
        {
            (false, false) => frame.Execute(),
            (true, false) => new BishGenerator(frame),
            (false, true) => new BishAsyncTask(frame),
            (true, true) => new BishAsyncGenerator(frame)
        };
    }

    public override BishStackLayer GetStackLayer(IList<BishObject> args) =>
        base.GetStackLayer(args).WithSource(Inner.Source, Inner.Current?.Pos);

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

public class BishAsyncTask(BishFrame inner) : BishTask
{
    public override BishType DefaultType => StaticType;
    public new static readonly BishType StaticType = new("async.Task");
    public int Stage { get; private set; }

    [Async]
    public BishObject? Poll(BishObject ctx)
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

public class BishAsyncGenerator(BishFrame inner) : BishObject, IBishAsyncIterator
{
    public override BishType DefaultType => StaticType;
    public new static readonly BishType StaticType = new("async.gen", [BishIterator.AsyncType]);
    public int Stage { get; internal set; }

    [Iter]
    public BishObject Next() => new BishAsyncIteratorTask(this);

    public BishObject? NextPoll(BishObject ctx)
    {
        BishObject? yielded = null;
        BishObject? awaited = null;

        inner.YieldHandler = result => yielded = result;
        inner.AwaitHandler = result => awaited = result;

        try
        {
            inner.Execute();

            if (yielded is not null)
            {
                inner.Paused = false;
                Stage++;
                return yielded;
            }

            if (awaited is not null)
            {
                inner.Paused = false;
                Stage++;
                awaited.GetMember("poll").Call([ctx]);
                return null;
            }

            Stage = -1;
            return BishIteratorStop.Instance;
        }
        catch (BishException e)
        {
            Stage = -1;
            return new BishErrorResult(e.Error);
        }
    }

    [Builtin("hook")]
    public static BishInt Get_stage(BishAsyncGenerator self) => BishInt.Of(self.Stage);
}