namespace BishTest.Core;

public class ErrorTest(TestInfoFixture fixture) : Test(fixture)
{
    [Fact]
    public void TestThrow()
    {
        Action(() => Execute("throw Error('error');")).Should().Excepts(BishError.StaticType)
            .Which.Error.Message.Should().Be("error");
    }

    [Fact]
    public void TestError()
    {
        Execute("x:=try throw Error('error');");
        ExpectResult("x.error.message", S("error"));
        ExpectResult("if(x is err e)e.message", S("error"));
        Action(() => Execute("x.y;")).Should().Excepts(BishError.StaticType);
        ExpectErrorResult("x?.y");
        ExpectResult("x??0", I(0));
    }

    [Fact]
    public void TestWith()
    {
        Execute("a:=b:=c:=0;class W{enter(self)a+=1;exit(self,error)if(error is null)b+=1 else c+=1;};");
        Execute("with(W()){}");
        Execute("with(_:W()){throw Error('error');}");
        ExpectResult("a", I(2));
        ExpectResult("b", I(1));
        ExpectResult("c", I(1));
    }
}