namespace BishTest.Bytecode;

public class ScopeDepthTest : Test
{
    public readonly BishScope Scope = BishScope.Globals();

    public ScopeDepthTest() => Scope.DefVar("a", new BishInt(0));

    [Fact]
    public void TestScope()
    {
        var frame = new BishFrame([
            new Bytecodes.Inner(),
            // a := 1
            new Bytecodes.Int(1),
            new Bytecodes.Def("a"),
            // b := 2
            new Bytecodes.Int(2),
            new Bytecodes.Def("b"),
            new Bytecodes.Outer()
        ], Scope);
        frame.Execute();
        Scope.TryGetVar("a").Should().BeEquivalentTo(new BishInt(0));
        Scope.TryGetVar("b").Should().BeNull();
    }
}