namespace BishTest.Compiler;

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
        Execute("x:=s:=0;try throw Error('error');catch(e)=>x=e;finally{s=1;}");
        Scope.GetVar("x").Should().BeOfType<BishError>().Which.Message.Should().Be("error");
        Scope.GetVar("s").Should().BeEquivalentTo(I(1));
    }

    [Fact]
    public void TestWhen()
    {
        Action(() => Execute("try{e:=Error('error');e.data=-1;throw e;}catch(e)when(e.data>0){}"))
            .Should().Excepts(BishError.StaticType).Which.Error.Message.Should().Be("error");
        Execute("try{e:=Error('error');e.data=1;throw e;}catch(e)when(e.data>0){}");
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