namespace BishTest.Core;

public class FuncTest(TestInfoFixture fixture) : Test(fixture)
{
    [Fact]
    public void TestFunc()
    {
        Execute("x:=((x)x+1)(3);");
        ExpectResult("x", "4");
        Execute("x:=((x){return x+1;})(3);");
        ExpectResult("x", "4");
        Execute("func f(x)x+1;x:=f(3);");
        ExpectResult("x", "4");
        Execute("func f(x){return x+1;};x:=f(3);");
        ExpectResult("x", "4");
        Execute("func f(){return};");
        ExpectResult("f()", "null");
        ExpectCompileError("func f(x,y,x)null;");
    }

    [Fact]
    public void TestDefault()
    {
        Execute("func f(x,y:1,z:0)x*y-z;");
        ExpectResult("f(3,2,1)", "5");
        ExpectResult("f(3,2)", "6");
        ExpectResult("f(3)", "3");
        ExpectCompileError("func f(x=0,y,z=0)null;");
    }

    [Fact]
    public void TestRest()
    {
        Execute("func f(..rest)rest;");
        ExpectResult("f(1,2,3)", "[1,2,3]");
        ExpectCompileError("func f(x,y=0,..z)null;");
        ExpectCompileError("func f(x,y,..z=0)null;");
        ExpectCompileError("func f(x,..y,..z)null;");
        Execute("func f(..rest){rest[0]:=0;rest[0]};");
        ExpectResult("f(1,2,3)", "0");
    }

    [Fact]
    public void TestScope()
    {
        Execute("a:=1;f:=(x)x-a;x1:=f(3);a=2;x2:=f(5);");
        ExpectResult("x1", "2");
        ExpectResult("x2", "3");
    }

    [Fact]
    public void TestRecursive()
    {
        Execute("func f(n)if(n<=0)1 else n*f(n-1);");
        ExpectResult("f(4)", "24");
    }

    [Fact]
    public void TestClosure()
    {
        Execute("counter:=(){count:=0;add:=()count+=1;return add;};c1:=counter();c2:=counter();");
        ExpectResult("c1()", "1");
        Execute("c2();");
        ExpectResult("c2()", "2");
    }

    [Fact]
    public void TestDecorator()
    {
        Execute("func d1(f)(x)f(x*2);func d2(f)(x)f(x)+1;");
        ExpectResult("(@d1 @d2 (x)x)(3)", "7");
    }

    [Fact]
    public void TestYield()
    {
        ExpectError("yield 0;", BishError.TypeErrorType);
        Execute("func square(it)*{for(i:it)yield i^2;};");
        Execute("func add(it1, it2)*{yield* it1;yield* it2;};");
        Execute("l:=list(range(5));l=list(add(l,square(l)));");
        ExpectResult("l", "[0,1,2,3,4,0,1,4,9,16]");
    }

    [Fact]
    public void TestGenerator()
    {
        Execute("func f()*{yield 1;yield 2;};g:=f();");
        ExpectResult("g.stage", "0");
        ExpectResult("g.next()", "1");
        ExpectResult("g.stage", "1");
        ExpectResult("g.next()", "2");
        ExpectResult("g.stage", "2");
        ExpectResult("g.next()", "IteratorStop");
        ExpectResult("g.stage", "-1");
    }

    [Fact]
    public void TestDeconstruct()
    {
        ExpectResult("(([a,..b])a*b)([3,2,1])", "[2,1,2,1,2,1]");
    }

    [Fact]
    public void TestFuncType()
    {
        Execute("Arg:=Func.Arg;");

        // func f(x,y:1,z:0)x*y-z;
        Execute("f:=Func('f',[Arg('x'),Arg('y').default(1),Arg('z').default(0)],([x,y,z])x*y-z);");
        ExpectResult("f(3,2,1)", "5");
        ExpectResult("f(3,2)", "6");
        ExpectResult("f(3)", "3");

        // func f(..rest)rest;
        Execute("f:=Func('f',[Arg('rest').rest()],([rest])rest);");
        ExpectResult("f(1,2,3)", "[1,2,3]");

        Execute("func f(a,b,c)a*b*c;");
        ExpectResult("f(2,3,4)", "24");
        ExpectResult("f.binds(2)(3,4)", "24");
        ExpectResult("f.binds(2,3)(4)", "24");
    }

    [Fact]
    public void TestBindHook()
    {
        Execute("class F{bind(self,_)0};");
        Execute("x:=F();class C{y:=x};c:=C();");
        ExpectResult("c.y", "0");
        ExpectTrue("C.y===x");
    }
}