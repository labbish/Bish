namespace BishTest.Bytecode;

public class MemberTest : Test
{
    public readonly BishScope Scope = new();

    public MemberTest()
    {
        var x = new BishObject();
        x.SetMember("a", new BishInt(1));
        x.SetMember("b", new BishInt(2));
        Scope.DefVar("x", x);
    }

    [Fact]
    public void TestGetMember()
    {
        var frame = new BishFrame([
            new BishBytecodeGet("x"),
            new BishBytecodeGetMember("a")
        ], Scope);
        frame.Execute();
        frame.Stack.Pop().Should().BeEquivalentTo(new BishInt(1));
    }
    
    [Fact]
    public void TestSetMember()
    {
        var frame = new BishFrame([
            // x.a = 0
            new BishBytecodeInt(0),
            new BishBytecodeGet("x"),
            new BishBytecodeSetMember("a")
        ], Scope);
        frame.Execute();
        frame.Stack.Should().BeEmpty();
        frame.Scope.GetVar("x").GetMember("a").Should().BeEquivalentTo(new BishInt(0));
    }
    
    [Fact]
    public void TestDelMember()
    {
        var frame = new BishFrame([
            new BishBytecodeGet("x"),
            new BishBytecodeDelMember("a")
        ], Scope);
        frame.Execute();
        frame.Stack.Should().BeEmpty();
        frame.Scope.GetVar("x").TryGetMember("a").Should().BeNull();
    }
}