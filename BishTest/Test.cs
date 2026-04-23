global using BishRuntime;
using System.Reflection;
using BishCompiler;
using Xunit.Sdk;

namespace BishTest;

[Collection("opt")]
public class Test(TestInfoFixture fixture)
{
    // ReSharper disable once UnusedMember.Global
    protected TestInfoFixture Fixture => fixture;

    protected readonly BishScope Scope = BishScope.Globals;

    private BishFrame Compile(string code)
    {
        var frame = BishCompiler.BishCompiler.Compile(code, out var errors, Scope);
        foreach (var error in errors) throw new CompilationException(error);
        using var stream = new MemoryStream();
        stream.WriteBytecodes(frame.Bytecodes);
        stream.Position = 0;
        frame.Bytecodes = stream.ReadBytecodes().ToList();
        return frame;
    }

    protected void ExpectCompileError(string code)
    {
        try
        {
            Compile(code);
        }
        catch (Exception)
        {
            return;
        }

        throw new AssertionFailedException("Expected compilation error but caught none");
    }

    private static void PostCheck(BishFrame frame)
    {
        if (frame.Stack.Count != 0)
            throw new AssertionFailedException($"Expect stack to be empty, found {string.Join(", ", frame.Stack)}");
        var specials = frame.Scope.Vars.Keys.Where(key => key.StartsWith('$')).ToList();
        if (specials.Count != 0)
            throw new AssertionFailedException($"Expect special vars to be empty, found {string.Join(", ", specials)}");
    }

    protected void Execute(string code)
    {
        var frame = Compile(code);
        frame.Execute();
        PostCheck(frame);
    }

    protected BishObject Result(string expr)
    {
        var frame = Compile(expr);
        frame.Execute();
        var result = frame.Stack.Pop();
        PostCheck(frame);
        return result;
    }

    protected void ExpectResult(string expr, string expected)
    {
        var expect = Result(expected);
        var result = Result(expr);
        if (BishOperator.Eq(expect, result)) return;
        throw new AssertionFailedException($"Expected {expect} but found {result}");
    }

    protected void ExpectTrue(string expr) => ExpectResult(expr, "true");

    protected void ExpectFalse(string expr) => ExpectResult(expr, "false");

    protected void ExpectError(string expr, BishType type, string? message = null)
    {
        try
        {
            Execute(expr);
        }
        catch (BishException e)
        {
            if (e.Error.Type.CanAssignTo(type))
            {
                if (message is null || message == e.Error.Message) return;
                throw new AssertionFailedException($"Expected message to be {message} but found {e.Error.Message}");
            }

            throw new AssertionFailedException($"Expected error to be {type} but found {e.Error.Type}");
        }

        throw new AssertionFailedException($"Expected {type} thrown but caught none");
    }

    protected void ExpectErrorResult(string expr)
    {
        var result = Result(expr);
        if (result is BishErrorResult) return;
        throw new AssertionFailedException($"Expected ErrorResult but found {result}");
    }
}

public class AssertionFailedException(string message) : Exception(message);

public class CompilationException(CompilationError error) : Exception($"Compilation error: {error}");

[CollectionDefinition("opt")]
public class TestCollectionWithSummary : ICollectionFixture<TestInfoFixture>;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class TestInfoFixture : IDisposable
{
    public void Dispose() => Console.WriteLine(BishOptimizer.Info());
}

[AttributeUsage(AttributeTargets.Method)]
public class RepeatAttribute(int count) : DataAttribute
{
    public override IEnumerable<object[]> GetData(MethodInfo testMethod) =>
        Enumerable.Range(0, count).Select(i => new object[] { i });
}