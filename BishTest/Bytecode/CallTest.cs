namespace BishTest.Bytecode;

public class CallTest : Test
{
    public readonly BishScope Scope = BishScope.Globals();

    public CallTest()
    {
        Scope.DefVar("f", BishBuiltinBinder.Builtin("f", F));
        Scope.DefVar("S", BishBuiltinBinder.Builtin("S", Sum));
    }

    public static BishInt F(BishInt a, BishInt b, [DefaultNull] BishInt? c) =>
        new(a.Value + b.Value * 10 + (c?.Value ?? 0) * 100);

    public static BishInt Sum([Rest] BishList nums) =>
        new(nums.List.Select(n => n.ExpectToBe<BishInt>("element").Value).Sum());

    [Theory]
    [InlineData(1, 2, 21)]
    [InlineData(1, 2, 3, 321)]
    public void TestCall(params int[] argsResult)
    {
        if (argsResult is not [.. var args, var result])
            throw new ArgumentException("TestCall requires arguments");
        var frame = new BishFrame([
            ..args.Select(x => new Bytecodes.Int(x)),
            new Bytecodes.Get("f"),
            new Bytecodes.Call(args.Length)
        ], Scope);
        frame.Execute();
        frame.Stack.Pop().Should().BeEquivalentTo(I(result));
    }

    [Fact]
    public void TestOperator()
    {
        var frame = new BishFrame([
            new Bytecodes.Int(1),
            new Bytecodes.Int(2),
            new Bytecodes.Op("op_Add", 2)
        ]);
        frame.Execute();
        frame.Stack.Pop().Should().BeEquivalentTo(I(3));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 2, 3)]
    [InlineData(1, 2, 3, 6)]
    public void TestRest(params int[] argsResult)
    {
        if (argsResult is not [.. var args, var result])
            throw new ArgumentException("TestCall requires arguments");
        var frame = new BishFrame([
            ..args.Select(n => new Bytecodes.Int(n)),
            new Bytecodes.BuildList(args.Length),
            new Bytecodes.Get("S"),
            new Bytecodes.CallArgs()
        ], Scope);
        frame.Execute();
        frame.Stack.Pop().Should().BeEquivalentTo(I(result));
    }
}