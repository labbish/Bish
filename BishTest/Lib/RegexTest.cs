namespace BishTest.Lib;

public class RegexTest(TestInfoFixture fixture) : LibTest(fixture, "regex", ["Regex"])
{
    [Fact]
    public void TestFlag()
    {
        ExpectFalse("Regex('^abc$','i').match('AbC') is null");
        ExpectFalse(@"Regex('^x$','m').match('a\nx\nb') is null");
        ExpectFalse(@"Regex('^a.b$','s').match('a\nb') is null");
        
        ExpectResult("Regex('abc').pattern", "'abc'");
        ExpectResult("Regex('abc').flags", "null");
        ExpectResult("Regex('abc','misc').flags", "'misc'");
    }

    [Fact]
    public void TestMatch()
    {
        Execute("m:=Regex('([Aa])+(?<n>b)?c').match('xaAcs');");
        ExpectResult("m.value", "'aAc'");
        ExpectResult("m.start", "1");
        ExpectResult("m.end", "4");
        ExpectResult("m.length", "3");
        ExpectResult("m.name", "'0'");

        Execute("g:=m.groups;");
        ExpectResult("g[0].value", "'aAc'");
        ExpectResult("g[1].value", "'A'");
        ExpectResult("g[2]", "null");
        ExpectResult("g['n']", "null");
        
        Execute("c:=g[1].captures;");
        ExpectResult("c[0].value", "'a'");
        ExpectResult("c[1].value", "'A'");
        ExpectResult("c.iter().map((x)x.value).join(',')", "'a,A'");
    }

    [Fact]
    public void TestMatchAll()
    {
        Execute("m:=Regex('[Aa]b?').matchAll('ab,aAb');");
        ExpectResult("m[0].value", "'ab'");
        ExpectResult("m[1].value", "'a'");
        ExpectResult("m[2].value", "'Ab'");
    }

    [Fact]
    public void TestReplace()
    {
        Execute("r:=Regex('([Aa])b?');s:='ab,aAb';");
        ExpectResult("r.replace(s,'c')", "'c,cc'");
        ExpectResult("r.replace(s,(m)m.groups[1].value)", "'a,aA'");
    }

    [Fact]
    public void TestSplit()
    {
        ExpectResult("Regex('[,;]').split('a,b;c')", "['a','b','c']");
    }
}