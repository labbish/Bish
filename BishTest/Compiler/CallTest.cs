namespace BishTest.Compiler;

public class CallTest : CompilerTest
{
    public CallTest() => Scope.DefVar("S", BishBuiltinBinder.Builtin("S", Sum));

    public static BishInt Sum([Rest] BishList nums) =>
        new(nums.List.Select(n => n.ExpectToBe<BishInt>("element").Value).Sum());

    [Fact]
    public void TestCall()
    {
        ExpectResult("S()", I(0));
        ExpectResult("S(1)", I(1));
        ExpectResult("S(1, 2,)", I(3));
        ExpectResult("S(1, 2, 3)", I(6));
        ExpectResult("S(1, 2, 3, 4)", I(10));
    }
}