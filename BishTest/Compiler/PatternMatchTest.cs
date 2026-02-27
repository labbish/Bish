namespace BishTest.Compiler;

public class PatternMatchTest(OptimizeInfoFixture fixture) : CompilerTest(fixture)
{
    [Fact]
    public void TestMatch()
    {
        ExpectResult("1 is _", B(true));
        ExpectResult("1 is null", B(false));
        ExpectResult("null is null", B(true));
        ExpectResult("3 is 1+2", B(true));
        ExpectResult("5 is 1+2", B(false));
        ExpectResult("3 is >=1 and <4", B(true));
        ExpectResult("3 is not (<1 or >=4)", B(true));
        ExpectResult("0 is of int x", B(true));
        ExpectResult("1 is of num y", B(true));
        ExpectResult("2 is of string z", B(false));
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
    [InlineData("'str'", "'s'")]
    [InlineData("['s','t','r']", "'s'")]
    [InlineData("3.14", "-1")]
    public void TestSwitch(string value, string expect)
    {
        List<(string Pattern, string Expr)> cases =
        [
            ("null", "0"),
            ("1", "1"),
            ("of int and <0", "2"),
            ("of num n and <=0", "-n"),
            ("of string seq or of list seq", "seq[0]"),
            ("_", "-1")
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