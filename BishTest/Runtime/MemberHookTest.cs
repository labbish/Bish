namespace BishTest.Runtime;

public class MemberHookTest : Test
{
    public readonly BishObject X = T.StaticType.CreateInstance([]), Y = U.StaticType.CreateInstance([]);

    public MemberHookTest()
    {
        X.Members.Add("a", I(0));
    }

    [Fact]
    public void TestObjectGetHook()
    {
        X.TryGetMember("a").Should().BeEquivalentTo(I(0));
        X.TryGetMember("b").Should().BeEquivalentTo(S("get"));
    }

    [Fact]
    public void TestObjectSetHook()
    {
        X.SetMember("a", Null).Should().BeEquivalentTo(S("set"));
        X.SetMember("b", Null).Should().BeEquivalentTo(S("set"));
        X.Members.Should().HaveCount(1);
        X.Members["a"].Should().BeEquivalentTo(I(0));
    }


    [Fact]
    public void TestObjectDelHook()
    {
        X.TryDelMember("a").Should().BeEquivalentTo(I(0));
        X.TryDelMember("b").Should().BeEquivalentTo(S("del"));
        X.Members.Should().HaveCount(0);
    }

    [Fact]
    public void TestMemberGetter()
    {
        Y.TryGetMember("a").Should().BeEquivalentTo(I(0));
    }

    [Fact]
    public void TestMemberSetter()
    {
        Y.SetMember("a", I(2)).Should().BeEquivalentTo(Null);
        Y.TryGetMember("a").Should().BeEquivalentTo(I(2));
    }

    [Fact]
    public void TestMemberDeller()
    {
        Y.TryDelMember("a").Should().BeEquivalentTo(Null);
        Y.TryGetMember("a").Should().BeEquivalentTo(I(0));
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
    public static U Create() => new();

    [Builtin("hook")]
    public static BishInt Get_a(U self) => new(self.A);

    [Builtin("hook")]
    public static void Set_a(U self, BishInt value) => self.A = value.Value;

    [Builtin("hook")]
    public static BishNull Del_a(U self) => BishNull.Instance;

    static U()
    {
        BishBuiltinBinder.Bind<U>();
    }
}