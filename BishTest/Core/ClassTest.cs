namespace BishTest.Core;

public class ClassTest(TestInfoFixture fixture) : Test(fixture)
{
    [Fact]
    public void TestClass()
    {
        Execute("class C{name:='C';init(self)self.x:='c';func f(self,s)self.x+s;};");
        Execute("c:=C();");
        ExpectResult("C.name", "'C'");
        ExpectResult("c.x", "'c'");
        ExpectTrue("c.type===C");
        ExpectResult("c.f('s')", "'cs'");
    }

    [Fact]
    public void TestInherit()
    {
        Execute("class C{name:='C';init(self)self.x:='c';func f(self,s)self.x+s;};");
        Execute("class D:C{name:='D';init(self)self.x:='d';func f(self,s)self.base().f(s)+'?';};");
        Execute("d:=D();");
        ExpectResult("D.name", "'D'");
        ExpectResult("d.x", "'d'");
        ExpectTrue("d.type===D");
        ExpectResult("d.f('s')", "'ds?'");
    }

    [Fact]
    public void TestRest()
    {
        Execute("class C{name:='C';init(self)self.x:='c';func f(self,s)self.x+s;};");
        Execute("class D:..[C]{name:='D';init(self)self.x:='d';func f(self,s)self.base().f(s)+'?';};");
        Execute("d:=D();");
        ExpectResult("D.name", "'D'");
        ExpectResult("d.x", "'d'");
        ExpectTrue("d.type===D");
        ExpectResult("d.f('s')", "'ds?'");
    }

    [Fact]
    public void TestDecorator()
    {
        Execute("func d(cls){cls.name='D';orig:=cls.hook_init;cls.hook_init=(self){orig(self);self.y:='d';};cls};");
        Execute("@d class C{name:='C';init(self)self.x:='c';func f(self,s)self.x+s;};");
        Execute("c:=C();");
        ExpectResult("C.name", "'D'");
        ExpectResult("c.x", "'c'");
        ExpectResult("c.y", "'d'");
    }

    [Fact]
    public void TestOperator()
    {
        Execute("class C{init(self,x)self.x:=x;oper+(a,b)C(a.x*b.x);oper+(a)C(-a.x);};");
        ExpectResult("(C(3)+C(2)).x", "6");
        ExpectResult("(C(3)+C('a')).x", "'aaa'");
        ExpectResult("(+C(2.5)).x", "-2.5");

        Execute("class C{init(self,x)self._x:=x;get x(self)self._x;set x(self,x)self._x=x;"
                + "def x(self,x)self._x:=x;del x(self)del self._x;};c:=C(5);");
        ExpectResult("c.x", "5");
        ExpectResult("c.x:=3", "3");
        ExpectResult("c.x", "3");
        ExpectResult("c.x=6", "6");
        ExpectResult("c.x", "6");
        Execute("del c.x;");
        ExpectError("c.x;", BishError.AttributeErrorType);

        Execute("class C{init(self,x)self._x:=x;get [](self,_)self._x;"
                + "set [](self,_,x)self._x:=x;del [](self,_)del self._x;};c:=C(5);");
        ExpectResult("c[0]", "5");
        ExpectResult("c[1]=3", "3");
        ExpectResult("c[2]", "3");
        Execute("del c[3];");
        ExpectError("c[4];", BishError.ArgumentErrorType);

        Execute("class C{init(self,x)self.base()._x:=x;get(self,_)self.base()._x;"
                + "set(self,_,x)self.base()._x:=x;del(self,_)del self.base()._x;};c:=C(5);");
        ExpectResult("c.x0", "5");
        ExpectResult("c.x1=3", "3");
        ExpectResult("c.x2", "3");
        Execute("del c.x3;");
        ExpectError("c.x4;", BishError.AttributeErrorType);

        Execute("class C{instance:=null;create(self)instance??=self;};");
        ExpectTrue("C()===C()");
    }

    [Fact]
    public void TestExtend()
    {
        ExpectResult("class C{a:=0};C.a", "0");
        ExpectResult("extend C{b:=1};C.b", "1");
    }
}