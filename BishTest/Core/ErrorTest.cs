namespace BishTest.Core;

public class ErrorTest(TestInfoFixture fixture) : Test(fixture)
{
    [Fact]
    public void TestThrow()
    {
        Action(() => Execute("throw Error('error');")).Should().Excepts(BishError.StaticType)
            .Which.Error.Message.Should().Be("error");
    }

    [Fact]
    public void TestError()
    {
        Execute("x:=try throw Error('error');");
        ExpectResult("x.error.message", S("error"));
        ExpectResult("if(x is err e)e.message", S("error"));
        Action(() => Execute("x.y;")).Should().Excepts(BishError.StaticType);
        ExpectErrorResult("x?.y");
        ExpectResult("x??0", I(0));
    }

    [Fact]
    public void TestWith()
    {
        Scope.DefVar("W", WithTest.StaticType);
        Execute("with(W()){}");
        Execute("with(_:W()){throw Error('error');}");
        WithTest.Enters.Should().Be(2);
        WithTest.NormalExits.Should().Be(1);
        WithTest.ErrorExits.Should().Be(1);
    }
}

public class WithTest : BishObject
{
    public static int Enters { get; private set; }
    public static int NormalExits { get; private set; }
    public static int ErrorExits { get; private set; }
    
    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("bool");

    [Builtin("hook")]
    public static WithTest Create(BishObject _) => new();
    
    [Builtin("hook")]
    public static void Enter(WithTest _) => Enters++;

    [Builtin("hook")]
    public static BishBool Exit(WithTest _, BishObject error)
    {
        if (error is BishNull) NormalExits++;
        else ErrorExits++;
        return BishBool.True;
    }
    
    static WithTest() => BishBuiltinBinder.Bind<WithTest>();
}