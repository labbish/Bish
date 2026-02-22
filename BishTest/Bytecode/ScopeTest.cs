namespace BishTest.Bytecode;

public class ScopeTest : Test
{
    public readonly BishScope Inner, Outer;

    public ScopeTest()
    {
        Inner = BishScope.Globals();
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
            new Bytecodes.Get("c"),
            new Bytecodes.Get("b"),
            new Bytecodes.Get("a")
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
            new Bytecodes.Int(2),
            new Bytecodes.Def("b"),
            // c := 2
            new Bytecodes.Int(2),
            new Bytecodes.Def("c")
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
            new Bytecodes.Int(2),
            new Bytecodes.Set("b"),
            // c = 2
            new Bytecodes.Int(2),
            new Bytecodes.Set("c")
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
            new Bytecodes.Del("a")
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