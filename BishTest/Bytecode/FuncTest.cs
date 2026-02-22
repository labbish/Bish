namespace BishTest.Bytecode;

public class FuncTest : Test
{
    public BishScope Scope = new();

    [Fact]
    public void TestFunc()
    {
        var frame = new BishFrame([
            // a := 1
            new Bytecodes.Int(1),
            new Bytecodes.Def("a"),

            // f := x => x - a
            new Bytecodes.FuncStart("f", ["x"]),
            new Bytecodes.Inner(),
            // new Bytecodes.Set("x"),
            // new Bytecodes.Get("x"),
            new Bytecodes.Get("a"),
            new Bytecodes.Op("op_Sub", 2),
            new Bytecodes.Ret(),
            new Bytecodes.Outer(),
            new Bytecodes.FuncEnd("f"),
            new Bytecodes.MakeFunc("f"),
            new Bytecodes.Def("f"),

            // x1 := f(3)
            new Bytecodes.Int(3),
            new Bytecodes.Get("f"),
            new Bytecodes.Call(1),
            new Bytecodes.Def("x1"),

            // a = 2
            new Bytecodes.Int(2),
            new Bytecodes.Set("a"),

            // x2 := f(5)
            new Bytecodes.Int(5),
            new Bytecodes.Get("f"),
            new Bytecodes.Call(1),
            new Bytecodes.Def("x2")
        ], Scope);
        frame.Execute();
        Scope.GetVar("x1").Should().BeEquivalentTo(new BishInt(2));
        Scope.GetVar("x2").Should().BeEquivalentTo(new BishInt(3));
    }

    [Fact]
    public void TestFuncVar()
    {
        var frame = new BishFrame([
            // f := () => {...}
            new Bytecodes.FuncStart("f", []),
            new Bytecodes.Inner(),
            // x := 2
            new Bytecodes.Int(2),
            new Bytecodes.Def("x"),
            // x = x + 1
            new Bytecodes.Get("x"),
            new Bytecodes.Int(1),
            new Bytecodes.Op("op_Add", 2),
            new Bytecodes.Set("x"),
            // return x
            new Bytecodes.Get("x"),
            new Bytecodes.Ret(),

            new Bytecodes.Outer(),
            new Bytecodes.FuncEnd("f"),
            new Bytecodes.MakeFunc("f"),
            new Bytecodes.Def("f"),

            // x1 := f()
            new Bytecodes.Get("f"),
            new Bytecodes.Call(0),
            new Bytecodes.Def("x1"),

            // x2 := f()
            new Bytecodes.Get("f"),
            new Bytecodes.Call(0),
            new Bytecodes.Def("x2")
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
            new Bytecodes.FuncStart("f", ["n"]),
            new Bytecodes.Inner(),
            new Bytecodes.Def("n"),
            // if (n <= 0)
            new Bytecodes.Get("n"),
            new Bytecodes.Int(0),
            new Bytecodes.Op("op_Le", 2),
            new Bytecodes.JumpIfNot("tag"),
            // return 1
            new Bytecodes.Int(1),
            new Bytecodes.Ret(),
            // return f(n - 1) * n
            new Bytecodes.Get("n").Tagged("tag"),
            new Bytecodes.Int(1),
            new Bytecodes.Op("op_Sub", 2),
            new Bytecodes.Get("f"),
            new Bytecodes.Call(1),
            new Bytecodes.Get("n"),
            new Bytecodes.Op("op_Mul", 2),
            new Bytecodes.Ret(),

            new Bytecodes.Outer(),
            new Bytecodes.FuncEnd("f"),
            new Bytecodes.MakeFunc("f"),
            new Bytecodes.Def("f"),

            // ans := f(4)
            new Bytecodes.Int(4),
            new Bytecodes.Get("f"),
            new Bytecodes.Call(1),
            new Bytecodes.Def("ans")
        ], Scope);
        frame.Execute();
        Scope.GetVar("ans").Should().BeEquivalentTo(new BishInt(24));
    }

    [Fact]
    public void TestClosure()
    {
        var frame = new BishFrame([
            // counter := () => {...}
            new Bytecodes.FuncStart("counter", []),
            new Bytecodes.Inner(),

            // count := 0
            new Bytecodes.Int(0),
            new Bytecodes.Def("count"),

            // add := () => {...}
            new Bytecodes.FuncStart("add", []),
            new Bytecodes.Inner(),

            // count = count + 1
            new Bytecodes.Get("count"),
            new Bytecodes.Int(1),
            new Bytecodes.Op("op_Add", 2),
            new Bytecodes.Set("count"),

            // return count
            new Bytecodes.Get("count"),
            new Bytecodes.Ret(),

            new Bytecodes.Outer(),
            new Bytecodes.FuncEnd("add"),
            new Bytecodes.MakeFunc("add"),

            // return add
            new Bytecodes.Ret(),

            new Bytecodes.Outer(),
            new Bytecodes.FuncEnd("counter"),
            new Bytecodes.MakeFunc("counter"),
            new Bytecodes.Def("counter"),

            // c1 := counter()
            new Bytecodes.Get("counter"),
            new Bytecodes.Call(0),
            new Bytecodes.Def("c1"),

            // c2 := counter()
            new Bytecodes.Get("counter"),
            new Bytecodes.Call(0),
            new Bytecodes.Def("c2"),

            // x1 := c1()
            new Bytecodes.Get("c1"),
            new Bytecodes.Call(0),
            new Bytecodes.Def("x1"),

            // c2()
            new Bytecodes.Get("c2"),
            new Bytecodes.Call(0),
            new Bytecodes.Pop(), // just to empty the stack
            // x2 := c2()
            new Bytecodes.Get("c2"),
            new Bytecodes.Call(0),
            new Bytecodes.Def("x2")
        ], Scope);
        frame.Execute();
        Scope.GetVar("x1").Should().BeEquivalentTo(new BishInt(1));
        Scope.GetVar("x2").Should().BeEquivalentTo(new BishInt(2));
    }
}