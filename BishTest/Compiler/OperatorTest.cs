using System.Diagnostics.CodeAnalysis;

namespace BishTest.Compiler;

[Collection("opt")]
public class OperatorTest : CompilerTest
{
    public OperatorTest(OptimizeInfoFixture fixture) : base(fixture) =>
        Scope.DefVar("f", BishBuiltinBinder.Builtin("f", F));

    [SuppressMessage("Usage", "CA2211")] public static int Count;

    public static BishBool F()
    {
        Count++;
        return B(false);
    }

    [Fact]
    public void TestOperator()
    {
        ExpectResult("-5/2+3*(4-1^6)", N(6.5));
        ExpectResult("1<2&&3>=3&&5<=>7==-1", B(true));
    }

    [Fact]
    public void TestLogic()
    {
        ExpectResult("true&&true", B(true));
        ExpectResult("true&&false", B(false));
        ExpectResult("false&&true", B(false));
        ExpectResult("false&&false", B(false));
        ExpectResult("true||true", B(true));
        ExpectResult("true||false", B(true));
        ExpectResult("false||true", B(true));
        ExpectResult("false||false", B(false));

        Count = 0;
        ExpectResult("f()&&f()", B(true));
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
        ExpectResult("b", B(false));
        Count.Should().Be(1);
        Execute("b:=false;b&&=f();");
        ExpectResult("b", B(false));
        Count.Should().Be(1);

        Count = 0;
        Execute("o:=0;o.x=true;o.x&&=f();");
        ExpectResult("o.x", B(false));
        Count.Should().Be(1);
        Execute("o:=0;o.x=false;o.x&&=f();");
        ExpectResult("o.x", B(false));
        Count.Should().Be(1);
    }

    [Fact]
    public void TestRefEqual()
    {
        Execute("x:=y:=object();z:=x;w:=object();");
        ExpectResult("x===y", B(true));
        ExpectResult("x!==z", B(false));
        ExpectResult("x===w", B(false));
    }
}