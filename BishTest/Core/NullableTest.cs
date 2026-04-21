namespace BishTest.Core;

public class NullableTest(TestInfoFixture fixture) : Test(fixture)
{
    [Fact]
    public void TestNullableCombine()
    {
        ExpectResult("1??2", I(1));
        ExpectResult("null??2", I(2));
        Execute("x:=null;x??=2;");
        ExpectResult("x", I(2));
        Execute("o:=object();o.x:=null;o.x??=2;");
        ExpectResult("o.x", I(2));
    }

    [Fact]
    public void TestNullableChain()
    {
        Execute("x:=null;y:=object();y.x:=null;");
        ExpectResult("x?.a[b](c)", Null);
        ExpectResult("x?[i].a[b](c)", Null);
        ExpectResult("x?(i).a[b](c)", Null);
        ExpectResult("y?.x?.a[b](c)", Null);
    }

    [Fact]
    public void TestNullish()
    {
        Execute("class N{func nullish(self)true};n:=N();");
        ExpectResult("n?.a is of N", True);
        ExpectResult("n??0", I(0));
    }
}