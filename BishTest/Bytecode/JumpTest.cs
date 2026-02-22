namespace BishTest.Bytecode;

public class JumpTest : Test
{
    [Fact]
    public void TestJump()
    {
        var frame = new BishFrame([
            new BishBytecodeJump("tag"),
            new BishBytecodeInt(1),
            new BishBytecodeInt(2).Tagged("tag")
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
            new BishBytecodeGet(condition ? "true" : "false"),
            new BishBytecodeCopy(),
            new BishBytecodeJumpIf("if"),
            new BishBytecodeJumpIfNot("else"),
            new BishBytecodePop().Tagged("if"),
            new BishBytecodeInt(1),
            new BishBytecodeJump("end"),
            new BishBytecodeInt(2).Tagged("else"),
            new BishBytecodeNop().Tagged("end")
        ]);
        frame.Execute();
        frame.Stack.Pop().Should().BeEquivalentTo(new BishInt(condition ? 1 : 2));
        frame.Stack.Should().BeEmpty();
    }
}