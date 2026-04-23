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
    }

    [Fact]
    public void TestWith()
    {
        Execute("a:=b:=c:=0;class W{enter(self)a+=1;exit(self,error)if(error is null)b+=1 else c+=1;};");
        Execute("with(W()){}");
        Execute("with(_:W()){throw Error('error');}");
        ExpectResult("a", "2");
        ExpectResult("b", "1");
        ExpectResult("c", "1");
    }
}