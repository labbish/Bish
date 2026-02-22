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
}