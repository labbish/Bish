namespace BishTest.Compiler;

public class StringTest(OptimizeInfoFixture fixture) : CompilerTest(fixture)
{
    [Fact]
    public void TestString()
    {
        ExpectResult("""  'a\tb"\''  """, S("a\tb\"'"));
        ExpectResult("""  "a\tb\"'"  """, S("a\tb\"'"));
        ExpectResult("""  r'a\tb'  """, S("""a\tb"""));
        ExpectResult("""  r"a\tb"  """, S("""a\tb"""));
        ExpectResult("""  r#'a#\tb"''#  """, S("""a#\tb"'"""));
        ExpectResult("""  r#"a#\tb"'"#  """, S("""a#\tb"'"""));
    }
}