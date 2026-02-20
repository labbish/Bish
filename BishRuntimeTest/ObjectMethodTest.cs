using BishRuntime;
using FluentAssertions;

namespace BishRuntimeTest;

public class ObjectMethodTest : Test
{
    public readonly BishObject X = T.StaticType.CreateInstance([new BishString("x")]);

    private void Inject() =>
        X.Members.Add("f", new BishFunc([new BishArg("_", Default: BishNull.Instance)], _ => new BishInt(1)));

    [Fact]
    public void TestObjectInitialization()
    {
        X.Should().BeEquivalentTo(new T("x"));
    }

    [Fact]
    public void TestMethodTryCall()
    {
        Inject();
        new BishInt(0).TryCall([]).Should().BeNull();
        X.TryCall([]).Should().BeEquivalentTo(new BishInt(0));
        Action(() => X.TryCall([BishNull.Instance])).Should().Throw<BishArgumentException>();
        X.GetMember("f").TryCall([]).Should().BeEquivalentTo(new BishInt(1));
        X.GetMember("f").TryCall([BishNull.Instance]).Should().BeEquivalentTo(new BishInt(1));
        Action(() => X.GetMember("g").TryCall([])).Should().Throw<BishArgumentException>();
        Action(() => X.GetMember("g").TryCall([BishNull.Instance])).Should()
            .Throw<BishArgumentException>().WithInnerException<BishTypeException>();
        Action(() => T.StaticType.GetMember("g").TryCall([])).Should().Throw<BishArgumentException>();
        Action(() => T.StaticType.GetMember("g").TryCall([BishNull.Instance, X])).Should()
            .Throw<BishArgumentException>().WithInnerException<BishTypeException>();
        X.GetMember("toString").TryCall([]).Should().BeEquivalentTo(new BishString("T(x)"));
        T.StaticType.GetMember("getName").TryCall([]).Should().BeEquivalentTo(new BishString("T"));
    }

    [Fact]
    public void TestMethodCall()
    {
        Inject();
        Action(() => new BishInt(0).Call([])).Should().Throw<BishNotCallableException>();
        X.Call([]).Should().BeEquivalentTo(new BishInt(0));
        Action(() => X.Call([BishNull.Instance])).Should().Throw<BishArgumentException>();
        X.GetMember("f").Call([]).Should().BeEquivalentTo(new BishInt(1));
        X.GetMember("f").Call([BishNull.Instance]).Should().BeEquivalentTo(new BishInt(1));
        Action(() => X.GetMember("g").Call([])).Should().Throw<BishArgumentException>();
        Action(() => X.GetMember("g").Call([BishNull.Instance])).Should()
            .Throw<BishArgumentException>().WithInnerException<BishTypeException>();
        Action(() => T.StaticType.GetMember("g").Call([])).Should().Throw<BishArgumentException>();
        Action(() => T.StaticType.GetMember("g").Call([BishNull.Instance, X])).Should()
            .Throw<BishArgumentException>().WithInnerException<BishTypeException>();
        X.GetMember("toString").Call([]).Should().BeEquivalentTo(new BishString("T(x)"));
        T.StaticType.GetMember("getName").Call([]).Should().BeEquivalentTo(new BishString("T"));
    }
}

file class T(string tag) : BishObject
{
    public string Tag = tag;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("T");

    [Builtin]
    public static BishInt F(T self) => new(0);

    [Builtin]
    public static BishInt G(T self, T _) => new(0);

    [Builtin("hook")]
    public static T Create()
    {
        Console.WriteLine("Create");
        return new T("");
    }

    [Builtin("hook")]
    public static T Init(T self, BishString tag)
    {
        Console.WriteLine($"Init({tag})");
        self.Tag = tag.Value;
        return self;
    }

    [Builtin("op")]
    public static BishInt Call(T self) => new(0);

    static T() => BuiltinBinder.Bind<T>();

    public override string ToString()
    {
        return $"T({Tag})";
    }
}