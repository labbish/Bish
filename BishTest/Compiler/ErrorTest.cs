namespace BishTest.Compiler;

[Collection("opt")]
public class ErrorTest(OptimizeInfoFixture fixture) : CompilerTest(fixture)
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
        Execute("x:=s:=0;try throw Error('error');catch(e)=>x=e;finally{s=1;}");
        Scope.GetVar("x").Should().BeOfType<BishError>().Which.Message.Should().Be("error");
        Scope.GetVar("s").Should().BeEquivalentTo(I(1));
    }
}