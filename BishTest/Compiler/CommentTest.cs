namespace BishTest.Compiler;

public class CommentTest(TestInfoFixture fixture) : Test(fixture)
{
    [Fact]
    public void TestComment()
    {
        Execute("0;//comment");
        Execute("1;/*comment*/");
        ExpectResult("'//...'", S("//..."));
        ExpectResult("'/*...*/'", S("/*...*/"));
    }
}