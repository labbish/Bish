namespace BishTest.Core;

public class BuiltinsTest(TestInfoFixture fixture) : Test(fixture)
{
    [Fact]
    public void TestInt()
    {
        ExpectResult("int()", I(0));
        ExpectResult("int(3)", I(3));

        ExpectResult("+1", I(1));
        ExpectResult("-1", I(-1));
        ExpectResult("3+2", I(5));
        ExpectResult("3-2", I(1));
        ExpectResult("3*2", I(6));
        ExpectResult("3/2", N(1.5));
        ExpectResult("3%2", I(1));
        ExpectResult("(-3)%2", I(-1));
        Action(() => Execute("3%0;")).Should().Excepts(BishError.ArgumentErrorType);
        ExpectResult("3^2", N(9));

        ExpectResult("(3).abs()", I(3));
        ExpectResult("(0).abs()", I(0));
        ExpectResult("(-3).abs()", I(3));
        ExpectResult("(3).sign()", I(1));
        ExpectResult("(0).sign()", I(0));
        ExpectResult("(-3).sign()", I(-1));
        ExpectResult("(3).toString()", S("3"));

        ExpectResult("3==3", True);
        ExpectResult("3==2", False);
        ExpectResult("(3<=>2)>0", True);
        ExpectResult("(3<=>3)==0", True);
        ExpectResult("(2<=>3)<0", True);
        ExpectResult("(0).bool()", False);
        ExpectResult("(3).bool()", True);
    }

    [Fact]
    public void TestNum()
    {
        ExpectResult("num()", N(0));
        ExpectResult("num(3)", N(3));

        ExpectResult("+1.0", N(1));
        ExpectResult("-1.0", N(-1));
        ExpectResult("3.0+2.0", N(5));
        ExpectResult("3.0-2.0", N(1));
        ExpectResult("3.0*2.0", N(6));
        ExpectResult("3.0/2.0", N(1.5));
        ExpectResult("3.0%2.0", N(1));
        ExpectResult("(-3.0)%2.0", N(-1));
        ExpectResult("3.0^2.0", N(9));

        ExpectResult("(3.0).abs()", N(3));
        ExpectResult("(0.0).abs()", N(0));
        ExpectResult("(-3.0).abs()", N(3));
        ExpectResult("(3.0).sign()", I(1));
        ExpectResult("(0.0).sign()", I(0));
        ExpectResult("(-3.0).sign()", I(-1));
        ExpectResult("(1.3).floor()", I(1));
        ExpectResult("(1.3).ceil()", I(2));
        ExpectResult("(1.3).round()", I(1));
        ExpectResult("(1.7).round()", I(2));
        ExpectResult("(3.0).toString()", S("3"));

        ExpectResult("3.0==3.0", True);
        ExpectResult("3.0==2.0", False);
        ExpectResult("(3.0<=>2.0)>0", True);
        ExpectResult("(3.0<=>3.0)==0", True);
        ExpectResult("(2.0<=>3.0)<0", True);
        ExpectResult("(0.0).bool()", False);
        ExpectResult("(3.0).bool()", True);

        ExpectResult("num.E", N(Math.E));
        ExpectResult("num.PI", N(Math.PI));
    }

    [Fact]
    public void TestBool()
    {
        ExpectResult("bool()", False);
        ExpectResult("bool(true)", True);

        ExpectResult("~false", True);
        ExpectResult("~true", False);

        ExpectResult("false==false", True);
        ExpectResult("false==true", False);
        ExpectResult("true==false", False);
        ExpectResult("true==true", True);

        ExpectResult("(false).bool()", False);
        ExpectResult("(true).bool()", True);
    }

