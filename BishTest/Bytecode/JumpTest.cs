namespace BishTest.Bytecode;

public class JumpTest : Test
{
    [Fact]
    public void TestJump()
    {
        var frame = new BishFrame([
            new Bytecodes.Jump("tag"),
            new Bytecodes.Int(1),
            new Bytecodes.Int(2).Tagged("tag")
        ]);
        frame.Execute();
        frame.Stack.Pop().Should().BeEquivalentTo(new BishInt(2));
        frame.Stack.Should().BeEmpty();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TestJumpIf(bool condition)
    {
        var frame = new BishFrame([
            // condition ? 1 : 2
            new Bytecodes.Get(condition ? "true" : "false"),
            new Bytecodes.Copy(),
            new Bytecodes.JumpIf("if"),
            new Bytecodes.JumpIfNot("else"),
            new Bytecodes.Pop().Tagged("if"),
            new Bytecodes.Int(1),
            new Bytecodes.Jump("end"),
            new Bytecodes.Int(2).Tagged("else"),
            new Bytecodes.Nop().Tagged("end")
        ]);
        frame.Execute();
        frame.Stack.Pop().Should().BeEquivalentTo(new BishInt(condition ? 1 : 2));
        frame.Stack.Should().BeEmpty();
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 6)]
    [InlineData(100, 5050)]
    public void TestLoop(int n, int sum)
    {
        var frame = new BishFrame([
            new Bytecodes.Int(n),
            new Bytecodes.Def("n"),
            // i := 1
            new Bytecodes.Int(1),
            new Bytecodes.Def("i"),
            // s := 0
            new Bytecodes.Int(0),
            new Bytecodes.Def("s"),
            // do {
            new Bytecodes.Nop().Tagged("start"),
            // s = s + i
            new Bytecodes.Get("s"),
            new Bytecodes.Get("i"),
            new Bytecodes.Op("op_Add", 2),
            new Bytecodes.Set("s"),
            // i = i + 1
            new Bytecodes.Get("i"),
            new Bytecodes.Int(1),
            new Bytecodes.Op("op_Add", 2),
            new Bytecodes.Set("i"),
            // } while (i <= n)
            new Bytecodes.Get("i"),
            new Bytecodes.Get("n"),
            new Bytecodes.Op("op_Le", 2),
            new Bytecodes.JumpIf("start")
        ]);
        frame.Execute();
        frame.Scope.GetVar("s").Should().BeEquivalentTo(new BishInt(sum));
    }
}