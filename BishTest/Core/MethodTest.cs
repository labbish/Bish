namespace BishTest.Core;

public class MethodTest : Test
{
    public MethodTest(TestInfoFixture fixture) : base(fixture)
    {
        Execute("class T1{func f(self)0;func g(self,_)0;new(tag){.tag};"
                + "oper()(self)0;func toString(self)'T({})'.format(self.tag)};");
        Execute("x:=T1('x');x.f:=(_:null)1;");
    }

    [Fact]
    public void TestObjectInitialization() => ExpectResult("T1('y').tag", "'y'");

    [Fact]
    public void TestMethodCall()
    {
        ExpectError("0();", BishError.TypeErrorType);
        ExpectResult("x()", "0");
        ExpectError("x(null);", BishError.ArgumentErrorType);
        ExpectResult("x.f()", "1");
        ExpectResult("x.f(null)", "1");
        ExpectError("x.g();", BishError.ArgumentErrorType);
        ExpectResult("x.g(null)", "0");
        ExpectError("T1.g();", BishError.ArgumentErrorType);
        ExpectResult("T1.g(null,x)", "0");
        ExpectResult("x.toString()", "'T(x)'");
        ExpectResult("T1.name", "'T1'");
    }

    [Fact]
    public void TestSpecialBindMethod()
    {
        Execute("x:=1;");
        ExpectResult("x.toString()", "'1'");
        ExpectResult("null.toString()", "'null'");
        ExpectResult("x.type.toString(x)", "'1'");
        ExpectResult("string.from(x.type)", "'int'");
        ExpectResult("string.from(x)", "'1'");
        
        Execute("class C{func toString(self)'X'};class D:C;");
        ExpectResult("C().toString()", "'X'");
        ExpectResult("D().toString()", "'X'");
        ExpectResult("'{}'.format(C())", "'X'");
        ExpectResult("'{}'.format(D())", "'X'");
    }

    [Fact]
    public void TestBase()
    {
        Execute("class C1{func f(self)self.a;};");
        Execute("class C2:C1{func f(self)self.b;};");
        Execute("x:=C2();x.a:=1;x.b:=2;");
        ExpectResult("x.f()", "2");
        ExpectResult("x.base().f()", "1");
    }
}