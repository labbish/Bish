namespace BishTest.Bytecode;

public class FuncTest : Test
{
    public BishScope Scope = new();
    
    [Fact]
    public void TestFunc1()
    {
        var frame = new BishFrame([
            // a := 1
            new BishBytecodeInt(1),
            new BishBytecodeDef("a"),
            
            // f := x => x - a
            new BishBytecodeFuncStart("f", ["x"]),
            new BishBytecodeInner(),
            // new BishBytecodeSet("x"),
            // new BishBytecodeGet("x"),
            new BishBytecodeGet("a"),
            new BishBytecodeOp("op_Sub", 2),
            new BishBytecodeRet(),
            new BishBytecodeOuter(),
            new BishBytecodeFuncEnd("f"),
            new BishBytecodeMakeFunc("f"),
            new BishBytecodeDef("f"),
            
            // x1 := f(3)
            new BishBytecodeGet("f"),
            new BishBytecodeInt(3),
            new BishBytecodeCall(1),
            new BishBytecodeDef("x1"),
            
            // a = 2
            new BishBytecodeInt(2),
            new BishBytecodeSet("a"),
            
            // x2 := f(5)
            new BishBytecodeGet("f"),
            new BishBytecodeInt(5),
            new BishBytecodeCall(1),
            new BishBytecodeDef("x2")
        ], Scope);
        frame.Execute();
        Scope.GetVar("x1").Should().BeEquivalentTo(new BishInt(2));
        Scope.GetVar("x2").Should().BeEquivalentTo(new BishInt(3));
    }
    
    [Fact]
    public void TestFunc2()
    {
        var frame = new BishFrame([
            // f := () => {...}
            new BishBytecodeFuncStart("f", []),
            new BishBytecodeInner(),
            // x := 2
            new BishBytecodeInt(2),
            new BishBytecodeDef("x"),
            // x = x + 1
            new BishBytecodeGet("x"),
            new BishBytecodeInt(1),
            new BishBytecodeOp("op_Add", 2),
            new BishBytecodeSet("x"),
            // return x
            new BishBytecodeGet("x"),
            new BishBytecodeRet(),
            
            new BishBytecodeOuter(),
            new BishBytecodeFuncEnd("f"),
            new BishBytecodeMakeFunc("f"),
            new BishBytecodeDef("f"),
            
            // x1 := f()
            new BishBytecodeGet("f"),
            new BishBytecodeCall(0),
            new BishBytecodeDef("x1"),
            
            // x2 := f()
            new BishBytecodeGet("f"),
            new BishBytecodeCall(0),
            new BishBytecodeDef("x2")
        ], Scope);
        frame.Execute();
        Scope.GetVar("x1").Should().BeEquivalentTo(new BishInt(3));
        Scope.GetVar("x2").Should().BeEquivalentTo(new BishInt(3));
    }
    
    [Fact]
    public void TestFuncRecursive()
    {
        var frame = new BishFrame([
            // f := () => {...}
            new BishBytecodeFuncStart("f", ["n"]),
            new BishBytecodeInner(),
            new BishBytecodeDef("n"),
            // if (n <= 0)
            new BishBytecodeGet("n"),
            new BishBytecodeInt(0),
            new BishBytecodeOp("op_Le", 2),
            new BishBytecodeJumpIfNot("tag"),
            // return 1
            new BishBytecodeInt(1),
            new BishBytecodeRet(),
            // return f(n - 1) * n
            new BishBytecodeGet("n").Tagged("tag"),
            new BishBytecodeInt(1),
            new BishBytecodeOp("op_Sub", 2),
            new BishBytecodeGet("f"),
            new BishBytecodeSwap(),
            new BishBytecodeCall(1),
            new BishBytecodeGet("n"),
            new BishBytecodeOp("op_Mul", 2),
            new BishBytecodeRet(),
            
            new BishBytecodeOuter(),
            new BishBytecodeFuncEnd("f"),
            new BishBytecodeMakeFunc("f"),
            new BishBytecodeDef("f"),
            
            // f(4)
            new BishBytecodeGet("f"),
            new BishBytecodeInt(4),
            new BishBytecodeCall(1)
        ], Scope);
        frame.Execute();
        frame.Stack.Pop().Should().BeEquivalentTo(new BishInt(24));
    }
}