namespace BishTest.Compiler;

[Collection("opt")]
public class CompilerTest(OptimizeInfoFixture fixture) : Test
{
    public OptimizeInfoFixture Fixture => fixture;

    public readonly BishScope Scope = BishScope.Globals;

    protected BishFrame Compile(string code, BishScope scope)
    {
        var frame = BishCompiler.BishCompiler.Compile(code, scope, ErrorHandlingMode.Throw, optimize: false);
        Interlocked.Add(ref Fixture.Before, frame.Bytecodes.Count);
        frame.Bytecodes = BishOptimizer.Optimize(frame.Bytecodes);
        Interlocked.Add(ref Fixture.After, frame.Bytecodes.Count);
        return frame;
    }

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

[CollectionDefinition("opt")]
public class TestCollectionWithSummary : ICollectionFixture<OptimizeInfoFixture>;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class OptimizeInfoFixture : IDisposable
{
    public int Before, After;

    public void Dispose() =>
        Console.WriteLine(
            $"Before Optimization: {Before}; After Optimization: {After}; Optimized {1 - (double)After / Before:P}");
}