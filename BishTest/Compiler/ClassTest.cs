// ReSharper disable InconsistentNaming

namespace BishTest.Compiler;

[Collection("opt")]
public class ClassTest(OptimizeInfoFixture fixture) : CompilerTest(fixture)
{
    [Fact]
    public void TestClass()
    {
        Execute("class C{name:='C';init(self)=>self.x='c';func f(self,s)=>self.x+s;};");
        Execute("c:=C();");
        BishObject C = Scope.GetVar("C"), c = Scope.GetVar("c");
        C.GetMember("name").Should().BeEquivalentTo(S("C"));
        c.GetMember("x").Should().BeEquivalentTo(S("c"));
        c.Type.Should().Be(C);
        c.GetMember("f").Call([S("s")]).Should().BeEquivalentTo(S("cs"));
    }

    [Fact]
    public void TestInherit()
    {
        Execute("class C{name:='C';init(self)=>self.x='c';func f(self,s)=>self.x+s;};");
        Execute("class D:C{name:='D';init(self)=>self.x='d';func f(self,s)=>self.base().f(s)+'?';};");
        Execute("d:=D();");
        BishObject D = Scope.GetVar("D"), d = Scope.GetVar("d");
        D.GetMember("name").Should().BeEquivalentTo(S("D"));
        d.GetMember("x").Should().BeEquivalentTo(S("d"));
        d.Type.Should().Be(D);
        d.GetMember("f").Call([S("s")]).Should().BeEquivalentTo(S("ds?"));
    }

    [Fact]
    public void TestRest()
    {
        Execute("class C{name:='C';init(self)=>self.x='c';func f(self,s)=>self.x+s;};");
        Execute("class D:..[C]{name:='D';init(self)=>self.x='d';func f(self,s)=>self.base().f(s)+'?';};");
        Execute("d:=D();");
        BishObject D = Scope.GetVar("D"), d = Scope.GetVar("d");
        D.GetMember("name").Should().BeEquivalentTo(S("D"));
        d.GetMember("x").Should().BeEquivalentTo(S("d"));
        d.Type.Should().Be(D);
        d.GetMember("f").Call([S("s")]).Should().BeEquivalentTo(S("ds?"));
    }

    [Fact]
    public void TestDecorator()
    {
        Execute(
            "func d(cls){cls.name='D';orig:=cls.hook_init;cls.hook_init=(self){orig(self);self.y='d';};return cls;};");
        Execute("@d class C{name:='C';init(self)=>self.x='c';func f(self,s)=>self.x+s;};");
        Execute("c:=C();");
        BishObject C = Scope.GetVar("C"), c = Scope.GetVar("c");
        C.GetMember("name").Should().BeEquivalentTo(S("D"));
        c.GetMember("x").Should().BeEquivalentTo(S("c"));
        c.GetMember("y").Should().BeEquivalentTo(S("d"));
    }

    [Fact]
    public void TestOperator()
    {
        Execute("class C{init(self,x)=>self.x=x;oper+(a,b)=>C(a.x*b.x);oper+(a)=>C(-a.x);};");
        ExpectResult("(C(3)+C(2)).x", I(6));
        ExpectResult("(C(3)+C('a')).x", S("aaa"));
        ExpectResult("(+C(2.5)).x", N(-2.5));

        Execute("class C{init(self,x)=>self._x=x;get x(self)=>self._x;"
                + "set x(self,x)=>self._x=x;del x(self)=>del self._x;};c:=C(5);");
        ExpectResult("c.x", I(5));
        ExpectResult("c.x=3", I(3));
        ExpectResult("c.x", I(3));
        Execute("del c.x;");
        Action(() => Execute("c.x;")).Should().Excepts();

        Execute("class C{init(self,x)=>self._x=x;get [](self,_)=>self._x;"
                + "set [](self,_,x)=>self._x=x;del [](self,_)=>del self._x;};c:=C(5);");
        ExpectResult("c[0]", I(5));
        ExpectResult("c[1]=3", I(3));
        ExpectResult("c[2]", I(3));
        Execute("del c[3];");
        Action(() => Execute("c[4];")).Should().Excepts();

        Execute("class C{init(self,x)=>self.base()._x=x;get(self,_)=>self.base()._x;"
                + "set(self,_,x)=>self.base()._x=x;del(self,_)=>del self.base()._x;};c:=C(5);");
        ExpectResult("c.x0", I(5));
        ExpectResult("c.x1=3", I(3));
        ExpectResult("c.x2", I(3));
        Execute("del c.x3;");
        Action(() => Execute("c.x4;")).Should().Excepts();

        Execute("class C{instance:=null;create(self)=>instance??=self;};");
        ExpectResult("C()===C()", B(true));
    }
}