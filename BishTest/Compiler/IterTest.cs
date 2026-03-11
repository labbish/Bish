namespace BishTest.Compiler;

public class IterTest(TestInfoFixture fixture) : Test(fixture)
{
    [Fact]
    public void TestIter()
    {
        ExpectResult("range(5).toList()", L(I(0), I(1), I(2), I(3), I(4)));
        ExpectResult("range(5).entries.toList()",
            L(L(I(0), I(0)), L(I(1), I(1)), L(I(2), I(2)), L(I(3), I(3)), L(I(4), I(4))));
        ExpectResult("range(5).map((x)=>x*2).toList()", L(I(0), I(2), I(4), I(6), I(8)));
        ExpectResult("range(5).filter((x)=>x%2==0).toList()", L(I(0), I(2), I(4)));
        ExpectResult("range(5).take(3).toList()", L(I(0), I(1), I(2)));
        ExpectResult("range(5).skip(2).toList()", L(I(2), I(3), I(4)));
        ExpectResult("range(5).flatMap((x)=>[x,x*2]).toList()",
            L(I(0), I(0), I(1), I(2), I(2), I(4), I(3), I(6), I(4), I(8)));
        ExpectResult("range(5).reduce((x,y)=>x+y)", I(10));
        ExpectResult("range(5).reduce((x,y)=>x+y,1)", I(11));
        Execute("s:=0;range(5).foreach((x)=>s+=x);");
        ExpectResult("s", I(10));
        ExpectResult("range(5).all((x)=>x>=0)", True);
        ExpectResult("range(5).all((x)=>x>0)", False);
        ExpectResult("range(5).any((x)=>x<=0)", True);
        ExpectResult("range(5).any((x)=>x<0)", False);
        ExpectResult("[true,false].iter().any()", True);
        ExpectResult("[true,false].iter().all()", False);
        ExpectResult("range(5).find((x)=>x==3)", I(3));
        ExpectResult("range(5).find((x)=>x==-1)", Null);
        ExpectResult("range(5).contains(3)", True);
        ExpectResult("range(5).contains(-1)", False);
        ExpectResult("range(5).join()", S("01234"));
        ExpectResult("range(5).join(',')", S("0,1,2,3,4"));
        ExpectResult("range(5).concat(range(2), range(3)).toList()",
            L(I(0), I(1), I(2), I(3), I(4), I(0), I(1), I(0), I(1), I(2)));
        ExpectResult("[..range(5), ..range(2), ..range(3)]",
            L(I(0), I(1), I(2), I(3), I(4), I(0), I(1), I(0), I(1), I(2)));
    }
}