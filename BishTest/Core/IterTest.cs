namespace BishTest.Core;

public class IterTest(TestInfoFixture fixture) : Test(fixture)
{
    [Fact]
    public void TestIter()
    {
        Execute("s:='';x:='abc';for(c:x)s=c+s;");
        ExpectResult("x", "'abc'");
        ExpectResult("s", "'cba'");

        ExpectResult("range(5).toList()", "[0,1,2,3,4]");
        ExpectResult("range(5).entries.toList()", "[[0,0],[1,1],[2,2],[3,3],[4,4]]");
        ExpectResult("range(5).map((x)x*2).toList()", "[0,2,4,6,8]");
        ExpectResult("range(5).filter((x)x%2==0).toList()", "[0,2,4]");
        ExpectResult("range(5).take(3).toList()", "[0,1,2]");
        ExpectResult("range(5).skip(2).toList()", "[2,3,4]");
        ExpectResult("range(5).flatMap((x)[x,x*2]).toList()", "[0,0,1,2,2,4,3,6,4,8]");
        ExpectResult("range(5).reduce((x,y)x+y)", "10");
        ExpectResult("range(5).reduce((x,y)x+y,1)", "11");
        Execute("s:=0;range(5).foreach((x)s+=x);");
        ExpectResult("s", "10");
        ExpectTrue("range(5).all((x)x>=0)");
        ExpectFalse("range(5).all((x)x>0)");
        ExpectTrue("range(5).any((x)x<=0)");
        ExpectFalse("range(5).any((x)x<0)");
        ExpectTrue("[true,false].iter().any()");
        ExpectFalse("[true,false].iter().all()");
        ExpectResult("range(5).find((x)x==3)", "3");
        ExpectResult("range(5).find((x)x==-1)", "null");
        ExpectTrue("range(5).contains(3)");
        ExpectFalse("range(5).contains(-1)");
        ExpectResult("range(5).join()", "'01234'");
        ExpectResult("range(5).join(',')", "'0,1,2,3,4'");
        ExpectResult("range(5).concat(range(2), range(3)).toList()", "[0,1,2,3,4,0,1,0,1,2]");
        ExpectResult("[..range(5), ..range(2), ..range(3)]", "[0,1,2,3,4,0,1,0,1,2]");
    }
}