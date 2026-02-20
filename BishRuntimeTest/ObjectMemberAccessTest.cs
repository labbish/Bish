using BishRuntime;
using FluentAssertions;

namespace BishRuntimeTest;

public class ObjectMemberAccessTest : Test
{
    public readonly BishType T1, T2;
    public readonly BishObject X;

    public ObjectMemberAccessTest()
    {
        T1 = new BishType("T1");
        T1.Members.Add("a", new BishInt(0));
        T1.Members.Add("b", new BishInt(0));
        T2 = new BishType("T2", [T1]);
        T2.Members.Add("a", new BishInt(1));
        T2.Members.Add("c", new BishInt(1));
        X = new BishObject(T2);
        X.Members.Add("c", new BishInt(2));
    }

    [Fact]
    public void TestTryGetMember()
    {
        T1.TryGetMember("a").Should().BeEquivalentTo(new BishInt(0));
        T1.TryGetMember("b").Should().BeEquivalentTo(new BishInt(0));
        T1.TryGetMember("c").Should().BeNull();
        T2.TryGetMember("a").Should().BeEquivalentTo(new BishInt(1));
        T2.TryGetMember("b").Should().BeEquivalentTo(new BishInt(0));
        T2.TryGetMember("c").Should().BeEquivalentTo(new BishInt(1));
        X.TryGetMember("a").Should().BeEquivalentTo(new BishInt(1));
        X.TryGetMember("b").Should().BeEquivalentTo(new BishInt(0));
        X.TryGetMember("c").Should().BeEquivalentTo(new BishInt(2));
    }

    [Fact]
    public void TestGetMember()
    {
        T1.GetMember("a").Should().BeEquivalentTo(new BishInt(0));
        T1.GetMember("b").Should().BeEquivalentTo(new BishInt(0));
        Action(() => T1.GetMember("c")).Should().Throw<BishNoSuchMemberException>();
        T2.GetMember("a").Should().BeEquivalentTo(new BishInt(1));
        T2.GetMember("b").Should().BeEquivalentTo(new BishInt(0));
        T2.GetMember("c").Should().BeEquivalentTo(new BishInt(1));
        X.GetMember("a").Should().BeEquivalentTo(new BishInt(1));
        X.GetMember("b").Should().BeEquivalentTo(new BishInt(0));
        X.GetMember("c").Should().BeEquivalentTo(new BishInt(2));
    }

    [Fact]
    public void TestSetMember()
    {
        T2.SetMember("a", new BishInt(3));
        X.SetMember("b", new BishInt(4));
        X.SetMember("c", new BishInt(5));
        
        T1.TryGetMember("a").Should().BeEquivalentTo(new BishInt(0));
        T1.TryGetMember("b").Should().BeEquivalentTo(new BishInt(0));
        T1.TryGetMember("c").Should().BeNull();
        T2.TryGetMember("a").Should().BeEquivalentTo(new BishInt(3));
        T2.TryGetMember("b").Should().BeEquivalentTo(new BishInt(0));
        T2.TryGetMember("c").Should().BeEquivalentTo(new BishInt(1));
        X.TryGetMember("a").Should().BeEquivalentTo(new BishInt(3));
        X.TryGetMember("b").Should().BeEquivalentTo(new BishInt(4));
        X.TryGetMember("c").Should().BeEquivalentTo(new BishInt(5));
    }

    [Fact]
    public void TestTryDelMember()
    {
        T2.TryDelMember("a").Should().BeEquivalentTo(new BishInt(1));
        X.TryDelMember("b").Should().BeNull();
        X.TryDelMember("c").Should().BeEquivalentTo(new BishInt(2));
        
        T1.TryGetMember("a").Should().BeEquivalentTo(new BishInt(0));
        T1.TryGetMember("b").Should().BeEquivalentTo(new BishInt(0));
        T1.TryGetMember("c").Should().BeNull();
        T2.TryGetMember("a").Should().BeEquivalentTo(new BishInt(0));
        T2.TryGetMember("b").Should().BeEquivalentTo(new BishInt(0));
        T2.TryGetMember("c").Should().BeEquivalentTo(new BishInt(1));
        X.TryGetMember("a").Should().BeEquivalentTo(new BishInt(0));
        X.TryGetMember("b").Should().BeEquivalentTo(new BishInt(0));
        X.TryGetMember("c").Should().BeEquivalentTo(new BishInt(1));
    }

    [Fact]
    public void TestDelMember()
    {
        T2.DelMember("a").Should().BeEquivalentTo(new BishInt(1));
        Action(() => X.DelMember("b")).Should().Throw<BishNoSuchMemberException>();
        X.DelMember("c").Should().BeEquivalentTo(new BishInt(2));
        
        T1.TryGetMember("a").Should().BeEquivalentTo(new BishInt(0));
        T1.TryGetMember("b").Should().BeEquivalentTo(new BishInt(0));
        T1.TryGetMember("c").Should().BeNull();
        T2.TryGetMember("a").Should().BeEquivalentTo(new BishInt(0));
        T2.TryGetMember("b").Should().BeEquivalentTo(new BishInt(0));
        T2.TryGetMember("c").Should().BeEquivalentTo(new BishInt(1));
        X.TryGetMember("a").Should().BeEquivalentTo(new BishInt(0));
        X.TryGetMember("b").Should().BeEquivalentTo(new BishInt(0));
        X.TryGetMember("c").Should().BeEquivalentTo(new BishInt(1));
    }
}