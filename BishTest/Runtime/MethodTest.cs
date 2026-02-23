namespace BishTest.Runtime;

public class MethodTest : Test
{
    public readonly BishObject X = T.StaticType.CreateInstance([S("x")]);

    private void Inject() =>
        X.Members.Add("f", new BishFunc([new BishArg("_", Default: Null)], _ => I(1)));

    [Fact]
    public void TestObjectInitialization()
    {
        X.Should().BeEquivalentTo(new T("x"));
        T.StaticType.Call([S("y")]).Should().BeEquivalentTo(new T("y"));
    }

    [Fact]
    public void TestMethodTryCall()
    {
        Inject();
        I(0).TryCall([]).Should().BeNull();
        X.TryCall([]).Should().BeEquivalentTo(I(0));
        Action(() => X.TryCall([Null])).Should().Excepts(BishError.ArgumentErrorType);
        X.GetMember("f").TryCall([]).Should().BeEquivalentTo(I(1));
        X.GetMember("f").TryCall([Null]).Should().BeEquivalentTo(I(1));
        Action(() => X.GetMember("g").TryCall([])).Should().Excepts(BishError.ArgumentErrorType);
        Action(() => X.GetMember("g").TryCall([Null])).Should().Excepts(BishError.TypeErrorType);
        Action(() => T.StaticType.GetMember("g").TryCall([])).Should().Excepts(BishError.ArgumentErrorType);
        Action(() => T.StaticType.GetMember("g").TryCall([Null, X])).Should().Excepts(BishError.TypeErrorType);
        X.GetMember("toString").TryCall([]).Should().BeEquivalentTo(S("T(x)"));
        T.StaticType.GetMember("name").Should().BeEquivalentTo(S("T"));
    }

    [Fact]
    public void TestMethodCall()
    {
        Inject();
        Action(() => I(0).Call([])).Should().Excepts(BishError.TypeErrorType);
        X.Call([]).Should().BeEquivalentTo(I(0));
        Action(() => X.Call([Null])).Should().Excepts(BishError.ArgumentErrorType);
        X.GetMember("f").Call([]).Should().BeEquivalentTo(I(1));
        X.GetMember("f").Call([Null]).Should().BeEquivalentTo(I(1));
        Action(() => X.GetMember("g").Call([])).Should().Excepts(BishError.ArgumentErrorType);
        Action(() => X.GetMember("g").Call([Null])).Should().Excepts(BishError.TypeErrorType);
        Action(() => T.StaticType.GetMember("g").Call([])).Should().Excepts(BishError.ArgumentErrorType);
        Action(() => T.StaticType.GetMember("g").Call([Null, X])).Should().Excepts(BishError.TypeErrorType);
        X.GetMember("toString").Call([]).Should().BeEquivalentTo(S("T(x)"));
        T.StaticType.GetMember("name").Should().BeEquivalentTo(S("T"));
    }

    [Fact]
    public void TestSpecialBindMethod()
    {
        var x = I(1);
        // x.Type.GetMember("toString").Call([x]).Should().BeEquivalentTo(S("1"));
        x.GetMember("toString").Call([]).Should().BeEquivalentTo(S("1"));
        Null.GetMember("toString").Call([]).Should().BeEquivalentTo(S("null"));
        x.Type.GetMember("toString").Call([]).Should().BeEquivalentTo(S("[Type int]"));
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
    public static T Create() => new("");

    [Builtin("hook")]
    public static T Init(T self, BishString tag)
    {
        self.Tag = tag.Value;
        return self;
    }

    [Builtin("op")]
    public static BishInt Call(T self) => new(0);

    static T() => BishBuiltinBinder.Bind<T>();

    public override string ToString() => $"T({Tag})";
}