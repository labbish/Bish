namespace BishTest.Lib;

public class MacrosTest(TestInfoFixture fixture) : Test(fixture)
{
    [Fact]
    public void TestMacro()
    {
        ExpectResult("#macro.exec[a:=1];a:=0;x:=#macro.eval[a];#macro.exec[a:=2];[x,a]", "[1,0]");
    }
}