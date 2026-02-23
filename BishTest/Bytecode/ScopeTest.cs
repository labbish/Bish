namespace BishTest.Bytecode;

public class ScopeTest : Test
{
    public readonly BishScope Outer, Inner;

    public ScopeTest()
    {
        Outer = BishScope.Globals();
        Outer.DefVar("a", I(0));
        Outer.DefVar("b", I(0));

        Inner = Outer.CreateInner();
        Inner.DefVar("a", I(1));
        Inner.DefVar("c", I(1));
    }

    [Fact]
    public void TestGet()
    {
        var frame = new BishFrame([
            new Bytecodes.Get("c"),
            new Bytecodes.Get("b"),
            new Bytecodes.Get("a")
        ], Inner);
        frame.Execute();
        frame.Stack.Pop().Should().BeEquivalentTo(I(1)); // a
        frame.Stack.Pop().Should().BeEquivalentTo(I(0)); // b
        frame.Stack.Pop().Should().BeEquivalentTo(I(1)); // c
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
        ], Inner);
        frame.Execute();
        Outer.TryGetVar("a").Should().BeEquivalentTo(I(0));
        Outer.TryGetVar("b").Should().BeEquivalentTo(I(0));
        Outer.TryGetVar("c").Should().BeNull();
        Inner.TryGetVar("a").Should().BeEquivalentTo(I(1));
        Inner.TryGetVar("b").Should().BeEquivalentTo(I(2));
        Inner.TryGetVar("c").Should().BeEquivalentTo(I(2));
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
        ], Inner);
        frame.Execute();
        Outer.TryGetVar("a").Should().BeEquivalentTo(I(0));
        Outer.TryGetVar("b").Should().BeEquivalentTo(I(2));
        Outer.TryGetVar("c").Should().BeNull();
        Inner.TryGetVar("a").Should().BeEquivalentTo(I(1));
        Inner.TryGetVar("b").Should().BeEquivalentTo(I(2));
        Inner.TryGetVar("c").Should().BeEquivalentTo(I(2));
    }

    [Fact]
    public void TestDel()
    {
        var frame = new BishFrame([
            new Bytecodes.Del("a")
        ], Inner);
        frame.Execute();
        Outer.TryGetVar("a").Should().BeEquivalentTo(I(0));
        Outer.TryGetVar("b").Should().BeEquivalentTo(I(0));
        Outer.TryGetVar("c").Should().BeNull();
        Inner.TryGetVar("a").Should().BeEquivalentTo(I(0));
        Inner.TryGetVar("b").Should().BeEquivalentTo(I(0));
        Inner.TryGetVar("c").Should().BeEquivalentTo(I(1));
    }
}