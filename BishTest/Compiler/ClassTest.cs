// ReSharper disable InconsistentNaming

namespace BishTest.Compiler;

[Collection("opt")]
public class ClassTest(OptimizeInfoFixture fixture) : CompilerTest(fixture)
{
    [Fact]
    public void TestClass()
    {
        Execute("class C{name:='C';func hook_Init(self)=>self.x='c';func f(self,s)=>self.x+s;};");
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
        Execute("class C{name:='C';func hook_Init(self)=>self.x='c';func f(self,s)=>self.x+s;};");
        Execute("class D:C{name:='D';func hook_Init(self)=>self.x='d';func f(self,s)=>self.base().f(s)+'?';};");
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
        Execute("class C{name:='C';func hook_Init(self)=>self.x='c';func f(self,s)=>self.x+s;};");
        Execute("class D:..[C]{name:='D';func hook_Init(self)=>self.x='d';func f(self,s)=>self.base().f(s)+'?';};");
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
        Execute("func d(cls){cls.name='D';init:=cls.hook_Init;cls.hook_Init=(self)=>{init(self);self.y='d';};return cls;};");
        Execute("@d class C{name:='C';func hook_Init(self)=>self.x='c';func f(self,s)=>self.x+s;};");
        Execute("c:=C();");
        BishObject C = Scope.GetVar("C"), c = Scope.GetVar("c");
        C.GetMember("name").Should().BeEquivalentTo(S("D"));
        c.GetMember("x").Should().BeEquivalentTo(S("c"));
        c.GetMember("y").Should().BeEquivalentTo(S("d"));
    }
}