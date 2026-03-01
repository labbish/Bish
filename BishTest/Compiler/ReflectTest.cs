namespace BishTest.Compiler;

public class ReflectTest : CompilerTest
{
    public ReflectTest(OptimizeInfoFixture fixture) : base(fixture)
    {
        Execute("class B{a:=3;};");
        Execute("class C:B{a:=0;init(self)=>self.b=0;func f(self)=>self.a;};");
        Execute("class D{func f(self)=>-1;o:='d';};");
        Execute("c:=C();c.a=c.b=1;");
    }

    [Fact]
    public void TestObjectReflect()
    {
        Execute("r:=reflect(c);");
        ExpectResult("r.object===c", B(true));
        ExpectResult("r.members['a']", I(1));
        ExpectResult("r.members['b']", I(1));
        Execute("r.members['a']=2;del r.members['b'];");
        ExpectResult("c.a", I(2));
        Action(() => Execute("c.b;")).Should().Excepts();
        ExpectResult("r.type===C", B(true));
        Execute("r.type=D;");
        ExpectResult("c.f()", I(-1));
    }
    
    [Fact]
    public void TestTypeReflect()
    {
        Execute("R:=reflect(C);");
        ExpectResult("R.parents==[B]", B(true));
        ExpectResult("R.MRO==[B,object]", B(true));
        Execute("R.parents[:]=[D];");
        ExpectResult("C.o", S("d"));
    }

    [Fact]
    public void TestScopeReflect()
    {
        Execute("s:=reflect();");
        ExpectResult("s.outer", Null);
        Execute("s1:=null;{s1=reflect();}");
        ExpectResult("s1.outer==s", B(true));
        ExpectResult("s.vars['int']===int", B(true));
        Execute("s.vars['int']=string;del s.vars['num'];");
        ExpectResult("int()", S(""));
        Action(() => Execute("num();")).Should().Excepts();
    }
}