    [Fact]
    public void TestString()
    {
        ExpectResult("string()", S(""));
        ExpectResult("string('abc')", S("abc"));

        ExpectResult("'a'+'bc'", S("abc"));
        ExpectResult("'a'*3", S("aaa"));
        ExpectResult("3*'a'", S("aaa"));
        ExpectResult("('abc').toString()", S("abc"));
        ExpectResult("'a'=='a'", True);
        ExpectResult("'a'=='b'", False);
        ExpectResult("''.bool()", False);
        ExpectResult("'a'.bool()", True);
        ExpectResult("('abc')[1]", S("b"));
        ExpectResult("('abc')[-1]", S("c"));
        Action(() => Execute("('abc')[3];")).Should().Excepts(BishError.ArgumentErrorType);
        Action(() => Execute("('abc')[-4];")).Should().Excepts(BishError.ArgumentErrorType);
        ExpectResult("('abc').length", I(3));

        Execute("iter:='abc'.iter();");
        ExpectResult("iter.next()", S("a"));
        ExpectResult("iter.next()", S("b"));
        ExpectResult("iter.next()", S("c"));
        Action(() => Execute("iter.next();")).Should().Excepts(BishError.IteratorStopType);

        ExpectResult("'{1},{},{},{0},{}'.format(0,1,2)", S("1,0,1,0,2"));
        ExpectResult("'0,1,,2'.split(',')", L(S("0"), S("1"), S(""), S("2")));
    }

    [Fact]
    public void TestRange()
    {
        Execute("r:=range(1,10,3);");
        ExpectResult("r.next()", I(1));
        ExpectResult("r.next()", I(4));
        ExpectResult("r.next()", I(7));
        Action(() => Execute("r.next();")).Should().Excepts(BishError.IteratorStopType);

        ExpectResult("r.start", I(1));
        ExpectResult("r.end", I(10));
        ExpectResult("r.step", I(3));

        Execute("reversed:=range(10,1,-3);");
        ExpectResult("reversed.next()", I(10));
        ExpectResult("reversed.next()", I(7));
        ExpectResult("reversed.next()", I(4));
        Action(() => Execute("reversed.next();")).Should().Excepts(BishError.IteratorStopType);

        ExpectResult("range(1,10,1)==range(1,10)", True);
        ExpectResult("range(0,10,1)==range(10)", True);

        Action(() => Execute("range(0,0,0);")).Should().Excepts(BishError.ArgumentErrorType);
    }

    [Fact]
    public void TestList()
    {
        ExpectResult("list()", L());

        Execute("a:=0;b:='x';c:=true;");
        ExpectResult("[a,b]+[c]", L(I(0), S("x"), True));
        ExpectResult("[a,b]*3", L(I(0), S("x"), I(0), S("x"), I(0), S("x")));
        ExpectResult("3*[a,b]", L(I(0), S("x"), I(0), S("x"), I(0), S("x")));
        ExpectResult("[a,[b,c]]==[a,[c,c]]", False);
        ExpectResult("[a,[b,c]]==[a,[b,c]]", True);
        ExpectResult("([]).bool()", False);
        ExpectResult("([a]).bool()", True);

        Execute("l:=[a,c,b];");
        ExpectResult("l[0]", I(0));
        ExpectResult("l[-1]", S("x"));
        Execute("l[1]=b;"); //[a,b,b]
        Execute("del l[2];"); //[a,b]
        Execute("l.add(c);"); //[a,b,c]
        ExpectResult("l", L(I(0), S("x"), True));
        ExpectResult("l.length", I(3));

        Action(() => Execute("l[3];")).Should().Excepts(BishError.ArgumentErrorType);
        Action(() => Execute("del l[-4];")).Should().Excepts(BishError.ArgumentErrorType);

        Execute("iter:=l.iter();");
        ExpectResult("iter.next()", I(0));
        ExpectResult("iter.next()", S("x"));
        ExpectResult("iter.next()", True);
        Action(() => Execute("iter.next();")).Should().Excepts(BishError.IteratorStopType);
        ExpectResult("l", L(I(0), S("x"), True));

        ExpectResult("list(range(5))", L(I(0), I(1), I(2), I(3), I(4)));

        ExpectResult("[0,1,2,3].iter().join(',')", S("0,1,2,3"));
    }

