namespace BishTest.Runtime;

public class MethodTest : Test
{
    public readonly BishObject X = T.StaticType.CreateInstance([new BishString("x")]);

    private void Inject() =>
        X.Members.Add("f", new BishFunc([new BishArg("_", Default: BishNull.Instance)], _ => new BishInt(1)));

    [Fact]
    public void TestObjectInitialization()
    {
        X.Should().BeEquivalentTo(new T("x"));
        T.StaticType.Call([new BishString("y")]).Should().BeEquivalentTo(new T("y"));
    }

    [Fact]
    public void TestMethodTryCall()
    {
        Inject();
        new BishInt(0).TryCall([]).Should().BeNull();
        X.TryCall([]).Should().BeEquivalentTo(new BishInt(0));
        Action(() => X.TryCall([BishNull.Instance])).Should().Excepts(BishError.ArgumentErrorType);
        X.GetMember("f").TryCall([]).Should().BeEquivalentTo(new BishInt(1));
        X.GetMember("f").TryCall([BishNull.Instance]).Should().BeEquivalentTo(new BishInt(1));
        Action(() => X.GetMember("g").TryCall([])).Should().Excepts(BishError.ArgumentErrorType);
        Action(() => X.GetMember("g").TryCall([BishNull.Instance])).Should().Excepts(BishError.TypeErrorType);
        Action(() => T.StaticType.GetMember("g").TryCall([])).Should().Excepts(BishError.ArgumentErrorType);
        Action(() => T.StaticType.GetMember("g").TryCall([BishNull.Instance, X])).Should()
            .Excepts(BishError.TypeErrorType);
        X.GetMember("toString").TryCall([]).Should().BeEquivalentTo(new BishString("T(x)"));
        T.StaticType.GetMember("getName").TryCall([]).Should().BeEquivalentTo(new BishString("T"));
    }

    [Fact]
    public void TestMethodCall()
    {
        Inject();
        Action(() => new BishInt(0).Call([])).Should().Excepts(BishError.TypeErrorType);
        X.Call([]).Should().BeEquivalentTo(new BishInt(0));
        Action(() => X.Call([BishNull.Instance])).Should().Excepts(BishError.ArgumentErrorType);
        X.GetMember("f").Call([]).Should().BeEquivalentTo(new BishInt(1));
        X.GetMember("f").Call([BishNull.Instance]).Should().BeEquivalentTo(new BishInt(1));
        Action(() => X.GetMember("g").Call([])).Should().Excepts(BishError.ArgumentErrorType);
        Action(() => X.GetMember("g").Call([BishNull.Instance])).Should().Excepts(BishError.TypeErrorType);
        Action(() => T.StaticType.GetMember("g").Call([])).Should().Excepts(BishError.ArgumentErrorType);
        Action(() => T.StaticType.GetMember("g").Call([BishNull.Instance, X])).Should()
            .Excepts(BishError.TypeErrorType);
        X.GetMember("toString").Call([]).Should().BeEquivalentTo(new BishString("T(x)"));
        T.StaticType.GetMember("getName").Call([]).Should().BeEquivalentTo(new BishString("T"));
    }

    [Fact]
    public void TestSpecialBindMethod()
    {
        var x = new BishInt(1);
        // x.Type.GetMember("toString").Call([x]).Should().BeEquivalentTo(new BishString("1"));
        x.GetMember("toString").Call([]).Should().BeEquivalentTo(new BishString("1"));
        BishNull.Instance.GetMember("toString").Call([]).Should().BeEquivalentTo(new BishString("null"));
        x.Type.GetMember("toString").Call([]).Should().BeEquivalentTo(new BishString("[Type int]"));
    }
}

file class T(string tag) : BishObject
{
    // ReSharper disable once MemberCanBePrivate.Local
    public string Tag = tag;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("T");

    [Builtin(special: false)]
    public static BishInt F(T self) => new(0);

    [Builtin(special: false)]
    public static BishInt G(T self, T _) => new(0);

    [Builtin("hook")]
    public static T Create()
    {
        return new T("");
    }

    [Builtin("hook")]
    public static T Init(T self, BishString tag)
    {
        self.Tag = tag.Value;
        return self;
    }

    [Builtin("op")]
    public static BishInt Call(T self) => new(0);

    static T() => BishBuiltinBinder.Bind<T>();

    public override string ToString()
    {
        return $"T({Tag})";
    }
}