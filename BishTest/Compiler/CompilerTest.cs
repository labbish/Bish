namespace BishTest.Compiler;

public class CompilerTest : Test
{
    public readonly BishScope Scope = BishScope.Globals;

    protected void ExpectResult(string expr, BishObject result)
    {
        var frame = Compile($"result:={expr};", Scope);
        frame.Execute();
        Scope.GetVar("result").Should().BeEquivalentTo(result);
    }
}