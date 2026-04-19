namespace BishTest.Core;

public class MethodTest : Test
{
    public MethodTest(TestInfoFixture fixture) : base(fixture)
    {
        Scope.DefVar("T1", T1.StaticType);
        Execute("x:=T1('x');x.f:=(_:null)1;");
    }

    [Fact]
    public void TestObjectInitialization() => ExpectResult("T1('y')", new T1("y"));

    [Fact]
    public void TestMethodCall()
    {
        Action(() => Execute("0();")).Should().Excepts(BishError.TypeErrorType);
        ExpectResult("x()", I(0));
        Action(() => Execute("x(null);")).Should().Excepts(BishError.ArgumentErrorType);
        ExpectResult("x.f()", I(1));
        ExpectResult("x.f(null)", I(1));
        Action(() => Execute("x.g();")).Should().Excepts(BishError.ArgumentErrorType);
        Action(() => Execute("x.g(null);")).Should().Excepts(BishError.TypeErrorType);
        Action(() => Execute("T1.g();")).Should().Excepts(BishError.ArgumentErrorType);
        Action(() => Execute("T1.g(null,x);")).Should().Excepts(BishError.TypeErrorType);
        ExpectResult("x.toString()", S("T(x)"));
        ExpectResult("T1.name", S("T"));
    }

    [Fact]
    public void TestSpecialBindMethod()
    {
        Execute("x:=1;");
        ExpectResult("x.toString()", S("1"));
        ExpectResult("null.toString()", S("null"));
        ExpectResult("x.type.toString()", S("int"));
    }

    [Fact]
    public void TestBase()
    {
        Execute("class C1{func f(self)self.a;};");
        Execute("class C2:C1{func f(self)self.b;};");
        Execute("x:=C2();x.a:=1;x.b:=2;");
        ExpectResult("x.f()", I(2));
        ExpectResult("x.base().f()", I(1));
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