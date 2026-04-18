namespace BishTest.Core;

public class CallTest : Test
{
    public CallTest(TestInfoFixture fixture) : base(fixture)
    {
        Scope.DefVar("f", BishBuiltinBinder.Builtin("f", F));
        Scope.DefVar("S", BishBuiltinBinder.Builtin("S", Sum));
    }

    public static BishInt F(BishInt a, BishInt b, [DefaultNull] BishInt? c) =>
        BishInt.Of(a.Value + b.Value * 10 + (c?.Value ?? 0) * 100);

    public static BishInt Sum([Rest] BishList nums) =>
        BishInt.Of(nums.List.Select(n => n.ExpectToBe<BishInt>("element").Value).Sum());

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