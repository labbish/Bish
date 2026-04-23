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
        ExpectResult("c.vars['a']", "1");
        ExpectResult("c.vars['b']", "1");
        Execute("c.vars['a']=2;del c.vars['b'];c.vars['c']:=3;");
        ExpectResult("c.a", "2");
        ExpectError("c.b;", BishError.AttributeErrorType);
        ExpectResult("c.c", "3");
        ExpectTrue("c.type===C");
        Execute("c.type=D;");
        ExpectResult("c.f()", "-1");
    }

    [Fact]
    public void TestScopeReflect()
    {
        Execute("s:=this;f:=()'f';g:=()'g';h:=()'h';");
        ExpectResult("s.outer.outer", "null");
        Execute("s1:=null;{s1=this;}");
        ExpectTrue("s1.outer===s");
        ExpectTrue("s.vars['f']===f");
        Execute("s.vars['f']=g;del s.vars['h'];");
        ExpectResult("f()", "'g'");
        ExpectError("h();", BishError.AttributeErrorType);
    }
}