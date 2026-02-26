namespace BishTest.Bytecode;

public class IterTest : Test
{
    public readonly BishScope Scope = BishScope.Globals;

    [Fact]
    public void TestIterString()
    {
        var frame = new BishFrame([
            // s := ""
            new Bytecodes.String(""),
            new Bytecodes.Def("s"),
            // x := "abc"
            new Bytecodes.String("abc"),
            new Bytecodes.Def("x"),
            // for (c : x) {
            new Bytecodes.Get("x"),
            new Bytecodes.Op("op_Iter", 1),
            new Bytecodes.ForIter("end").Tagged("start"),
            // s = c + s
            new Bytecodes.Get("s"),
            new Bytecodes.Op("op_Add", 2),
            new Bytecodes.Set("s"),
            new Bytecodes.Pop(),
            // }
            new Bytecodes.Jump("start"),
            new Bytecodes.Nop().Tagged("end")
        ], Scope);
        frame.Execute();
        Scope.GetVar("x").Should().BeEquivalentTo(S("abc"));
        Scope.GetVar("s").Should().BeEquivalentTo(S("cba"));
    }
    
    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 6)]
    [InlineData(100, 5050)]
    public void TestIterRange(int n, int s)
    {
        var frame = new BishFrame([
            // s := 0
            new Bytecodes.Int(0),
            new Bytecodes.Def("s"),
            // for i in range(n + 1) {
            new Bytecodes.Get("range"),
            new Bytecodes.Int(n + 1),
            new Bytecodes.Call(1),
            new Bytecodes.ForIter("end").Tagged("start"),
            // s = s + i
            new Bytecodes.Get("s"),
            new Bytecodes.Op("op_Add", 2),
            new Bytecodes.Set("s"),
            new Bytecodes.Pop(),
            // }
            new Bytecodes.Jump("start"),
            new Bytecodes.Nop().Tagged("end")
        ], Scope);
        frame.Execute();
        Scope.GetVar("s").Should().BeEquivalentTo(I(s));
    }
}