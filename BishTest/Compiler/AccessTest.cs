namespace BishTest.Compiler;

[Collection("opt")]
public class AccessTest : CompilerTest
{
    public AccessTest(OptimizeInfoFixture fixture) : base(fixture)
    {
        Scope.DefVar("a", I(5));
        Scope.DefVar("b", S("abc"));
        Scope.DefVar("n", Null);
        var x = new BishObject();
        x.SetMember("y", new BishObject());
        x.SetMember("z", I(4));
        Scope.DefVar("x", x);
        Scope.DefVar("l", L(I(0), I(1), I(2), I(3), I(4)));
    }

    [Fact]
    public void TestAccess()
    {
        Execute("a=b.length;c:='c';x.y.z=0;del x.z;m:=del n;");
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
        Execute("{a=b.length;b:='b'}");
        Scope.GetVar("a").Should().BeEquivalentTo(I(3));
        Scope.GetVar("b").Should().BeEquivalentTo(S("abc"));
    }

    [Fact]
    public void TestIndex()
    {
        ExpectResult("l[0]", I(0));
        Execute("l[1]=-1;l[2]*=3;");
        Scope.GetVar("l").Should().BeEquivalentTo(L(I(0), I(-1), I(6), I(3), I(4)));
        Execute("del l[3];");
        Scope.GetVar("l").Should().BeEquivalentTo(L(I(0), I(-1), I(6), I(3)));
    }
}