namespace BishTest.Lib;

public class FormatTest(TestInfoFixture fixture) : Test(fixture)
{
    [Fact]
    public void TestFormat()
    {
        ExpectResult("'{},{1:?},{},{:?},{0}'.format('a','b','c')", "\"a,'b',b,'c',a\"");
        ExpectResult("'{:%<4},{:%>4},{:%^4}'.format(1,2,3)", "'1%%%,%%%2,%3%%'");
        ExpectResult("'{:04}'.format(1)", "'0001'");
        ExpectResult("'{:+},{:+}'.format(1,-1)", "'+1,-1'");
        ExpectResult("pi:=3.1415926;'{:.0},{:.2},{:.4}'.format(pi,pi,pi)", "'3,3.14,3.1416'");
        ExpectResult("'{:.3},{:.3}'.format('x','abcde')", "'x,abc'");
        ExpectResult("'{:b},{:o},{:x},{:X}'.format(0b10,0o67,0xf5,0xF5)", "'10,67,f5,F5'");
        ExpectResult("'{:.1e},{:.1E}'.format(1.2e10,1.2e10)", "'1.2e+010,1.2E+010'");
        ExpectResult("'{:#b},{:#o},{:#x}'.format(0b11,0o11,0x11)", "'0b11,0o11,0x11'");
        ExpectResult("'p{1:-^+#12.1X}q'.format(0, 114)", "'p---0x+72----q'");
        ExpectResult("'{}{{}}{}'.format(1,2)", "'1{}2'");
    }

    [Fact]
    public void TestFormatEval()
    {
        ExpectResult("x:=3;'{},{1},{#1},{x},{x+1}'.formatEval(0,2)", "'0,1,2,3,4'");
    }
}