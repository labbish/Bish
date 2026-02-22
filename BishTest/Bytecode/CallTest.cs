namespace BishTest.Bytecode;

public class CallTest : Test
{
    public readonly BishScope Scope = new();

    public CallTest() => Scope.DefVar("f", BishBuiltinBinder.Builtin(F));

    public static BishInt F(BishInt a, BishInt b, [DefaultNull] BishInt? c) =>
        new(a.Value + b.Value * 10 + (c?.Value ?? 0) * 100);

    [Fact]
    public void TestCall1()
    {
        var frame = new BishFrame([
            // f(1, 2)
            new Bytecodes.Int(1),
            new Bytecodes.Int(2),
            new Bytecodes.Get("f"),
            new Bytecodes.Call(2)
        ], Scope);
        frame.Execute();
        frame.Stack.Pop().Should().BeEquivalentTo(new BishInt(21));
    }

    [Fact]
    public void TestCall2()
    {
        var frame = new BishFrame([
            // f(1, 2, 3)
            new Bytecodes.Int(1),
            new Bytecodes.Int(2),
            new Bytecodes.Int(3),
            new Bytecodes.Get("f"),
            new Bytecodes.Call(3)
        ], Scope);
        frame.Execute();
        frame.Stack.Pop().Should().BeEquivalentTo(new BishInt(321));
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
        frame.Stack.Pop().Should().BeEquivalentTo(new BishInt(3));
    }
}