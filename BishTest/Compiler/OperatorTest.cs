using System.Diagnostics.CodeAnalysis;

namespace BishTest.Compiler;

public class OperatorTest : Test
{
    public OperatorTest(TestInfoFixture fixture) : base(fixture) =>
        Scope.DefVar("f", BishBuiltinBinder.Builtin("f", F));

    [SuppressMessage("Usage", "CA2211")] public static int Count;

    public static BishBool F()
    {
        Count++;
        return False;
    }

    [Fact]
    public void TestOperator()
    {
        ExpectResult("-5/2+3*(4-1^6)", N(6.5));
        ExpectResult("1<2&&3>=3&&5<=>7==-1", True);
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

        Count = 0;
        ExpectResult("f()&&f()", True);
        Count.Should().Be(1);

        Count = 0;
        ExpectResult("true?3:f()", I(3));
        ExpectResult("false?f():3", I(3));
        Count.Should().Be(0);
    }

    [Fact]
    public void TestLogicAssign()
    {
        Count = 0;
        Execute("b:=true;b&&=f();");
        ExpectResult("b", False);
        Count.Should().Be(1);
        Execute("b:=false;b&&=f();");
        ExpectResult("b", False);
        Count.Should().Be(1);

        Count = 0;
        Execute("o:=0;o.x=true;o.x&&=f();");
        ExpectResult("o.x", False);
        Count.Should().Be(1);
        Execute("o:=0;o.x=false;o.x&&=f();");
        ExpectResult("o.x", False);
        Count.Should().Be(1);
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