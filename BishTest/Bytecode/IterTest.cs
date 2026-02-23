namespace BishTest.Bytecode;

public class IterTest : Test
{
    public readonly BishScope Scope = new();

    [Fact]
    public void TestIter()
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
            // }
            new Bytecodes.Jump("start"),
            new Bytecodes.Nop().Tagged("end")
        ], Scope);
        frame.Execute();
        Scope.GetVar("x").Should().BeEquivalentTo(new BishString("abc"));
        Scope.GetVar("s").Should().BeEquivalentTo(new BishString("cba"));
    }
}