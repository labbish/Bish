namespace BishTest.Core;

public class OperatorTest : Test
{
    public OperatorTest(TestInfoFixture fixture) : base(fixture) =>
        Execute("count:=0;func f(){count+=1;false};");

    [Fact]
    public void TestOperator()
    {
        ExpectResult("1+2", "3");
        ExpectResult("1+2.5", "3.5");
        ExpectResult("1.5+2", "3.5");
        ExpectError("1+'2';", BishError.ArgumentErrorType);

        ExpectResult("-5/2+3*(4-1^6)", "6.5");
        ExpectTrue("1<2&&3>=3&&5<=>7==-1");
    }

    [Fact]
    public void TestEquality()
    {
        ExpectTrue("1==1");
        ExpectFalse("1==2");
        ExpectTrue("1==1.0");
        ExpectTrue("1.0==1");
        ExpectTrue("1.0==1");
        ExpectFalse("object()==object()");
        ExpectFalse("1==''");
    }

    [Fact]
    public void TestAutoCompare()
    {
        ExpectFalse("1!=1");
        ExpectTrue("1!=2");

        ExpectTrue("1<2");
        ExpectFalse("2<2");
        ExpectFalse("2<1");

        ExpectTrue("1<=2");
        ExpectTrue("2<=2");
        ExpectFalse("2<=1");

        ExpectFalse("1>2");
        ExpectFalse("2>2");
        ExpectTrue("2>1");

        ExpectFalse("1>=2");
        ExpectTrue("2>=2");
        ExpectTrue("2>=1");
    }

    [Fact]
    public void TestLogic()
    {
        ExpectTrue("true&&true");
        ExpectFalse("true&&false");
        ExpectFalse("false&&true");
        ExpectFalse("false&&false");
        ExpectTrue("true||true");
        ExpectTrue("true||false");
        ExpectTrue("false||true");
        ExpectFalse("false||false");

        Execute("count=0;");
        ExpectFalse("f()&&f()");
        ExpectResult("count", "1");

        Execute("count=0;");
        ExpectResult("if(true)3else f()", "3");
        ExpectResult("if(false)f()else 3", "3");
        ExpectResult("count", "0");
    }

    [Fact]
    public void TestLogicAssign()
    {
        Execute("count=0;");
        Execute("b:=true;b&&=f();");
        ExpectFalse("b");
        ExpectResult("count", "1");
        Execute("b:=false;b&&=f();");
        ExpectFalse("b");
        ExpectResult("count", "1");

        Execute("count=0;");
        Execute("o:=object();o.x:=true;o.x&&=f();");
        ExpectFalse("o.x");
        ExpectResult("count", "1");
        Execute("o:=object();o.x:=false;o.x&&=f();");
        ExpectFalse("o.x");
        ExpectResult("count", "1");
    }

    [Fact]
    public void TestRefEqual()
    {
        Execute("x:=y:=object();z:=x;w:=object();");
        ExpectTrue("x===y");
        ExpectFalse("x!==z");
        ExpectFalse("x===w");

        ExpectTrue("null===null");
        ExpectTrue("true===true");
        ExpectTrue("false===false");
    }
}