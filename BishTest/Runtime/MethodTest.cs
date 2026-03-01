namespace BishTest.Runtime;

public class MethodTest : Test
{
    public readonly BishObject X = T1.StaticType.CreateInstance([S("x")]);

    private void Inject() =>
        X.Members.Add("f", new BishFunc("f", [new BishArg("_", Default: Null)], _ => I(1)));

    [Fact]
    public void TestObjectInitialization()
    {
        X.Should().BeEquivalentTo(new T1("x"));
        T1.StaticType.Call([S("y")]).Should().BeEquivalentTo(new T1("y"));
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
        Action(() => T1.StaticType.GetMember("g").TryCall([])).Should().Excepts(BishError.ArgumentErrorType);
        Action(() => T1.StaticType.GetMember("g").TryCall([Null, X])).Should().Excepts(BishError.TypeErrorType);
        X.GetMember("toString").TryCall([]).Should().BeEquivalentTo(S("T(x)"));
        T1.StaticType.GetMember("name").Should().BeEquivalentTo(S("T"));
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
        Action(() => T1.StaticType.GetMember("g").Call([])).Should().Excepts(BishError.ArgumentErrorType);
        Action(() => T1.StaticType.GetMember("g").Call([Null, X])).Should().Excepts(BishError.TypeErrorType);
        X.GetMember("toString").Call([]).Should().BeEquivalentTo(S("T(x)"));
        T1.StaticType.GetMember("name").Should().BeEquivalentTo(S("T"));
    }

    [Fact]
    public void TestSpecialBindMethod()
    {
        var x = I(1);
        // x.Type.GetMember("toString").Call([x]).Should().BeEquivalentTo(S("1"));
        x.GetMember("toString").Call([]).Should().BeEquivalentTo(S("1"));
        Null.GetMember("toString").Call([]).Should().BeEquivalentTo(S("null"));
        x.Type.GetMember("toString").Call([]).Should().BeEquivalentTo(S("int"));
    }

    [Fact]
    public void TestBase()
    {
        var c1 = new BishType("C1");
        c1.SetMember("f", new BishFunc("f", [new BishArg("self")], args => args[0].GetMember("a")));
        var c2 = new BishType("C2", [c1]);
        c2.SetMember("f", new BishFunc("f", [new BishArg("self")], args => args[0].GetMember("b")));
        var x = c2.CreateInstance([]);
        x.SetMember("a", I(1));
        x.SetMember("b", I(2));
        x.GetMember("f").Call([]).Should().BeEquivalentTo(I(2));
        x.GetMember("base").Call([]).GetMember("f").Call([]).Should().BeEquivalentTo(I(1));
    }
}

file class T1(string tag) : BishObject
{
    // ReSharper disable once MemberCanBePrivate.Local
    public string Tag = tag;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("T");

    [Builtin(special: false)]
    public static BishInt F(T1 self) => BishInt.Of(0);

    [Builtin(special: false)]
    public static BishInt G(T1 self, T1 _) => BishInt.Of(0);

    [Builtin("hook")]
    public static T1 Create(BishObject _) => new("");

    [Builtin("hook")]
    public static void Init(T1 self, BishString tag) => self.Tag = tag.Value;

    [Builtin("op")]
    public static BishInt Call(T1 self) => BishInt.Of(0);

    static T1() => BishBuiltinBinder.Bind<T1>();

    public override string ToString() => $"T({Tag})";
}