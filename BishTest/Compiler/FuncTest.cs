namespace BishTest.Compiler;

public class FuncTest : CompilerTest
{
    [Fact]
    public void TestFunc()
    {
        Execute("x:=((x)=>x+1)(3);");
        Scope.GetVar("x").Should().BeEquivalentTo(I(4));
        Execute("x:=((x)=>{return x+1;})(3);");
        Scope.GetVar("x").Should().BeEquivalentTo(I(4));
        Execute("func f(x)=>x+1;x:=f(3);");
        Scope.GetVar("x").Should().BeEquivalentTo(I(4));
        Execute("func f(x){return x+1;};x:=f(3);");
        Scope.GetVar("x").Should().BeEquivalentTo(I(4));
        Action(() => Execute("func f(x,y,x)=>null;")).Should().Excepts(BishError.ArgumentErrorType);
    }

    [Fact]
    public void TestDefault()
    {
        Execute("func f(x,y=1,z=0)=>x*y-z;");
        ExpectResult("f(3,2,1)", I(5));
        ExpectResult("f(3,2)", I(6));
        ExpectResult("f(3)", I(3));
        Action(() => Execute("func f(x=0,y,z=0)=>null;")).Should().Excepts(BishError.ArgumentErrorType);
    }

    [Fact]
    public void TestRest()
    {
        Execute("func f(..rest)=>rest;");
        ExpectResult("f(1,2,3)", L(I(1), I(2), I(3)));
        Action(() => Execute("func f(x,y=0,..z)=>null;")).Should().Excepts(BishError.ArgumentErrorType);
        Action(() => Execute("func f(x,y,..z=0)=>null;")).Should().Excepts(BishError.ArgumentErrorType);
        Action(() => Execute("func f(x,..y,..z)=>null;")).Should().Excepts(BishError.ArgumentErrorType);
    }

    [Fact]
    public void TestRecursive()
    {
        Execute("func f(n) => n <= 0 ? 1 : n * f(n - 1);");
        ExpectResult("f(4)", I(24));
    }

    [Fact]
    public void TestDecorator()
    {
        Execute("func d1(f)=>(x)=>f(x*2);func d2(f)=>(x)=>f(x)+1;");
        ExpectResult("(@d1 @d2 (x)=>x)(3)", I(7));
    }
}