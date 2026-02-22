namespace BishTest.Bytecode;

public class ClassTest : Test
{
    public readonly BishScope Scope = BishScope.Globals();

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

    [Fact]
    public void TestClassInherit()
    {
        var t1 = new BishType("T1");
        var t2 = new BishType("T2");
        Scope.DefVar("T1", t1);
        Scope.DefVar("T2", t2);
        var frame = new BishFrame([
            new Bytecodes.ClassStart("C"),
            new Bytecodes.ClassEnd("C"),
            new Bytecodes.Get("T1"),
            new Bytecodes.Get("T2"),
            new Bytecodes.MakeClass("C", 2),
            new Bytecodes.Def("C")
        ], Scope);
        frame.Execute();
        Scope.GetVar("C").Should().BeOfType<BishType>().Which.Parents
            .Should().Equal(t1, t2, BishObject.StaticType);
    }
}