using System.Diagnostics.CodeAnalysis;

namespace BishTest.Compiler;

[SuppressMessage("Usage", "CA2211")]
public class OperatorTest : CompilerTest
{
    public static int Count;

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
        
        Scope.DefVar("f", BishBuiltinBinder.Builtin("f", F));
        ExpectResult("f()&&f()", B(true));
        Count.Should().Be(1);
    }
}