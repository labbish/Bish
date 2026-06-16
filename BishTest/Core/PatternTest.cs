namespace BishTest.Core;

public class PatternTest(TestInfoFixture fixture) : Test(fixture)
{
    [Fact]
    public void TestMatch()
    {
        ExpectTrue("1 is _");
        ExpectFalse("1 is null");
        ExpectTrue("null is null");
        ExpectTrue("3 is 1+2");
        ExpectFalse("5 is 1+2");
        ExpectTrue("3 is >=1 and <4");
        ExpectTrue("3 is not (<1 or >=4)");
        ExpectTrue("0 is of int x");
        ExpectTrue("1 is of num y");
        ExpectFalse("2 is of string z");
        ExpectResult("x", "0");
        ExpectResult("y", "1");
        ExpectError("z", BishError.AttributeErrorType);
        ExpectCompileError("0 is [..a,..b];");
    }

    [Theory]
    [InlineData("null", "0")]
    [InlineData("1", "1")]
    [InlineData("-1", "2")]
    [InlineData("-0.5", "0.5")]
    [InlineData("0.0", "0.0")]
    [InlineData("[0,3]", "4")]
    [InlineData("[0,1,2,3,4]", "[1,2,3]")]
    [InlineData("'str'", "'s'")]
    [InlineData("['s','t','r']", "'s'")]
    [InlineData("{'a':3,'b':4,'c':5}", "6")]
    [InlineData("{'a':'','b':4,'c':5}", "-1")]
    [InlineData("{'b':4,'c':5}", "-1")]
    [InlineData("{.a:3,.b:4}", "9.0")]
    [InlineData("{.b:4}", "-1")]
    [InlineData("3.14", "-1")]
    [InlineData("false", "false")]
    public void TestSwitch(string value, string expect)
    {
        List<(string Pattern, string Expr)> cases =
        [
            ("null", "0"),
            ("1", "1"),
            ("of int and <0", "2"),
            ("of num n when n<=0", "-n"),
            ("[0, of int x]", "x+1"),
            ("[of int, ..of list m, of int]", "m"),
            ("of string seq or of list seq", "seq[0]"),
            ("{'a':of int a, ..of _ rest}", "a*rest.length"),
            ("{.a:of int a}", "a^2"),
            ("of _ x", "x&&-1")
        ];
        var expr = $"switch {value}" + "{" +
                   string.Join(",", cases.Select(branch => $"{branch.Pattern}=>{branch.Expr}")) + "}";
        var stat = $"s:=null;switch {value}" + "{" +
                   string.Join(",", cases.Select(branch => $"{branch.Pattern}=>{{s={branch.Expr};}}")) + "};s";
        ExpectResult(expr, expect);
        ExpectResult(stat, expect);
    }

    [Fact]
    public void TestAs()
    {
        ExpectResult("1 as int", "1");
        ExpectResult("1 as num", "1");
        ExpectResult("1 as string", "null");
    }

    [Fact]
    public void TestPipe()
    {
        ExpectResult("'3'|>int.parse($)|>$*$-$+1", "7");
        ExpectResult("3|>$ as string|?>$[0]|>$*3", "null");
        ExpectErrorResult("'3p'|>try int.parse($)|?>$*$-$+1");
    }
}