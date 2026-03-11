global using BishRuntime;
global using BishBytecode;
global using BishCompiler;
global using FluentAssertions;
global using Bytecodes = BishBytecode.Bytecodes;
using FluentAssertions.Specialized;

namespace BishTest;

[Collection("opt")]
public class Test(TestInfoFixture fixture)
{
    public TestInfoFixture Fixture => fixture;

    public readonly BishScope Scope = BishScope.Globals;
    
    protected static Action Action(Action action) => action;

    protected static BishInt I(int x) => BishInt.Of(x);
    protected static BishNum N(double x) => new(x);
    protected static BishString S(string s) => new(s);
    protected static BishList L(params List<BishObject> list) => new(list);

    protected static BishBool True => BishBool.True;
    protected static BishBool False => BishBool.False;

    protected static BishMap M(params List<(BishObject, BishObject)> entries) =>
        new(entries.Select(entry => new Entry(entry.Item1, entry.Item2)).ToList());

    protected static BishRange R(params int[] args) =>
        (BishRange)BishRange.StaticType.CreateInstance(args.Select(I).ToList<BishObject>());

    protected static BishType T(string name, params List<BishType> parents) => new(name, parents);

    protected static BishNull Null => BishNull.Instance;

    protected BishFrame Compile(string code)
    {
        var frame = BishCompiler.BishCompiler.Compile(code, out var errors, Scope, optimize: false);
        errors.Should().BeEmpty();
        Interlocked.Add(ref Fixture.Before, frame.Bytecodes.Count);
        frame.Bytecodes = BishOptimizer.Optimize(frame.Bytecodes);
        Interlocked.Add(ref Fixture.After, frame.Bytecodes.Count);
        return frame;
    }

    protected void Execute(string code)
    {
        var frame = Compile(code);
        frame.Execute();
        frame.Stack.Should().BeEmpty();
        frame.Scope.Vars.Keys.Where(key => key.StartsWith('$')).Should().BeEmpty();
    }

    protected BishObject Result(string expr)
    {
        Execute($"result:={expr};");
        return Scope.GetVar("result");
    }

    protected void ExpectResult(string expr, BishObject result) => Result(expr).Should().BeEquivalentTo(result);
}

public static class BishExceptionAssertion
{
    public static ExceptionAssertions<BishException> Excepts<TDelegate, TAssertions>(
        this DelegateAssertions<TDelegate, TAssertions> assertions, BishType? errorType = null)
        where TDelegate : Delegate
        where TAssertions : DelegateAssertions<TDelegate, TAssertions>
        => assertions.Throw<BishException>().Where(ex => ex.Error.Type.CanAssignTo(errorType ?? BishError.StaticType));
}

[CollectionDefinition("opt")]
public class TestCollectionWithSummary : ICollectionFixture<TestInfoFixture>;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class TestInfoFixture : IDisposable
{
    public int Before, After;

    public void Dispose() =>
        Console.WriteLine(
            $"Before Optimization: {Before}; After Optimization: {After}; Optimized {1 - (double)After / Before:P}");
}