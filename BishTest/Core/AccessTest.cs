namespace BishTest.Core;

public class AccessTest : Test
{
    public AccessTest(TestInfoFixture fixture) : base(fixture) =>
        Execute("a:=5;b:='abc';n:=null;x:={.y:object(),.z:4};l:=[0,1,2,3,4];");

    [Fact]
    public void TestAccess()
    {
        Execute("a=b.length;c:='c';x.y.z:=0;del x.z;m:=del n;");
        ExpectResult("a", "3");
        ExpectResult("b", "'abc'");
        ExpectResult("c", "'c'");
        ExpectResult("x.y.z", "0");
        ExpectError("x.z;", BishError.AttributeErrorType);
        ExpectError("n;", BishError.AttributeErrorType);
        ExpectResult("m", "null");
    }

    [Fact]
    public void TestScopeDepth()
    {
        Execute("{a=b.length;b:='b';}");
        ExpectResult("a", "3");
        ExpectResult("b", "'abc'");
    }

    [Fact]
    public void TestIndex()
    {
        ExpectResult("l[0]", "0");
        Execute("l[1]=-1;l[2]*=3;");
        ExpectResult("l", "[0,-1,6,3,4]");
        Execute("del l[3];");
        ExpectResult("l", "[0,-1,6,4]");
    }

    [Fact]
    public void TestRangeIndex()
    {
        Execute("l:=list(range(11));");
        ExpectResult("l[1:7:2]", "[1,3,5]");
        Execute("l[2:8]=['a','b'];");
        ExpectResult("l", "[0,1,'a','b',8,9,10]");
        Execute("l[::3]=[null]*3;");
        ExpectResult("l", "[null,1,'a',null,8,9,null]");
        Execute("del l[1::2];");
        ExpectResult("l", "[null,'a',8,null]");
    }

    [Fact]
    public void TestDeconstruct()
    {
        Execute("o:=object();");
        Execute("[a,[o.b,c],..l]:=[0,[1,2],3,4];");
        ExpectResult("a", "0");
        ExpectResult("o.b", "1");
        ExpectResult("c", "2");
        ExpectResult("l", "[3,4]");
        Execute("[a,[o.b,c],..l]=['a',['b','c'],'l1','l2'];");
        ExpectResult("a", "'a'");
        ExpectResult("o.b", "'b'");
        ExpectResult("c", "'c'");
        ExpectResult("l", "['l1','l2']");
        Execute("del [a,[o.b,c],..l];");
        ExpectError("a;", BishError.AttributeErrorType);
        ExpectError("o.b;", BishError.AttributeErrorType);
        ExpectError("c;", BishError.AttributeErrorType);
        ExpectError("l;", BishError.AttributeErrorType);
        ExpectCompileError("[..a,..b]:=0;");

        Execute("o:={'a':0,'b':1,'c':2};");
        Execute("{'a':a,'b':b,..c}:=o;");
        ExpectResult("a", "0");
        ExpectResult("b", "1");
        ExpectResult("c['c']", "2");
        Execute("{'a':a,'b':b,..c}={'a':-1,'b':-2,'c':-3};");
        ExpectResult("a", "-1");
        ExpectResult("b", "-2");
        ExpectResult("c['c']", "-3");
        Execute("del {'a':a,'b':b,..c};");
        ExpectError("a;", BishError.AttributeErrorType);
        ExpectError("b;", BishError.AttributeErrorType);
        ExpectError("c;", BishError.AttributeErrorType);
        ExpectResult("o['a']", "0");
        ExpectCompileError("{..m,'a':0}:=0;");

        Execute("o:={.a:0,.b:1,.c:2};");
        Execute("{.a,.b:c}:=o;");
        ExpectResult("a", "0");
        ExpectResult("c", "1");
        Execute("{.a,.b:c}={.a:-1,.b:-2,.c:-3};");
        ExpectResult("a", "-1");
        ExpectResult("c", "-2");
        Execute("del {.a,.c};");
        ExpectError("a;", BishError.AttributeErrorType);
        ExpectError("c;", BishError.AttributeErrorType);
        ExpectResult("o.a", "0");
        ExpectCompileError("{.a:0}:=0;");
    }

    [Fact]
    public void TestBuiltins()
    {
        Execute("map:=0;");
        ExpectResult("{0:0,1:1}[0]", "0");
    }
}