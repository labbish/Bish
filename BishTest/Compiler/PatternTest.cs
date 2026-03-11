namespace BishTest.Compiler;

public class PatternTest(TestInfoFixture fixture) : Test(fixture)
{
    [Fact]
    public void TestMatch()
    {
        ExpectResult("1 is _", True);
        ExpectResult("1 is null", False);
        ExpectResult("null is null", True);
        ExpectResult("3 is 1+2", True);
        ExpectResult("5 is 1+2", False);
        ExpectResult("3 is >=1 and <4", True);
        ExpectResult("3 is not (<1 or >=4)", True);
        ExpectResult("0 is of int x", True);
        ExpectResult("1 is of num y", True);
        ExpectResult("2 is of string z", False);
        Scope.GetVar("x").Should().BeEquivalentTo(I(0));
        Scope.GetVar("y").Should().BeEquivalentTo(N(1));
        Scope.TryGetVar("z").Should().BeNull();
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
            ("of _ x", "x && -1")
        ];
        var expr = $"s1:=({value}) switch" + "{" +
                   string.Join(",", cases.Select(branch => $"{branch.Pattern}=>{branch.Expr}")) + "};";
        var stat = $"s2:=null;switch ({value})" + "{" +
                   string.Join("", cases.Select(branch => $"case {branch.Pattern}: s2={branch.Expr};")) + "}";
        Execute(expr);
        Execute(stat);
        Execute($"s:={expect};");
        BishObject s = Scope.GetVar("s"), s1 = Scope.GetVar("s1"), s2 = Scope.GetVar("s2");
        s1.Should().BeEquivalentTo(s);
        s2.Should().BeEquivalentTo(s);
    }

    [Fact]
    public void TestAs()
    {
        ExpectResult("1 as int", I(1));
        ExpectResult("1 as num", N(1));
        ExpectResult("1 as string", Null);
    }

    [Fact]
    public void TestPipe()
    {
        ExpectResult("'3'|>int.parse($)|>$*$-$+1", I(7));
        ExpectResult("3|>$ as string|?>$[0]|>$*3", Null);
        ExpectResult("'3p'|>try int.parse($)|?>$*$-$+1", Null);
    }
}