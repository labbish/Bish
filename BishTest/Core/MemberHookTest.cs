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
        ExpectResult("x.a", "0");
        ExpectResult("x.b", "'get'");
    }

    [Fact]
    public void TestObjectSetHook()
    {
        ExpectResult("x.a=null", "null");
        ExpectResult("x.b=null", "'set'");
        ExpectResult("x.vars.length", "1");
        ExpectResult("x.a", "null");
    }

    [Fact]
    public void TestObjectDefHook()
    {
        ExpectResult("x.a:=null", "'def'");
        ExpectResult("x.b:=null", "'def'");
        ExpectResult("x.vars.length", "1");
        ExpectResult("x.a", "0");
    }


    [Fact]
    public void TestObjectDelHook()
    {
        ExpectResult("del x.a", "0");
        ExpectResult("del x.b", "'del'");
        ExpectResult("x.vars.length", "0");
    }

    [Fact]
    public void TestMemberGetter()
    {
        ExpectResult("y.a", "0");
    }

    [Fact]
    public void TestMemberSetter()
    {
        ExpectResult("y.a=2", "null");
        ExpectResult("y.a", "2");
    }

    [Fact]
    public void TestMemberDeffer()
    {
        ExpectResult("y.a:=2", "null");
        ExpectResult("y.a", "2");
    }

    [Fact]
    public void TestMemberDeller()
    {
        ExpectResult("del y.a", "null");
        ExpectResult("y.a", "0");
    }

    [Fact]
    public void TestNullAccess()
    {
        ExpectError("null.x;", BishError.NullErrorType);
        ExpectError("null.x=0;", BishError.NullErrorType);
        ExpectError("null.x:=0;", BishError.NullErrorType);
        ExpectError("del null.x;", BishError.NullErrorType);
    }
}