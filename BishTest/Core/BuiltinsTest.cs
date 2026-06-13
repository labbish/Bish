namespace BishTest.Core;

public class BuiltinsTest(TestInfoFixture fixture) : Test(fixture)
{
    [Fact]
    public void TestInt()
    {
        ExpectTrue("int()===0");
        ExpectTrue("int(128)===128");
        ExpectTrue("int(129)==129");

        ExpectResult("+1", "1");
        ExpectResult("-1", "-1");
        ExpectResult("3+2", "5");
        ExpectResult("3-2", "1");
        ExpectResult("3*2", "6");
        ExpectResult("3/2", "1.5");
        ExpectResult("3%2", "1");
        ExpectResult("(-3)%2", "-1");
        ExpectError("3%0;", BishError.ArgumentErrorType);
        ExpectResult("3^2", "9");

        ExpectResult("(3).abs()", "3");
        ExpectResult("(0).abs()", "0");
        ExpectResult("(-3).abs()", "3");
        ExpectResult("(3).sign()", "1");
        ExpectResult("(0).sign()", "0");
        ExpectResult("(-3).sign()", "-1");
        ExpectResult("(3).show()", "'3'");

        ExpectTrue("3==3");
        ExpectFalse("3==2");
        ExpectTrue("(3<=>2)>0");
        ExpectTrue("(3<=>3)==0");
        ExpectTrue("(2<=>3)<0");
        ExpectFalse("(0).bool()");
        ExpectTrue("(3).bool()");
    }

    [Fact]
    public void TestNum()
    {
        ExpectResult("num()", "0");
        ExpectResult("num(3)", "3");

        ExpectResult("+1.0", "1");
        ExpectResult("-1.0", "-1");
        ExpectResult("3.0+2.0", "5");
        ExpectResult("3.0-2.0", "1");
        ExpectResult("3.0*2.0", "6");
        ExpectResult("3.0/2.0", "1.5");
        ExpectResult("3.0%2.0", "1");
        ExpectResult("(-3.0)%2.0", "-1");
        ExpectResult("3.0^2.0", "9");

        ExpectResult("(3.0).abs()", "3");
        ExpectResult("(0.0).abs()", "0");
        ExpectResult("(-3.0).abs()", "3");
        ExpectResult("(3.0).sign()", "1");
        ExpectResult("(0.0).sign()", "0");
        ExpectResult("(-3.0).sign()", "-1");
        ExpectResult("(1.3).floor()", "1");
        ExpectResult("(1.3).ceil()", "2");
        ExpectResult("(1.3).round()", "1");
        ExpectResult("(1.7).round()", "2");
        ExpectResult("(3.0).show()", "'3'");

        ExpectTrue("3.0==3.0");
        ExpectFalse("3.0==2.0");
        ExpectTrue("(3.0<=>2.0)>0");
        ExpectTrue("(3.0<=>3.0)==0");
        ExpectTrue("(2.0<=>3.0)<0");
        ExpectFalse("(0.0).bool()");
        ExpectTrue("(3.0).bool()");

        ExpectTrue("num.E is of num");
        ExpectTrue("num.PI is of num");
    }

    [Fact]
    public void TestBool()
    {
        ExpectTrue("bool()===false");
        ExpectTrue("bool(true)===true");

        ExpectTrue("~false");
        ExpectFalse("~true");

        ExpectTrue("false==false");
        ExpectFalse("false==true");
        ExpectFalse("true==false");
        ExpectTrue("true==true");

        ExpectFalse("(false).bool()");
        ExpectTrue("(true).bool()");
    }

    [Fact]
    public void TestString()
    {
        ExpectResult("string()", "''");
        ExpectResult("string('abc')", "'abc'");

        ExpectResult("'a'+'bc'", "'abc'");
        ExpectResult("'a'*3", "'aaa'");
        ExpectResult("3*'a'", "'aaa'");
        ExpectResult("('abc').show()", "'abc'");
        ExpectTrue("'a'=='a'");
        ExpectFalse("'a'=='b'");
        ExpectFalse("''.bool()");
        ExpectTrue("'a'.bool()");
        ExpectResult("('abc')[1]", "'b'");
        ExpectResult("('abc')[-1]", "'c'");
        ExpectError("('abc')[3];", BishError.ArgumentErrorType);
        ExpectError("('abc')[-4];", BishError.ArgumentErrorType);
        ExpectResult("('abc').length", "3");

        Execute("iter:='abc'.iter();");
        ExpectResult("iter.next()", "'a'");
        ExpectResult("iter.next()", "'b'");
        ExpectResult("iter.next()", "'c'");
        ExpectResult("iter.next()", "IteratorStop");

        ExpectResult("'{},{?},{},{?},{}'.format('a','b','c')", "r#'a,'b',c,{?},{}'#");
        ExpectResult("'0,1,,2'.split(',')", "['0','1','','2']");
        
        ExpectResult("'x'.toCode()", "120");
        ExpectResult("string.fromCode(120)", "'x'");
    }

