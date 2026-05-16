namespace BishTest.Core;

public class ErrorTest(TestInfoFixture fixture) : Test(fixture)
{
    [Fact]
    public void TestThrow()
    {
        ExpectError("throw Error('error');", BishError.StaticType, "error");
    }

    [Fact]
    public void TestError()
    {
        Execute("x:=try throw Error('error');");
        ExpectResult("x.error.message", "'error'");
        ExpectResult("if(x is err e)e.message", "'error'");
        ExpectError("x.y;", BishError.StaticType);
        ExpectErrorResult("x?.y");
        ExpectResult("x??0", "0");
        ExpectResult("x is null", "false");
    }

    [Fact]
    public void TestWith()
    {
        Execute("a:=0;class W{func dispose(self)a+=1;};");
        Execute("with(W())0;");
        Execute("try with(_:W())throw Error('error');");
        Execute("((){with(W())return})();");
        ExpectResult("a", "3");
    }
}