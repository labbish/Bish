using BishRuntime;
using FluentAssertions;

namespace BishRuntimeTest;

public class ObjectMemberAccessHookTest : Test
{
    public BishObject X;

    public ObjectMemberAccessHookTest()
    {
        X = T.StaticType.CreateInstance([]);
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