namespace BishTest.Runtime;

public class MemberHookTest : Test
{
    public readonly BishObject X = T.StaticType.CreateInstance([]), Y = U.StaticType.CreateInstance([]);

    public MemberHookTest()
    {
        X.Members.Add("a", new BishInt(0));
    }

    [Fact]
    public void TestObjectGetHook()
    {
        X.TryGetMember("a").Should().BeEquivalentTo(new BishInt(0));
        X.TryGetMember("b").Should().BeEquivalentTo(new BishString("get"));
    }

    [Fact]
    public void TestObjectSetHook()
    {
        X.SetMember("a", BishNull.Instance).Should().BeEquivalentTo(new BishString("set"));
        X.SetMember("b", BishNull.Instance).Should().BeEquivalentTo(new BishString("set"));
        X.Members.Should().HaveCount(1);
        X.Members["a"].Should().BeEquivalentTo(new BishInt(0));
    }


    [Fact]
    public void TestObjectDelHook()
    {
        X.TryDelMember("a").Should().BeEquivalentTo(new BishInt(0));
        X.TryDelMember("b").Should().BeEquivalentTo(new BishString("del"));
        X.Members.Should().HaveCount(0);
    }

    [Fact]
    public void TestMemberGetter()
    {
        Y.TryGetMember("a").Should().BeEquivalentTo(new BishInt(0));
    }

    [Fact]
    public void TestMemberSetter()
    {
        Y.SetMember("a", new BishInt(2)).Should().BeEquivalentTo(BishNull.Instance);
        Y.TryGetMember("a").Should().BeEquivalentTo(new BishInt(2));
    }

    [Fact]
    public void TestMemberDeller()
    {
        Y.TryDelMember("a").Should().BeEquivalentTo(BishNull.Instance);
        Y.TryGetMember("a").Should().BeEquivalentTo(new BishInt(0));
    }

    [Fact]
    public void TestNullAccess()
    {
        var n = BishNull.StaticType.CreateInstance([]);
        Action(() => n.TryGetMember("")).Should().Excepts(BishError.NullErrorType);
        Action(() => n.GetMember("")).Should().Excepts(BishError.NullErrorType);
        Action(() => n.SetMember("", n)).Should().Excepts(BishError.NullErrorType);
        Action(() => n.TryDelMember("")).Should().Excepts(BishError.NullErrorType);
        Action(() => n.DelMember("")).Should().Excepts(BishError.NullErrorType);
    }
}

file class T : BishObject
{
    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("T");

    [Builtin("hook")]
    public static T Create() => new();

    [Builtin("hook")]
    public static BishString Get(T self, BishString name) => new("get");

    [Builtin("hook")]
    public static BishString Set(T self, BishString name, BishObject value) => new("set");

    [Builtin("hook")]
    public static BishString Del(T self, BishString name) => new("del");

    static T() => BishBuiltinBinder.Bind<T>();
}

file class U : BishObject
{
    public int A;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("U");

    [Builtin("hook")]
    public static U Create()
    {
        return new U();
    }

    [Builtin("hook")]
    public static BishInt Get_a(U self)
    {
        return new BishInt(self.A);
    }

    [Builtin("hook")]
    public static BishNull Set_a(U self, BishInt value)
    {
        self.A = value.Value;
        return BishNull.Instance;
    }

    [Builtin("hook")]
    public static BishNull Del_a(U self)
    {
        return BishNull.Instance;
    }

    static U()
    {
        BishBuiltinBinder.Bind<U>();
    }
}