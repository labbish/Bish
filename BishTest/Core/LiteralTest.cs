namespace BishTest.Core;

public class LiteralTest(TestInfoFixture fixture) : Test(fixture)
{
    [Fact]
    public void TestIntNum()
    {
        ExpectResult("123", "123");
        ExpectResult("0xAb", "0xAb");
        ExpectResult("0o35", "29");
        ExpectResult("0b1101", "0b1101");
        ExpectResult("1.5", "1.5");
        ExpectResult("1.", "1");
        ExpectResult(".5", ".5");
        ExpectResult("1.5e2", "1.5e2");
        ExpectResult("1.5e-2", "1.5e-2");
        ExpectResult("1.5e-0xa", "1.5e-10");
    }

    [Fact]
    public void TestString()
    {
        ExpectResult(""" 'a\tb"\'' """, """ "a\tb\"'" """);
        ExpectResult(""" r'a\tb' """, """ 'a\\tb' """);
        ExpectResult(""" r"a\tb" """, """ "a\\tb" """);
        ExpectResult(""" r#'a#\tb"''# """, """ 'a#\\tb"\'' """);
        ExpectResult(""" r#"a#\tb"'"# """, """ "a#\\tb\"'" """);
    }

    [Fact]
    public void TestListMapObj()
    {
        Execute("x:=[0,1,2,3];");
        ExpectResult("x[0]", "0");
        ExpectResult("x[3]", "3");
        ExpectResult("[0,..[..[1,2],3]]", "x");
        
        Execute("x:={0:'a',1:'b',2:'c'};");
        ExpectResult("x[0]", "'a'");
        ExpectResult("x[1]", "'b'");
        ExpectResult("x[2]", "'c'");
        ExpectResult("{0:'a',..{1:'b',2:'c'}}", "x");
        
        Execute("x:={.a:1,.b:2};y:={.x,.c:3};");
        ExpectResult("y.c", "3");
        ExpectResult("y.x.a", "1");
        ExpectResult("y.x.b", "2");
    }
}