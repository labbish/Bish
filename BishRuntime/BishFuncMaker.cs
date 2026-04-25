namespace BishRuntime;

public class BishCodedFunc(string name, IList<BishArg> inArgs, Func<BishFrame> getInner, bool isGen)
    : BishFunc(name, inArgs, args =>
    {
        var inner = getInner();
        foreach (var arg in args.Reverse()) inner.Stack.Push(arg);
        return isGen ? new BishGenerator(inner) : inner.Execute();
    })
{
    public BishFrame Inner => getInner();
    public bool IsGen => isGen;

    public override BishType DefaultType => StaticType;
    public new static readonly BishType StaticType = new("Func", [BishFunc.StaticType]);

    [Builtin("hook")]
    public static BishFrame Get_frame(BishCodedFunc self) => self.Inner;

    [Builtin("hook")]
    public static BishBool Get_isGen(BishCodedFunc self) => BishBool.Of(self.IsGen);
}

public class BishGenerator(BishFrame inner) : BishObject
{
    public override BishType DefaultType => StaticType;
    public new static readonly BishType StaticType = new("gen");
    public int Stage { get; private set; }

    [Iter]
    public BishObject? Next()
    {
        try
        {
            inner.Execute();
        }
        catch (BishException e) when (e.Error.Type.CanAssignTo(BishError.YieldValueType))
        {
            Stage++;
            return e.Error.GetMember("value");
        }

        Stage = -1;
        return null;
    }

    [Builtin("hook")]
    public static BishInt Get_stage(BishGenerator self) => BishInt.Of(self.Stage);
}