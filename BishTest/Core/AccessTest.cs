namespace BishTest.Core;

public class AccessTest : Test
{
    public AccessTest(TestInfoFixture fixture) : base(fixture) =>
        Execute("a:=5;b:='abc';n:=null;x:={.y:object(),.z:4};l:=[0,1,2,3,4];");

    [Fact]
    public void TestAccess()
    {
        Execute("a=b.length;c:='c';x.y.z:=0;del x.z;m:=del n;");
        ExpectResult("a", I(3));
        ExpectResult("b", S("abc"));
        ExpectResult("c", S("c"));
        ExpectResult("x.y.z", I(0));
        Action(() => Execute("x.z;")).Should().Excepts(BishError.AttributeErrorType);
        Action(() => Execute("n;")).Should().Excepts(BishError.AttributeErrorType);
        ExpectResult("m", Null);
    }

    [Fact]
    public void TestScopeDepth()
    {
        Execute("{a=b.length;b:='b';}");
        ExpectResult("a", I(3));
        ExpectResult("b", S("abc"));
    }

    [Fact]
    public void TestIndex()
    {
        ExpectResult("l[0]", I(0));
        Execute("l[1]=-1;l[2]*=3;");
        ExpectResult("l", L(I(0), I(-1), I(6), I(3), I(4)));
        Execute("del l[3];");
        ExpectResult("l", L(I(0), I(-1), I(6), I(4)));
    }

    [Fact]
    public void TestRangeIndex()
    {
        Execute("l:=list(range(11));");
        ExpectResult("l[1:7:2]", L(I(1), I(3), I(5)));
        Execute("l[2:8]=['a','b'];");
        ExpectResult("l", L(I(0), I(1), S("a"), S("b"), I(8), I(9), I(10)));
        Execute("l[::3]=[null]*3;");
        ExpectResult("l", L(Null, I(1), S("a"), Null, I(8), I(9), Null));
        Execute("del l[1::2];");
        ExpectResult("l", L(Null, S("a"), I(8), Null));
    }

    [Fact]
    public void TestDeconstruct()
    {
        Execute("o:=object();");
        Execute("[a,[o.b,c],..l]:=[0,[1,2],3,4];");
        ExpectResult("a", I(0));
        ExpectResult("o.b", I(1));
        ExpectResult("c", I(2));
        ExpectResult("l", L(I(3), I(4)));
        Execute("[a,[o.b,c],..l]=['a',['b','c'],'l1','l2'];");
        ExpectResult("a", S("a"));
        ExpectResult("o.b", S("b"));
        ExpectResult("c", S("c"));
        ExpectResult("l", L(S("l1"), S("l2")));
        Execute("del [a,[o.b,c],..l];");
        Action(() => Execute("a;")).Should().Excepts(BishError.AttributeErrorType);
        Action(() => Execute("o.b;")).Should().Excepts(BishError.AttributeErrorType);
        Action(() => Execute("c;")).Should().Excepts(BishError.AttributeErrorType);
        Action(() => Execute("l;")).Should().Excepts(BishError.AttributeErrorType);

        Execute("o:={'a':0,'b':1,'c':2};");
        Execute("{'a':a,'b':b,..c}:=o;");
        ExpectResult("a", I(0));
        ExpectResult("b", I(1));
        ExpectResult("c['c']", I(2));
        Execute("{'a':a,'b':b,..c}={'a':-1,'b':-2,'c':-3};");
        ExpectResult("a", I(-1));
        ExpectResult("b", I(-2));
        ExpectResult("c['c']", I(-3));
        Execute("del {'a':a,'b':b,..c};");
        Action(() => Execute("a;")).Should().Excepts(BishError.AttributeErrorType);
        Action(() => Execute("b;")).Should().Excepts(BishError.AttributeErrorType);
        Action(() => Execute("c;")).Should().Excepts(BishError.AttributeErrorType);
        ExpectResult("o['a']", I(0));

        Execute("o:={.a:0,.b:1,.c:2};");
        Execute("{.a,.b}:=o;");
        ExpectResult("a", I(0));
        ExpectResult("b", I(1));
        Execute("{.a,.b}={.a:-1,.b:-2,.c:-3};");
        ExpectResult("a", I(-1));
        ExpectResult("b", I(-2));
        Execute("del {.a,.b};");
        Action(() => Execute("a;")).Should().Excepts(BishError.AttributeErrorType);
        Action(() => Execute("b;")).Should().Excepts(BishError.AttributeErrorType);
        ExpectResult("o.a", I(0));
    }

    [Fact]
    public void TestBuiltins()
    {
        Execute("map:=0;");
        ExpectResult("{0:0,1:1}", M((I(0), I(0)), (I(1), I(1))));
    }
}