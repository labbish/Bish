namespace BishTest.Core;

public class FuncTest(TestInfoFixture fixture) : Test(fixture)
{
    [Fact]
    public void TestFunc()
    {
        Execute("x:=((x)=>x+1)(3);");
        Scope.GetVar("x").Should().BeEquivalentTo(I(4));
        Execute("x:=((x){return x+1;})(3);");
        Scope.GetVar("x").Should().BeEquivalentTo(I(4));
        Execute("func f(x)=>x+1;x:=f(3);");
        Scope.GetVar("x").Should().BeEquivalentTo(I(4));
        Execute("func f(x){return x+1;};x:=f(3);");
        Scope.GetVar("x").Should().BeEquivalentTo(I(4));
        Action(() => Compile("func f(x,y,x)=>null;")).Should().Throw();
    }

    [Fact]
    public void TestDefault()
    {
        Execute("func f(x,y:1,z:0)=>x*y-z;");
        ExpectResult("f(3,2,1)", I(5));
        ExpectResult("f(3,2)", I(6));
        ExpectResult("f(3)", I(3));
        Action(() => Compile("func f(x=0,y,z=0)=>null;")).Should().Throw();
    }

    [Fact]
    public void TestRest()
    {
        Execute("func f(..rest)=>rest;");
        ExpectResult("f(1,2,3)", L(I(1), I(2), I(3)));
        Action(() => Compile("func f(x,y=0,..z)=>null;")).Should().Throw();
        Action(() => Compile("func f(x,y,..z=0)=>null;")).Should().Throw();
        Action(() => Compile("func f(x,..y,..z)=>null;")).Should().Throw();
    }

    [Fact]
    public void TestScope()
    {
        Execute("a:=1;f:=(x)=>x-a;x1:=f(3);a=2;x2:=f(5);");
        Scope.GetVar("x1").Should().BeEquivalentTo(I(2));
        Scope.GetVar("x2").Should().BeEquivalentTo(I(3));
    }

    [Fact]
    public void TestRecursive()
    {
        Execute("func f(n)=>n<=0?1:n*f(n-1);");
        ExpectResult("f(4)", I(24));
    }

    [Fact]
    public void TestClosure()
    {
        Execute("counter:=(){count:=0;add:=()=>count+=1;return add;};c1:=counter();c2:=counter();");
        ExpectResult("c1()", I(1));
        Execute("c2();");
        ExpectResult("c2()", I(2));
    }

    [Fact]
    public void TestDecorator()
    {
        Execute("func d1(f)=>(x)=>f(x*2);func d2(f)=>(x)=>f(x)+1;");
        ExpectResult("(@d1 @d2 (x)=>x)(3)", I(7));
    }

    [Fact]
    public void TestYield()
    {
        Action(() => Execute("yield 0;")).Should().Excepts(BishError.YieldValueType);
        Execute("func square(it)*{for(i:it)yield i^2;};");
        Execute("func add(it1, it2)*{yield* it1;yield* it2;};");
        Execute("l:=list(range(5));l=list(add(l,square(l)));");
        ExpectResult("l", L(I(0), I(1), I(2), I(3), I(4), I(0), I(1), I(4), I(9), I(16)));
    }

    [Fact]
    public void TestDeconstruct()
    {
        ExpectResult("(([a,..b])=>a*b)([3,2,1])", L(I(2), I(1), I(2), I(1), I(2), I(1)));
    }

    [Fact]
    public void TestFuncType()
    {
        Execute("Arg:=Func.Arg;");

        // func f(x,y:1,z:0)=>x*y-z;
        Execute("f:=Func('f',[Arg('x'),Arg('y').default(1),Arg('z').default(0)],([x,y,z])=>x*y-z);");
        ExpectResult("f(3,2,1)", I(5));
        ExpectResult("f(3,2)", I(6));
        ExpectResult("f(3)", I(3));

        // func f(..rest)=>rest;
        Execute("f:=Func('f',[Arg('rest').rest()],([rest])=>rest);");
        ExpectResult("f(1,2,3)", L(I(1), I(2), I(3)));

        Execute("func f(a,b,c)=>a*b*c;");
        ExpectResult("f(2,3,4)", I(24));
        ExpectResult("f.bind(2)(3,4)", I(24));
        ExpectResult("f.bind(2,3)(4)", I(24));
    }
}