    [Fact]
    public void TestRange()
    {
        Execute("r:=range(1,10,3);");
        ExpectResult("r.next()", "1");
        ExpectResult("r.next()", "4");
        ExpectResult("r.next()", "7");
        ExpectResult("r.next()", "IteratorStop");

        ExpectResult("r.start", "1");
        ExpectResult("r.end", "10");
        ExpectResult("r.step", "3");

        Execute("reversed:=range(10,1,-3);");
        ExpectResult("reversed.next()", "10");
        ExpectResult("reversed.next()", "7");
        ExpectResult("reversed.next()", "4");
        ExpectResult("reversed.next()", "IteratorStop");

        ExpectResult("range(1,10,1)", "range(1,10)");
        ExpectResult("range(0,10,1)", "range(10)");

        ExpectError("range(0,0,0);", BishError.ArgumentErrorType);
    }

    [Fact]
    public void TestList()
    {
        ExpectResult("list()", "[]");

        Execute("a:=0;b:='x';c:=true;");
        ExpectResult("[a,b]+[c]", "[0,'x',true]");
        ExpectResult("[a,b]*3", "[0,'x',0,'x',0,'x']");
        ExpectResult("3*[a,b]", "[0,'x',0,'x',0,'x']");
        ExpectFalse("[a,[b,c]]==[a,[c,c]]");
        ExpectTrue("[a,[b,c]]==[a,[b,c]]");
        ExpectFalse("([]).bool()");
        ExpectTrue("([a]).bool()");

        Execute("l:=[a,c,b];");
        ExpectResult("l[0]", "0");
        ExpectResult("l[-1]", "'x'");
        Execute("l[1]=b;"); //[a,b,b]
        Execute("del l[2];"); //[a,b]
        Execute("l.add(c);"); //[a,b,c]
        ExpectResult("l", "[0,'x',true]");
        ExpectResult("l.length", "3");

        ExpectError("l[3];", BishError.ArgumentErrorType);
        ExpectError("del l[-4];", BishError.ArgumentErrorType);

        Execute("iter:=l.iter();");
        ExpectResult("iter.next()", "0");
        ExpectResult("iter.next()", "'x'");
        ExpectTrue("iter.next()");
        ExpectResult("iter.next()", "IteratorStop");
        ExpectResult("l", "[0,'x',true]");

        ExpectResult("list(range(5))", "[0,1,2,3,4]");

        ExpectResult("[0,1,2,3].iter().join(',')", "'0,1,2,3'");
    }

    [Fact]
    public void TestRangeIndex()
    {
        Execute("s:='0123456789';");
        ExpectResult("s[2:7]", "'23456'");
        ExpectResult("s[5:-2]", "'567'");
        ExpectResult("s[2:-1:2]", "'2468'");
        ExpectResult("s[8:3:-1]", "'87654'");

        Execute("l:=[0,1,2,3,4,5,6,7,8,9];");
        ExpectResult("l[2:7]", "[2,3,4,5,6]");
        ExpectResult("l[5:-2]", "[5,6,7]");
        ExpectResult("l[2:-1:2]", "[2,4,6,8]");
        ExpectResult("l[8:3:-1]", "[8,7,6,5,4]");

        ExpectResult("l[2:7]=[null,null,null]", "[null,null,null]");
        ExpectResult("l", "[0,1,null,null,null,7,8,9]");
        Execute("l[6:0:-2]=['a','b','c'];");
        ExpectResult("l", "[0,1,'c',null,'b',7,'a',9]");

        ExpectResult("del l[1:8:3]", "[1,'b',9]");
        ExpectResult("l", "[0,'c',null,7,'a']");
    }

    [Fact]
    public void TestMap()
    {
        ExpectResult("map()", "{}");

        Execute("a:=0;b:='x';c:=true;d:=null;");
        ExpectResult("{a:b,c:d}+{c:a}", "{0:'x',true:0}");
        ExpectFalse("{a:b,c:d}=={c:d,a:c}");
        ExpectTrue("{a:b,c:d}=={c:d,a:b}");
        ExpectFalse("({}).bool()");
        ExpectTrue("({a:b}).bool()");

        Execute("l:={a:b,c:d};");
        ExpectResult("l[a]", "'x'");
        ExpectResult("l[c]", "null");
        Execute("l[a]=c;"); //{a:c,c:d}
        Execute("del l[c];"); //{a:c}
        ExpectResult("l", "{0:true}");
        ExpectResult("l.length", "1");

        ExpectError("l[b];", BishError.ArgumentErrorType);
        ExpectError("del l[c];", BishError.ArgumentErrorType);

        Execute("l={a:b,c:d};");
        Execute("iter:=l.iter();");
        ExpectResult("iter.next()", "[0,'x']");
        ExpectResult("iter.next()", "[true,null]");
        ExpectResult("iter.next()", "IteratorStop");
        ExpectResult("l", "{0:'x',true:null}");

        ExpectResult("l.keys", "[0,true]");
        ExpectResult("l.values", "['x',null]");
        ExpectResult("l.entries", "[[0,'x'],[true,null]]");
    }

    [Fact]
    public void TestType()
    {
        Execute("class C{a:=b:=0};");
        Execute("D:=type('X',[C]);");
        Execute("D.a:=1;");
        ExpectResult("D().a", "1");
        ExpectResult("D().b", "0");
        
        ExpectResult("C.name", "'C'");
        ExpectResult("D.name", "'X'");
        
        ExpectResult("C.MRO", "[C]");
        ExpectResult("D.MRO", "[D,C]");
        
        ExpectResult("C.parents", "[]");
        ExpectResult("D.parents", "[C]");
        Execute("del D.parents[0];");
        ExpectError("D().b", BishError.AttributeErrorType);
    }
}