namespace BishTest.Lib;

public class FuncTest(TestInfoFixture fixture) : LibTest(fixture, "func", ["Func", "Arg"])
{
    [Fact]
    public void TestDefault()
    {
        // func f(x,y:1,z:0)=>x*y-z;
        Execute("f:=Func('f',[Arg('x'),Arg('y').default(1),Arg('z').default(0)],([x,y,z])=>x*y-z);");
        ExpectResult("f(3,2,1)", I(5));
        ExpectResult("f(3,2)", I(6));
        ExpectResult("f(3)", I(3));
    }

    [Fact]
    public void TestRest()
    {
        // func f(..rest)=>rest;
        Execute("f:=Func('f',[Arg('rest').rest()],([rest])=>rest);");
        ExpectResult("f(1,2,3)", L(I(1), I(2), I(3)));
    }

    [Fact]
    public void TestBind()
    {
        Execute("func f(a,b,c)=>a*b*c;");
        ExpectResult("f(2,3,4)", I(24));
        ExpectResult("f.bind(2)(3,4)", I(24));
        ExpectResult("f.bind(2,3)(4)", I(24));
    }
}