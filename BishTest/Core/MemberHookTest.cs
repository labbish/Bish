namespace BishTest.Core;

public class MemberHookTest : Test
{
    public MemberHookTest(TestInfoFixture fixture) : base(fixture)
    {
        Execute("class T1{get(self,name)'get';set(self,name,value)'set';"
                + "def(self,name,value)'def';del(self,name)'del'};x:=T1();");
        Execute("class T2{init(self)self.A:=0;get a(self)self.A;set a(self,a){self.A=a;null};"
                + "def a(self,a){self.A=a;null};del a(self)null};y:=T2();");
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