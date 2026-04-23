namespace BishTest.Core;

public class CallTest : Test
{
    public CallTest(TestInfoFixture fixture) : base(fixture) =>
        Execute("func f(a,b,c:0)a+b*10+c*100;func S(..nums)nums.iter().reduce((a,b)a+b,0);");

    [Fact]
    public void TestCall()
    {
        ExpectResult("S()", "0");
        ExpectResult("S(1)", "1");
        ExpectResult("S(1, 2,)", "3");
        ExpectResult("S(1, 2, 3)", "6");
        ExpectResult("S(1, 2, 3, 4)", "10");

        ExpectResult("1+2", "3");
    }

    [Fact]
    public void TestRest()
    {
        ExpectResult("f(1,2)", "21");
        ExpectResult("f(1,2,3)", "321");

        ExpectResult("S(..[])", "0");
        ExpectResult("S(1, ..[2, 3], 4)", "10");
        ExpectResult("[0, ..[1, ..[2, 3]], 4]", "[0,1,2,3,4]");
    }
}