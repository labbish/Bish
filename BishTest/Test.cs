global using BishRuntime;
global using BishCompiler;
global using FluentAssertions;
using System.Reflection;
using FluentAssertions.Specialized;
using Xunit.Sdk;

namespace BishTest;

[Collection("opt")]
public class Test(TestInfoFixture fixture)
{
    // ReSharper disable once UnusedMember.Global
    public TestInfoFixture Fixture => fixture;

    public readonly BishScope Scope = BishScope.Globals;

    protected static Action Action(Action action) => action;

    protected static BishInt I(int x) => BishInt.Of(x);
    protected static BishNum N(double x) => new(x);
    protected static BishString S(string s) => new(s);
    protected static BishList L(params IList<BishObject> list) => new(list);

    protected static BishBool True => BishBool.True;
    protected static BishBool False => BishBool.False;

    protected static BishMap M(params IList<(BishObject, BishObject)> entries) =>
        new(entries.Select(entry => new Entry(entry.Item1, entry.Item2)).ToList());

    protected static BishRange R(params int[] args) =>
        (BishRange)BishRange.StaticType.CreateInstance(args.Select(I).ToList<BishObject>());

    protected static BishType T(string name, params IList<BishType> parents) => new(name, parents);

    protected static BishNull Null => BishNull.Instance;

    protected BishFrame Compile(string code)
    {
        var frame = BishCompiler.BishCompiler.Compile(code, out var errors, Scope);
        errors.Should().BeEmpty();
        TestParse(frame);
        return frame;
    }

    private static void TestParse(BishFrame frame)
    {
        using var stream = new MemoryStream();
        stream.WriteBytecodes(frame.Bytecodes);
        stream.Position = 0;
        frame.Bytecodes = stream.ReadBytecodes().ToList();
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
        var frame = Compile(expr);
        frame.Execute();
        var result = frame.Stack.Pop();
        frame.Stack.Should().BeEmpty();
        frame.Scope.Vars.Keys.Where(key => key.StartsWith('$')).Should().BeEmpty();
        return result;
    }

    protected void ExpectResult(string expr, BishObject result) => Result(expr).Should().BeEquivalentTo(result);

    protected void ExpectErrorResult(string expr) => Result(expr).Should().BeOfType<BishErrorResult>();
}

public static class BishExceptionAssertion
{
    public static ExceptionAssertions<BishException> Excepts<TDelegate, TAssertions>(
        this DelegateAssertions<TDelegate, TAssertions> assertions, BishType errorType)
        where TDelegate : Delegate
        where TAssertions : DelegateAssertions<TDelegate, TAssertions>
        => assertions.Throw<BishException>().Where(ex => ex.Error.Type.CanAssignTo(errorType));
}

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