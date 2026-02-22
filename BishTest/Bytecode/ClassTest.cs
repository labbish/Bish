namespace BishTest.Bytecode;

public class ClassTest : Test
{
    public readonly BishScope Scope = new();

    [Fact]
    public void TestClass()
    {
        Scope.DefVar("a", new BishInt(1));
        var frame = new BishFrame([
            // class C {
            new Bytecodes.ClassStart("C"),
            // a := a
            new Bytecodes.Get("a"),
            new Bytecodes.Def("a"),
            // f = self => {
            new Bytecodes.FuncStart("f", ["self"]),
            new Bytecodes.Inner(),
            // return self.x
            new Bytecodes.GetMember("x"),
            new Bytecodes.Ret(),
            // } (end f)
            new Bytecodes.Outer(),
            new Bytecodes.FuncEnd("f"),
            new Bytecodes.MakeFunc("f"),
            new Bytecodes.Def("f"),
            // hook_Init = (self, x) => {
            new Bytecodes.FuncStart("init", ["self", "x"]),
            new Bytecodes.Inner(),
            // self.x = x
            new Bytecodes.SetMember("x"),
            // } (end f)
            new Bytecodes.Outer(),
            new Bytecodes.FuncEnd("init"),
            new Bytecodes.MakeFunc("init"),
            new Bytecodes.Def("hook_Init"),
            // } (end C)
            new Bytecodes.ClassEnd("C"),
            new Bytecodes.MakeClass("C"),
            new Bytecodes.Def("C"),

            // c := C(1)
            new Bytecodes.Int(1),
            new Bytecodes.Get("C"),
            new Bytecodes.Call(1),
            new Bytecodes.Def("c"),
            // x := c.f()
            new Bytecodes.Get("c"),
            new Bytecodes.GetMember("f"),
            new Bytecodes.Call(0),
            new Bytecodes.Def("x")
        ], Scope);
        frame.Execute();
        Scope.GetVar("c").GetMember("x").Should().BeEquivalentTo(new BishInt(1));
        Scope.GetVar("x").Should().BeEquivalentTo(new BishInt(1));
    }
}