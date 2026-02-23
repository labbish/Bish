namespace BishTest.Runtime;

public class MemberTest : Test
{
    public readonly BishType T1, T2;
    public readonly BishObject X;

    public MemberTest()
    {
        T1 = T("T1");
        T1.Members.Add("a", I(0));
        T1.Members.Add("b", I(0));
        T2 = T("T2", [T1]);
        T2.Members.Add("a", I(1));
        T2.Members.Add("c", I(1));
        X = new BishObject(T2);
        X.Members.Add("c", I(2));
    }

    [Fact]
    public void TestTryGetMember()
    {
        T1.TryGetMember("a").Should().BeEquivalentTo(I(0));
        T1.TryGetMember("b").Should().BeEquivalentTo(I(0));
        T1.TryGetMember("c").Should().BeNull();
        T2.TryGetMember("a").Should().BeEquivalentTo(I(1));
        T2.TryGetMember("b").Should().BeEquivalentTo(I(0));
        T2.TryGetMember("c").Should().BeEquivalentTo(I(1));
        X.TryGetMember("a").Should().BeEquivalentTo(I(1));
        X.TryGetMember("b").Should().BeEquivalentTo(I(0));
        X.TryGetMember("c").Should().BeEquivalentTo(I(2));
    }

    [Fact]
    public void TestGetMember()
    {
        T1.GetMember("a").Should().BeEquivalentTo(I(0));
        T1.GetMember("b").Should().BeEquivalentTo(I(0));
        Action(() => T1.GetMember("c")).Should().Excepts(BishError.AttributeErrorType);
        T2.GetMember("a").Should().BeEquivalentTo(I(1));
        T2.GetMember("b").Should().BeEquivalentTo(I(0));
        T2.GetMember("c").Should().BeEquivalentTo(I(1));
        X.GetMember("a").Should().BeEquivalentTo(I(1));
        X.GetMember("b").Should().BeEquivalentTo(I(0));
        X.GetMember("c").Should().BeEquivalentTo(I(2));
    }

    [Fact]
    public void TestSetMember()
    {
        T2.SetMember("a", I(3));
        X.SetMember("b", I(4));
        X.SetMember("c", I(5));

        T1.TryGetMember("a").Should().BeEquivalentTo(I(0));
        T1.TryGetMember("b").Should().BeEquivalentTo(I(0));
        T1.TryGetMember("c").Should().BeNull();
        T2.TryGetMember("a").Should().BeEquivalentTo(I(3));
        T2.TryGetMember("b").Should().BeEquivalentTo(I(0));
        T2.TryGetMember("c").Should().BeEquivalentTo(I(1));
        X.TryGetMember("a").Should().BeEquivalentTo(I(3));
        X.TryGetMember("b").Should().BeEquivalentTo(I(4));
        X.TryGetMember("c").Should().BeEquivalentTo(I(5));
    }

    [Fact]
    public void TestTryDelMember()
    {
        T2.TryDelMember("a").Should().BeEquivalentTo(I(1));
        X.TryDelMember("b").Should().BeNull();
        X.TryDelMember("c").Should().BeEquivalentTo(I(2));

        T1.TryGetMember("a").Should().BeEquivalentTo(I(0));
        T1.TryGetMember("b").Should().BeEquivalentTo(I(0));
        T1.TryGetMember("c").Should().BeNull();
        T2.TryGetMember("a").Should().BeEquivalentTo(I(0));
        T2.TryGetMember("b").Should().BeEquivalentTo(I(0));
        T2.TryGetMember("c").Should().BeEquivalentTo(I(1));
        X.TryGetMember("a").Should().BeEquivalentTo(I(0));
        X.TryGetMember("b").Should().BeEquivalentTo(I(0));
        X.TryGetMember("c").Should().BeEquivalentTo(I(1));
    }

    [Fact]
    public void TestDelMember()
    {
        T2.DelMember("a").Should().BeEquivalentTo(I(1));
        Action(() => X.DelMember("b")).Should().Excepts(BishError.AttributeErrorType);
        X.DelMember("c").Should().BeEquivalentTo(I(2));

        T1.TryGetMember("a").Should().BeEquivalentTo(I(0));
        T1.TryGetMember("b").Should().BeEquivalentTo(I(0));
        T1.TryGetMember("c").Should().BeNull();
        T2.TryGetMember("a").Should().BeEquivalentTo(I(0));
        T2.TryGetMember("b").Should().BeEquivalentTo(I(0));
        T2.TryGetMember("c").Should().BeEquivalentTo(I(1));
        X.TryGetMember("a").Should().BeEquivalentTo(I(0));
        X.TryGetMember("b").Should().BeEquivalentTo(I(0));
        X.TryGetMember("c").Should().BeEquivalentTo(I(1));
    }
}