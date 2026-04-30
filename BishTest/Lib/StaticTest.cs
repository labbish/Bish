namespace BishTest.Lib;

public class StaticTest(TestInfoFixture fixture) : Test(fixture)
{
    [Fact]
    public void TestStatic()
    {
        Execute("@static class C{a:=0};");
        ExpectResult("C.a", "0");
        ExpectError("C();", BishError.TypeErrorType);
    }
}