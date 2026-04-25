global using BishRuntime;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Xunit.Sdk;

namespace BishTest;

[Collection("opt")]
public class Test(TestInfoFixture fixture)
{
    // ReSharper disable once UnusedMember.Global
    protected TestInfoFixture Fixture => fixture;
    protected readonly BishScope Scope = BishScope.Globals;
    
    [DoesNotReturn]
    protected static void Fail(string message) => throw new AssertionFailedException(message);

    private BishFrame Compile(string code)
    {
        var frame = BishCompileService.Compile(code, scope: Scope);
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

        Fail("Expected compilation error but caught none");
    }

    private static void PostCheck(BishFrame frame)
    {
        if (frame.Stack.Count != 0)
            Fail($"Expect stack to be empty, found {string.Join(", ", frame.Stack)}");
        var specials = frame.Scope.Vars.Keys.Where(key => key.StartsWith('$')).ToList();
        if (specials.Count != 0)
            Fail($"Expect special vars to be empty, found {string.Join(", ", specials)}");
    }
    
    [SuppressMessage("Usage", "VSTHRD002")]
    public static BishFrame ExecuteWithTimeout(BishFrame frame)
    {
        try
        {
            var task = Task.Run(frame.Execute);
            return task.Wait(3000) ? frame : throw new TimeoutException("Time limit exceeded");
        }
        catch (AggregateException e)
        {
            throw e.Flatten().InnerExceptions.First();
        }
    }

    protected void Execute(string code) => PostCheck(ExecuteWithTimeout(Compile(code)));

    protected BishObject Result(string expr)
    {
        var frame = ExecuteWithTimeout(Compile(expr));
        var result = frame.Stack.Pop();
        PostCheck(frame);
        return result;
    }

    protected void ExpectResult(string expr, string expected)
    {
        var expect = Result(expected);
        var result = Result(expr);
        if (BishOperator.Eq(expect, result)) return;
        Fail($"Expected {expect} but found {result}");
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
                Fail($"Expected message to be {message} but found {e.Error.Message}");
            }

            Fail($"Expected error to be {type} but found {e.Error.Type}");
        }

        Fail($"Expected {type} thrown but caught none");
    }

    protected void ExpectErrorResult(string expr)
    {
        var result = Result(expr);
        if (result is BishErrorResult) return;
        Fail($"Expected ErrorResult but found {result}");
    }

    static Test() => BishCompiler.BishCompiler.Init();
}

public class AssertionFailedException(string message) : Exception(message);

[CollectionDefinition("opt")]
public class TestCollectionWithSummary : ICollectionFixture<TestInfoFixture>;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class TestInfoFixture : IDisposable
{
    public void Dispose()
    {
        using var stdout = new StreamWriter(Console.OpenStandardOutput());
        stdout.AutoFlush = true;
        stdout.WriteLine(BishOptimizer.Info());
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class RepeatAttribute(int count) : DataAttribute
{
    public override IEnumerable<object[]> GetData(MethodInfo testMethod) =>
        Enumerable.Range(0, count).Select(i => new object[] { i });
}