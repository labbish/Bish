namespace BishTest.Compiler;

public class CommentTest(OptimizeInfoFixture fixture) : CompilerTest(fixture)
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