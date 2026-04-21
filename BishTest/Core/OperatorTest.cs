namespace BishTest.Core;

public class OperatorTest : Test
{
    public OperatorTest(TestInfoFixture fixture) : base(fixture) =>
        Execute("count:=0;func f(){count+=1;false};");

    [Fact]
    public void TestOperator()
    {
        ExpectResult("1+2", I(3));
        ExpectResult("1+2.5", N(3.5));
        ExpectResult("1.5+2", N(3.5));
        Action(() => Execute("1+'2';")).Should().Excepts(BishError.ArgumentErrorType);

        ExpectResult("-5/2+3*(4-1^6)", N(6.5));
        ExpectResult("1<2&&3>=3&&5<=>7==-1", True);
    }

    [Fact]
    public void TestEquality()
    {
        ExpectResult("1==1", True);
        ExpectResult("1==2", False);
        ExpectResult("1==1.0", True);
        ExpectResult("1.0==1", True);
        ExpectResult("1.0==1", True);
        ExpectResult("object()==object()", False);
        ExpectResult("1==''", False);
    }

    [Fact]
    public void TestAutoCompare()
    {
        ExpectResult("1!=1", False);
        ExpectResult("1!=2", True);

        ExpectResult("1<2", True);
        ExpectResult("2<2", False);
        ExpectResult("2<1", False);

        ExpectResult("1<=2", True);
        ExpectResult("2<=2", True);
        ExpectResult("2<=1", False);

        ExpectResult("1>2", False);
        ExpectResult("2>2", False);
        ExpectResult("2>1", True);

        ExpectResult("1>=2", False);
        ExpectResult("2>=2", True);
        ExpectResult("2>=1", True);
    }

    [Fact]
    public void TestLogic()
    {
        ExpectResult("true&&true", True);
        ExpectResult("true&&false", False);
        ExpectResult("false&&true", False);
        ExpectResult("false&&false", False);
        ExpectResult("true||true", True);
        ExpectResult("true||false", True);
        ExpectResult("false||true", True);
        ExpectResult("false||false", False);

        Execute("count=0;");
        ExpectResult("f()&&f()", True);
        ExpectResult("count", I(1));

        Execute("count=0;");
        ExpectResult("if(true)3else f()", I(3));
        ExpectResult("if(false)f()else 3", I(3));
        ExpectResult("count", I(0));
    }

    [Fact]
    public void TestLogicAssign()
    {
        Execute("count=0;");
        Execute("b:=true;b&&=f();");
        ExpectResult("b", False);
        ExpectResult("count", I(1));
        Execute("b:=false;b&&=f();");
        ExpectResult("b", False);
        ExpectResult("count", I(1));

        Execute("count=0;");
        Execute("o:=object();o.x:=true;o.x&&=f();");
        ExpectResult("o.x", False);
        ExpectResult("count", I(1));
        Execute("o:=object();o.x:=false;o.x&&=f();");
        ExpectResult("o.x", False);
        ExpectResult("count", I(1));
    }

    [Fact]
    public void TestRefEqual()
    {
        Execute("x:=y:=object();z:=x;w:=object();");
        ExpectResult("x===y", True);
        ExpectResult("x!==z", False);
        ExpectResult("x===w", False);

        ExpectResult("null===null", True);
        ExpectResult("true===true", True);
        ExpectResult("false===false", True);
    }
}