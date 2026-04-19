namespace BishTest.Core;

public class CallTest : Test
{
    public CallTest(TestInfoFixture fixture) : base(fixture) =>
        Execute("func f(a,b,c:0)a+b*10+c*100;func S(..nums)nums.iter().reduce((a,b)a+b,0);");

    [Fact]
    public void TestCall()
    {
        ExpectResult("S()", I(0));
        ExpectResult("S(1)", I(1));
        ExpectResult("S(1, 2,)", I(3));
        ExpectResult("S(1, 2, 3)", I(6));
        ExpectResult("S(1, 2, 3, 4)", I(10));

        ExpectResult("1+2", I(3));
    }

    [Fact]
    public void TestRest()
    {
        ExpectResult("f(1,2)", I(21));
        ExpectResult("f(1,2,3)", I(321));

        ExpectResult("S(..[])", I(0));
        ExpectResult("S(1, ..[2, 3], 4)", I(10));
        ExpectResult("[0, ..[1, ..[2, 3]], 4]", L(I(0), I(1), I(2), I(3), I(4)));
    }
}