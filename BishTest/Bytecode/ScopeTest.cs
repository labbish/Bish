namespace BishTest.Bytecode;

public class ScopeTest : Test
{
    public readonly BishScope Inner, Outer;

    public ScopeTest()
    {
        Inner = new BishScope();
        Inner.DefVar("a", new BishInt(0));
        Inner.DefVar("b", new BishInt(0));

        Outer = new BishScope(Inner);
        Outer.DefVar("a", new BishInt(1));
        Outer.DefVar("c", new BishInt(1));
    }

    [Fact]
    public void TestGet()
    {
        var frame = new BishFrame([
            new BishBytecodeGet("c"),
            new BishBytecodeGet("b"),
            new BishBytecodeGet("a")
        ], Outer);
        frame.Execute();
        frame.Stack.Pop().Should().BeEquivalentTo(new BishInt(1)); // a
        frame.Stack.Pop().Should().BeEquivalentTo(new BishInt(0)); // b
        frame.Stack.Pop().Should().BeEquivalentTo(new BishInt(1)); // c
    }

    [Fact]
    public void TestDef()
    {
        var frame = new BishFrame([
            // b := 2
            new BishBytecodeInt(2),
            new BishBytecodeDef("b"),
            // c := 2
            new BishBytecodeInt(2),
            new BishBytecodeDef("c")
        ], Outer);
        frame.Execute();
        Inner.TryGetVar("a").Should().BeEquivalentTo(new BishInt(0));
        Inner.TryGetVar("b").Should().BeEquivalentTo(new BishInt(0));
        Inner.TryGetVar("c").Should().BeNull();
        Outer.TryGetVar("a").Should().BeEquivalentTo(new BishInt(1));
        Outer.TryGetVar("b").Should().BeEquivalentTo(new BishInt(2));
        Outer.TryGetVar("c").Should().BeEquivalentTo(new BishInt(2));
    }

    [Fact]
    public void TestSet()
    {
        var frame = new BishFrame([
            // b = 2
            new BishBytecodeInt(2),
            new BishBytecodeSet("b"),
            // c = 2
            new BishBytecodeInt(2),
            new BishBytecodeSet("c")
        ], Outer);
        frame.Execute();
        Inner.TryGetVar("a").Should().BeEquivalentTo(new BishInt(0));
        Inner.TryGetVar("b").Should().BeEquivalentTo(new BishInt(2));
        Inner.TryGetVar("c").Should().BeNull();
        Outer.TryGetVar("a").Should().BeEquivalentTo(new BishInt(1));
        Outer.TryGetVar("b").Should().BeEquivalentTo(new BishInt(2));
        Outer.TryGetVar("c").Should().BeEquivalentTo(new BishInt(2));
    }

    [Fact]
    public void TestDel()
    {
        var frame = new BishFrame([
            new BishBytecodeDel("a")
        ], Outer);
        frame.Execute();
        Inner.TryGetVar("a").Should().BeEquivalentTo(new BishInt(0));
        Inner.TryGetVar("b").Should().BeEquivalentTo(new BishInt(0));
        Inner.TryGetVar("c").Should().BeNull();
        Outer.TryGetVar("a").Should().BeEquivalentTo(new BishInt(0));
        Outer.TryGetVar("b").Should().BeEquivalentTo(new BishInt(0));
        Outer.TryGetVar("c").Should().BeEquivalentTo(new BishInt(1));
    }
}