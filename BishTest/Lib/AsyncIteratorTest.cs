namespace BishTest.Lib;

public class AsyncIteratorTest(TestInfoFixture fixture) : Test(fixture)
{
    [Fact]
    public void TestAsyncIter()
    {
        Execute("func asyncRange(..args) async* yield* range(..args);");
        
        ExpectResult("await asyncRange(5).toList()", "[0,1,2,3,4]");
        ExpectResult("await AsyncIterator.from(asyncRange(5)).toList()", "[0,1,2,3,4]");
        ExpectResult("await asyncRange(5).entries.toList()", "[[0,0],[1,1],[2,2],[3,3],[4,4]]");
        ExpectResult("await asyncRange(5).map((x)x*2).toList()", "[0,2,4,6,8]");
        ExpectResult("await asyncRange(5).filter((x)x%2==0).toList()", "[0,2,4]");
        ExpectResult("await asyncRange(5).take(3).toList()", "[0,1,2]");
        ExpectResult("await asyncRange(5).skip(2).toList()", "[2,3,4]");
        ExpectResult("await asyncRange(5).flatMap((x)[x,x*2]).toList()", "[0,0,1,2,2,4,3,6,4,8]");
        ExpectResult("await asyncRange(5).reduce((x,y)x+y)", "10");
        ExpectResult("await asyncRange(5).reduce((x,y)x+y,1)", "11");
        ExpectResult("s:=0;await asyncRange(5).foreach((x)s+=x);s", "10");
        ExpectTrue("await asyncRange(5).all((x)x>=0)");
        ExpectFalse("await asyncRange(5).all((x)x>0)");
        ExpectTrue("await asyncRange(5).any((x)x<=0)");
        ExpectFalse("await asyncRange(5).any((x)x<0)");
        ExpectTrue("await AsyncIterator.from([true,false].iter()).any()");
        ExpectFalse("await AsyncIterator.from([true,false].iter()).all()");
        ExpectResult("await asyncRange(5).find((x)x==3)", "3");
        ExpectResult("await asyncRange(5).find((x)x==-1)", "null");
        ExpectTrue("await asyncRange(5).contains(3)");
        ExpectFalse("await asyncRange(5).contains(-1)");
        ExpectResult("await asyncRange(5).join()", "'01234'");
        ExpectResult("await asyncRange(5).join(',')", "'0,1,2,3,4'");
        ExpectResult("await asyncRange(5).concat(asyncRange(2), asyncRange(3)).toList()", "[0,1,2,3,4,0,1,0,1,2]");
    }
}