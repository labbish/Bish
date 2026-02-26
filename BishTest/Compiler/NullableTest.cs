namespace BishTest.Compiler;

public class NullableTest(OptimizeInfoFixture fixture) : CompilerTest(fixture)
{
    [Fact]
    public void TestNullableCombine()
    {
        ExpectResult("1??2", I(1));
        ExpectResult("null??2", I(2));
        Execute("x:=null;x??=2;");
        ExpectResult("x", I(2));
        Execute("o:=0;o.x=null;o.x??=2;");
        ExpectResult("o.x", I(2));
    }

    [Fact]
    public void TestNullableChain()
    {
        Execute("x:=null;y:=object();y.x=null;");
        ExpectResult("x?.a[b](c)", Null);
        ExpectResult("x?[i].a[b](c)", Null);
        ExpectResult("x?(i).a[b](c)", Null);
        ExpectResult("y?.x?.a[b](c)", Null);
    }
}