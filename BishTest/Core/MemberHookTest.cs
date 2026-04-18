namespace BishTest.Core;

public class MemberHookTest : Test
{
    public MemberHookTest(TestInfoFixture fixture) : base(fixture)
    {
        Scope.Vars.Add("x", T1.StaticType.CreateInstance([]));
        Scope.Vars.Add("y", T2.StaticType.CreateInstance([]));
        Execute("x.vars['a']:=0;");
    }

    [Fact]
    public void TestObjectGetHook()
    {
        ExpectResult("x.a", I(0));
        ExpectResult("x.b", S("get"));
    }

    [Fact]
    public void TestObjectSetHook()
    {
        ExpectResult("x.a=null", Null);
        ExpectResult("x.b=null", S("set"));
        ExpectResult("x.vars.length", I(1));
        ExpectResult("x.a", Null);
    }

    [Fact]
    public void TestObjectDefHook()
    {
        ExpectResult("x.a:=null", S("def"));
        ExpectResult("x.b:=null", S("def"));
        ExpectResult("x.vars.length", I(1));
        ExpectResult("x.a", I(0));
    }


    [Fact]
    public void TestObjectDelHook()
    {
        ExpectResult("del x.a", I(0));
        ExpectResult("del x.b", S("del"));
        ExpectResult("x.vars.length", I(0));
    }

    [Fact]
    public void TestMemberGetter()
    {
        ExpectResult("y.a", I(0));
    }

    [Fact]
    public void TestMemberSetter()
    {
        ExpectResult("y.a=2", Null);
        ExpectResult("y.a", I(2));
    }

    [Fact]
    public void TestMemberDeffer()
    {
        ExpectResult("y.a:=2", Null);
        ExpectResult("y.a", I(2));
    }

    [Fact]
    public void TestMemberDeller()
    {
        ExpectResult("del y.a", Null);
        ExpectResult("y.a", I(0));
    }

    [Fact]
    public void TestNullAccess()
    {
        Action(() => Execute("null.x;")).Should().Excepts(BishError.NullErrorType);
        Action(() => Execute("null.x=0;")).Should().Excepts(BishError.NullErrorType);
        Action(() => Execute("null.x:=0;")).Should().Excepts(BishError.NullErrorType);
        Action(() => Execute("del null.x;")).Should().Excepts(BishError.NullErrorType);
    }
}

file class T1 : BishObject
{
    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("T");

    [Builtin("hook")]
    public static T1 Create(BishObject _) => new();

    [Builtin("hook")]
    public static BishString Get(T1 self, BishString name) => new("get");

    [Builtin("hook")]
    public static BishString Set(T1 self, BishString name, BishObject value) => new("set");

    [Builtin("hook")]
    public static BishString Def(T1 self, BishString name, BishObject value) => new("def");

    [Builtin("hook")]
    public static BishString Del(T1 self, BishString name) => new("del");

    static T1() => BishBuiltinBinder.Bind<T1>();
}

file class T2 : BishObject
{
    public int A;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("U");

    [Builtin("hook")]
    public static T2 Create(BishObject _) => new();

    [Builtin("hook")]
    public static BishInt Get_a(T2 self) => BishInt.Of(self.A);

    [Builtin("hook")]
    public static void Set_a(T2 self, BishInt value) => self.A = value.Value;

    [Builtin("hook")]
    public static void Def_a(T2 self, BishInt value) => self.A = value.Value;

    [Builtin("hook")]
    public static BishNull Del_a(T2 self) => BishNull.Instance;

    static T2()
    {
        BishBuiltinBinder.Bind<T2>();
    }
}