    [Fact]
    public void TestRangeIndex()
    {
        Execute("s:='0123456789';");
        ExpectResult("s[2:7]", S("23456"));
        ExpectResult("s[5:-2]", S("567"));
        ExpectResult("s[2:-1:2]", S("2468"));
        ExpectResult("s[8:3:-1]", S("87654"));

        Execute("l:=[0,1,2,3,4,5,6,7,8,9];");
        ExpectResult("l[2:7]", L(I(2), I(3), I(4), I(5), I(6)));
        ExpectResult("l[5:-2]", L(I(5), I(6), I(7)));
        ExpectResult("l[2:-1:2]", L(I(2), I(4), I(6), I(8)));
        ExpectResult("l[8:3:-1]", L(I(8), I(7), I(6), I(5), I(4)));

        ExpectResult("l[2:7]=[null,null,null]", L(Null, Null, Null));
        ExpectResult("l", L(I(0), I(1), Null, Null, Null, I(7), I(8), I(9)));
        Execute("l[6:0:-2]=['a','b','c'];");
        ExpectResult("l", L(I(0), I(1), S("c"), Null, S("b"), I(7), S("a"), I(9)));

        ExpectResult("del l[1:8:3]", L(I(1), S("b"), I(9)));
        ExpectResult("l", L(I(0), S("c"), Null, I(7), S("a")));
    }

    [Fact]
    public void TestMap()
    {
        ExpectResult("map()", M());

        Execute("a:=0;b:='x';c:=true;d:=null;");
        ExpectResult("{a:b,c:d}+{c:a}", M((I(0), S("x")), (True, I(0))));
        ExpectResult("{a:b,c:d}=={c:d,a:c}", False);
        ExpectResult("{a:b,c:d}=={c:d,a:b}", True);
        ExpectResult("({}).bool()", False);
        ExpectResult("({a:b}).bool()", True);

        Execute("l:={a:b,c:d};");
        ExpectResult("l[a]", S("x"));
        ExpectResult("l[c]", Null);
        Execute("l[a]=c;"); //{a:c,c:d}
        Execute("del l[c];"); //{a:c}
        ExpectResult("l", M((I(0), True)));
        ExpectResult("l.length", I(1));

        Action(() => Execute("l[b];")).Should().Excepts(BishError.ArgumentErrorType);
        Action(() => Execute("del l[c];")).Should().Excepts(BishError.ArgumentErrorType);

        Execute("l={a:b,c:d};");
        Execute("iter:=l.iter();");
        ExpectResult("iter.next()", L(I(0), S("x")));
        ExpectResult("iter.next()", L(True, Null));
        Action(() => Execute("iter.next();")).Should().Excepts(BishError.IteratorStopType);
        ExpectResult("l", M((I(0), S("x")), (True, Null)));

        ExpectResult("l.keys", L(I(0), True));
        ExpectResult("l.values", L(S("x"), Null));
        ExpectResult("l.entries", L(L(I(0), S("x")), L(True, Null)));
    }

    [Fact]
    public void TestType()
    {
        Execute("class C{a:=b:=0};");
        Execute("D:=type('X',[C]);");
        Execute("D.a:=1;");
        ExpectResult("D().a", I(1));
        ExpectResult("D().b", I(0));
        
        ExpectResult("C.name", S("C"));
        ExpectResult("D.name", S("X"));
        
        ExpectResult("C.MRO==[object]", True);
        ExpectResult("D.MRO==[C,object]", True);
        
        ExpectResult("C.parents==[]", True);
        ExpectResult("D.parents==[C]", True);
        Execute("del D.parents[0];");
        Action(() => Execute("D().b")).Should().Excepts(BishError.AttributeErrorType);
    }
}