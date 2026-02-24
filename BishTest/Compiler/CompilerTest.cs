namespace BishTest.Compiler;

public class CompilerTest : Test
{
    public readonly BishScope Scope = BishScope.Globals;
    
    protected void Execute(string code)
    {
        var frame = Compile(code, Scope);
        frame.Execute();
        frame.Stack.Should().BeEmpty();
    }

    protected void ExpectResult(string expr, BishObject result)
    {
        Execute($"result:={expr};");
        Scope.GetVar("result").Should().BeEquivalentTo(result);
    }
}