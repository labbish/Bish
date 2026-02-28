namespace BishTest.Compiler;

public class LiteralTest(OptimizeInfoFixture fixture) : CompilerTest(fixture)
{
    [Fact]
    public void TestIntNum()
    {
        ExpectResult("123", I(123));
        ExpectResult("0xAb", I(0xAb));
        ExpectResult("0o35", I(035));
        ExpectResult("0b1101", I(0b1101));
        ExpectResult("1.5", N(1.5));
        ExpectResult("1.", N(1));
        ExpectResult(".5", N(.5));
        ExpectResult("1.5e2", N(1.5e2));
        ExpectResult("1.5e-2", N(1.5e-2));
        ExpectResult("1.5e-0xa", N(1.5e-10));
    }

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

    [Fact]
    public void TestListMap()
    {
        ExpectResult("[0,1,2,3]", L(I(0), I(1), I(2), I(3)));
        ExpectResult("[0,..[..[1,2],3]]", L(I(0), I(1), I(2), I(3)));
        
        ExpectResult("{0:'a',1:'b',2:'c'}", M((I(0), S("a")), (I(1), S("b")), (I(2), S("c"))));
        ExpectResult("{0:'a',..{1:'b',2:'c'}}", M((I(0), S("a")), (I(1), S("b")), (I(2), S("c"))));
    }
}