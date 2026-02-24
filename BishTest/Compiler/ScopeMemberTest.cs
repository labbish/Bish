namespace BishTest.Compiler;

public class ScopeMemberTest : Test
{
    public readonly BishScope Scope = BishScope.Globals;

    public ScopeMemberTest()
    {
        Scope.DefVar("a", I(5));
        Scope.DefVar("b", S("abc"));
        Scope.DefVar("n", Null);
        var x = new BishObject();
        x.SetMember("y", new BishObject());
        x.SetMember("z", I(4));
        Scope.DefVar("x", x);
    }

    [Fact]
    public void TestScopeMember()
    {
        var frame = Compile("a=b.length;c:='c';x.y.z=0;del x.z;m:=del n;", Scope);
        frame.Execute();
        Scope.GetVar("a").Should().BeEquivalentTo(I(3));
        Scope.GetVar("b").Should().BeEquivalentTo(S("abc"));
        Scope.GetVar("c").Should().BeEquivalentTo(S("c"));
        Scope.GetVar("x").GetMember("y").GetMember("z").Should().BeEquivalentTo(I(0));
        Scope.GetVar("x").TryGetMember("z").Should().BeNull();
        Scope.TryGetVar("n").Should().BeNull();
        Scope.GetVar("m").Should().BeEquivalentTo(Null);
    }

    [Fact]
    public void TestScopeDepth()
    {
        var frame = Compile("{a=b.length;b:='b'}", Scope);
        frame.Execute();
        Scope.GetVar("a").Should().BeEquivalentTo(I(3));
        Scope.GetVar("b").Should().BeEquivalentTo(S("abc"));
    }
}