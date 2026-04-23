namespace BishTest.Core;

public class NullableTest(TestInfoFixture fixture) : Test(fixture)
{
    [Fact]
    public void TestNullableCombine()
    {
        ExpectResult("1??2", "1");
        ExpectResult("null??2", "2");
        Execute("x:=null;x??=2;");
        ExpectResult("x", "2");
        Execute("o:=object();o.x:=null;o.x??=2;");
        ExpectResult("o.x", "2");
    }

    [Fact]
    public void TestNullableChain()
    {
        Execute("x:=null;y:=object();y.x:=null;");
        ExpectResult("x?.a[b](c)", "null");
        ExpectResult("x?[i].a[b](c)", "null");
        ExpectResult("x?(i).a[b](c)", "null");
        ExpectResult("y?.x?.a[b](c)", "null");
    }

    [Fact]
    public void TestNullish()
    {
        Execute("class N{func nullish(self)true};n:=N();");
        ExpectTrue("n?.a is of N");
        ExpectResult("n??0", "0");
    }
}