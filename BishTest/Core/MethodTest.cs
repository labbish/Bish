namespace BishTest.Core;

public class MethodTest : Test
{
    public MethodTest(TestInfoFixture fixture) : base(fixture)
    {
        Execute("class T1{func f(self)0;func g(self,_)0;init(self,tag)self.tag:=tag;"
                + "oper()(self)0;func toString(self)'T({})'.format(self.tag)};");
        Execute("x:=T1('x');x.f:=(_:null)1;");
    }

    [Fact]
    public void TestObjectInitialization() => ExpectResult("T1('y').tag", S("y"));

    [Fact]
    public void TestMethodCall()
    {
        Action(() => Execute("0();")).Should().Excepts(BishError.TypeErrorType);
        ExpectResult("x()", I(0));
        Action(() => Execute("x(null);")).Should().Excepts(BishError.ArgumentErrorType);
        ExpectResult("x.f()", I(1));
        ExpectResult("x.f(null)", I(1));
        Action(() => Execute("x.g();")).Should().Excepts(BishError.ArgumentErrorType);
        ExpectResult("x.g(null)", I(0));
        Action(() => Execute("T1.g();")).Should().Excepts(BishError.ArgumentErrorType);
        ExpectResult("T1.g(null,x)", I(0));
        ExpectResult("x.toString()", S("T(x)"));
        ExpectResult("T1.name", S("T1"));
    }

    [Fact]
    public void TestSpecialBindMethod()
    {
        Execute("x:=1;");
        ExpectResult("x.toString()", S("1"));
        ExpectResult("null.toString()", S("null"));
        ExpectResult("x.type.toString()", S("int"));
        
        Execute("class C{func toString(self)'X'};class D:C;");
        ExpectResult("C().toString()", S("X"));
        ExpectResult("D().toString()", S("X"));
        // Uses BishOperator.Call("toString", [c])
        ExpectResult("'{}'.format(C())", S("X"));
        ExpectResult("'{}'.format(D())", S("X"));
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