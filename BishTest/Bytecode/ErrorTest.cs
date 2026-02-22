namespace BishTest.Bytecode;

public class ErrorTest : Test
{
    public readonly BishScope Scope = BishScope.Globals();

    [Fact]
    public void TestThrow()
    {
        var frame = new BishFrame([
            // throw Error("error")
            new Bytecodes.String("error"),
            new Bytecodes.Get("Error"),
            new Bytecodes.Call(1),
            new Bytecodes.Throw()
        ], Scope);
        Action(() => frame.Execute()).Should().Excepts(BishError.StaticType).Which.Error.Message.Should().Be("error");
    }

    [Fact]
    public void TestTryThrownCatch()
    {
        var frame = new BishFrame([
            // try {
            new Bytecodes.TryStart("#"),
            // throw Error("error")
            new Bytecodes.String("error"),
            new Bytecodes.Get("Error"),
            new Bytecodes.Call(1),
            new Bytecodes.Throw(),
            // } catch (e) {
            new Bytecodes.TryEnd("#"),
            new Bytecodes.CatchStart("#"),
            // return e
            new Bytecodes.Ret(),
            // }
            new Bytecodes.CatchEnd("#")
        ], Scope);
        frame.Execute().Should().BeEquivalentTo(new BishError("error"));
    }

    [Fact]
    public void TestTryThrownFinally()
    {
        Scope.DefVar("x", new BishInt(0));
        var frame = new BishFrame([
            // try {
            new Bytecodes.TryStart("#"),
            // throw Error("error")
            new Bytecodes.String("error"),
            new Bytecodes.Get("Error"),
            new Bytecodes.Call(1),
            new Bytecodes.Throw(),
            // } finally {
            new Bytecodes.TryEnd("#"),
            new Bytecodes.FinallyStart("#"),
            // x = 1
            new Bytecodes.Int(1),
            new Bytecodes.Set("x"),
            // }
            new Bytecodes.FinallyEnd("#")
        ], Scope);
        Action(() => frame.Execute()).Should().Excepts(BishError.StaticType).Which.Error.Message.Should().Be("error");
        Scope.GetVar("x").Should().BeEquivalentTo(new BishInt(1));
    }

    [Fact]
    public void TestTryReturned()
    {
        Scope.DefVar("x", new BishInt(0));
        var frame = new BishFrame([
            // try {
            new Bytecodes.TryStart("#"),
            // return 0
            new Bytecodes.Int(0),
            new Bytecodes.Ret(),
            // } finally {
            new Bytecodes.TryEnd("#"),
            new Bytecodes.FinallyStart("#"),
            // x = 1
            new Bytecodes.Int(1),
            new Bytecodes.Set("x"),
            // }
            new Bytecodes.FinallyEnd("#")
        ], Scope);
        frame.Execute().Should().BeEquivalentTo(new BishInt(0));
        Scope.GetVar("x").Should().BeEquivalentTo(new BishInt(1));
    }

    [Fact]
    public void TestCatchThrown()
    {
        Scope.DefVar("x", new BishInt(0));
        var frame = new BishFrame([
            // try {
            new Bytecodes.TryStart("#"),
            // throw Error("error")
            new Bytecodes.String("error"),
            new Bytecodes.Get("Error"),
            new Bytecodes.Call(1),
            new Bytecodes.Throw(),
            // } catch {
            new Bytecodes.TryEnd("#"),
            new Bytecodes.CatchStart("#"),
            new Bytecodes.Pop(),
            // throw Error("other")
            new Bytecodes.String("other"),
            new Bytecodes.Get("Error"),
            new Bytecodes.Call(1),
            new Bytecodes.Throw(),
            // } finally {
            new Bytecodes.CatchEnd("#"),
            new Bytecodes.FinallyStart("#"),
            // x = 1
            new Bytecodes.Int(1),
            new Bytecodes.Set("x"),
            // }
            new Bytecodes.FinallyEnd("#")
        ], Scope);
        Action(() => frame.Execute()).Should().Excepts(BishError.StaticType).Which.Error.Message.Should().Be("other");
        Scope.GetVar("x").Should().BeEquivalentTo(new BishInt(1));
    }

    [Fact]
    public void TestCatchReturned()
    {
        Scope.DefVar("x", new BishInt(0));
        var frame = new BishFrame([
            // try {
            new Bytecodes.TryStart("#"),
            // throw Error("error")
            new Bytecodes.String("error"),
            new Bytecodes.Get("Error"),
            new Bytecodes.Call(1),
            new Bytecodes.Throw(),
            // } catch {
            new Bytecodes.TryEnd("#"),
            new Bytecodes.CatchStart("#"),
            new Bytecodes.Pop(),
            // return 0
            new Bytecodes.Int(0),
            new Bytecodes.Ret(),
            // } finally {
            new Bytecodes.CatchEnd("#"),
            new Bytecodes.FinallyStart("#"),
            // x = 1
            new Bytecodes.Int(1),
            new Bytecodes.Set("x"),
            // }
            new Bytecodes.FinallyEnd("#")
        ], Scope);
        frame.Execute().Should().BeEquivalentTo(new BishInt(0));
        Scope.GetVar("x").Should().BeEquivalentTo(new BishInt(1));
    }

    [Fact]
    public void TestFinallyThrown()
    {
        var frame = new BishFrame([
            // try {
            new Bytecodes.TryStart("#"),
            // throw Error("error")
            new Bytecodes.String("error"),
            new Bytecodes.Get("Error"),
            new Bytecodes.Call(1),
            new Bytecodes.Throw(),
            // } finally {
            new Bytecodes.TryEnd("#"),
            new Bytecodes.FinallyStart("#"),
            // throw Error("other")
            new Bytecodes.String("other"),
            new Bytecodes.Get("Error"),
            new Bytecodes.Call(1),
            new Bytecodes.Throw(),
            // }
            new Bytecodes.FinallyEnd("#")
        ], Scope);
        Action(() => frame.Execute()).Should().Excepts(BishError.StaticType).Which.Error.Message.Should().Be("other");
    }

    [Fact]
    public void TestFinallyReturned()
    {
        var frame = new BishFrame([
            // try {
            new Bytecodes.TryStart("#"),
            // throw Error("error")
            new Bytecodes.String("error"),
            new Bytecodes.Get("Error"),
            new Bytecodes.Call(1),
            new Bytecodes.Throw(),
            // } finally {
            new Bytecodes.TryEnd("#"),
            new Bytecodes.FinallyStart("#"),
            // return 0
            new Bytecodes.Int(0),
            new Bytecodes.Ret(),
            // }
            new Bytecodes.FinallyEnd("#")
        ], Scope);
        Action(() => frame.Execute()).Should().Excepts(BishError.StaticType).Which.Error.Message.Should().Be("error");
    }

    // TODO: tests on returning and throwing within try-catch-finally blocks
}