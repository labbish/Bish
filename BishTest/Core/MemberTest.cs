namespace BishTest.Core;

public class MemberTest : Test
{
    public MemberTest(TestInfoFixture fixture) : base(fixture) =>
        Execute("class T1{a:=b:=0;};class T2:T1{a:=c:=1;};x:=T2();x.c:=2;");

    [Fact]
    public void TestGetMember()
    {
        ExpectResult("T1.a", "0");
        ExpectResult("T1.b", "0");
        ExpectError("T1.c;", BishError.AttributeErrorType);
        ExpectResult("T2.a", "1");
        ExpectResult("T2.b", "0");
        ExpectResult("T2.c", "1");
        ExpectResult("x.a", "1");
        ExpectResult("x.b", "0");
        ExpectResult("x.c", "2");
    }

    [Fact]
    public void TestSetMember()
    {
        Execute("T2.a=3;x.b=4;x.c=5;");

        ExpectResult("T1.a", "0");
        ExpectResult("T1.b", "4");
        ExpectError("T1.c;", BishError.AttributeErrorType);
        ExpectResult("T2.a", "3");
        ExpectResult("T2.b", "4");
        ExpectResult("T2.c", "1");
        ExpectResult("x.a", "3");
        ExpectResult("x.b", "4");
        ExpectResult("x.c", "5");
    }

    [Fact]
    public void TestDefMember()
    {
        Execute("T2.a:=3;x.b:=4;x.c:=5;");

        ExpectResult("T1.a", "0");
        ExpectResult("T1.b", "0");
        ExpectError("T1.c;", BishError.AttributeErrorType);
        ExpectResult("T2.a", "3");
        ExpectResult("T2.b", "0");
        ExpectResult("T2.c", "1");
        ExpectResult("x.a", "3");
        ExpectResult("x.b", "4");
        ExpectResult("x.c", "5");
    }

    [Fact]
    public void TestDelMember()
    {
        ExpectResult("del T2.a", "1");
        ExpectError("del x.b;", BishError.AttributeErrorType);
        ExpectResult("del x.c", "2");

        ExpectResult("T1.a", "0");
        ExpectResult("T1.b", "0");
        ExpectError("T1.c;", BishError.AttributeErrorType);
        ExpectResult("T2.a", "0");
        ExpectResult("T2.b", "0");
        ExpectResult("T2.c", "1");
        ExpectResult("x.a", "0");
        ExpectResult("x.b", "0");
        ExpectResult("x.c", "1");
    }
}