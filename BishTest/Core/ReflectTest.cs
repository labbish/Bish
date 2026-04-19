namespace BishTest.Core;

public class ReflectTest : Test
{
    public ReflectTest(TestInfoFixture fixture) : base(fixture)
    {
        Execute("class B{a:=3;};");
        Execute("class C:B{a:=0;init(self)self.b:=0;func f(self)self.a;};");
        Execute("class D{func f(self)-1;o:='d';};");
        Execute("c:=C();c.a:=c.b:=1;");
    }

    [Fact]
    public void TestObjectReflect()
    {
        ExpectResult("c.vars['a']", I(1));
        ExpectResult("c.vars['b']", I(1));
        Execute("c.vars['a']=2;del c.vars['b'];c.vars['c']:=3;");
        ExpectResult("c.a", I(2));
        Action(() => Execute("c.b;")).Should().Excepts(BishError.AttributeErrorType);
        ExpectResult("c.c", I(3));
        ExpectResult("c.type===C", True);
        Execute("c.type=D;");
        ExpectResult("c.f()", I(-1));
    }

    [Fact]
    public void TestScopeReflect()
    {
        Execute("s:=this;f:=()'f';g:=()'g';h:=()'h';");
        ExpectResult("s.outer.outer", Null);
        Execute("s1:=null;{s1=this;}");
        ExpectResult("s1.outer===s", True);
        ExpectResult("s.vars['f']===f", True);
        Execute("s.vars['f']=g;del s.vars['h'];");
        ExpectResult("f()", S("g"));
        Action(() => Execute("h();")).Should().Excepts(BishError.AttributeErrorType);
    }
}