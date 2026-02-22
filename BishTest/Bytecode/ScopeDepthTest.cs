namespace BishTest.Bytecode;

public class ScopeDepthTest : Test
{
    public readonly BishScope Scope = new();

    public ScopeDepthTest() => Scope.DefVar("a", new BishInt(0));

    [Fact]
    public void TestScope()
    {
        var frame = new BishFrame([
            new BishBytecodeInner(),
            // a := 1
            new BishBytecodeInt(1),
            new BishBytecodeDef("a"),
            // b := 2
            new BishBytecodeInt(2),
            new BishBytecodeDef("b"),
            new BishBytecodeOuter()
        ], Scope);
        frame.Execute();
        Scope.TryGetVar("a").Should().BeEquivalentTo(new BishInt(0));
        Scope.TryGetVar("b").Should().BeNull();
    }
}