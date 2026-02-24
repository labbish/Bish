namespace BishTest.Bytecode;

public class MemberTest : Test
{
    public readonly BishScope Scope = BishScope.Globals;

    public MemberTest()
    {
        var x = new BishObject();
        x.SetMember("a", I(1));
        x.SetMember("b", I(2));
        Scope.DefVar("x", x);
    }

    [Fact]
    public void TestGetMember()
    {
        var frame = new BishFrame([
            new Bytecodes.Get("x"),
            new Bytecodes.GetMember("a")
        ], Scope);
        frame.Execute();
        frame.Stack.Pop().Should().BeEquivalentTo(I(1));
    }

    [Fact]
    public void TestSetMember()
    {
        var frame = new BishFrame([
            // x.a = 0
            new Bytecodes.Int(0),
            new Bytecodes.Get("x"),
            new Bytecodes.SetMember("a")
        ], Scope);
        frame.Execute();
        frame.Scope.GetVar("x").GetMember("a").Should().BeEquivalentTo(I(0));
    }

    [Fact]
    public void TestDelMember()
    {
        var frame = new BishFrame([
            new Bytecodes.Get("x"),
            new Bytecodes.DelMember("a")
        ], Scope);
        frame.Execute();
        frame.Scope.GetVar("x").TryGetMember("a").Should().BeNull();
    }
}