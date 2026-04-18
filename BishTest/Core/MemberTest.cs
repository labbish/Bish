namespace BishTest.Core;

public class MemberTest : Test
{
    public MemberTest(TestInfoFixture fixture) : base(fixture) =>
        Execute("class T1{a:=b:=0;};class T2:T1{a:=c:=1;};x:=T2();x.c:=2;");

    [Fact]
    public void TestGetMember()
    {
        ExpectResult("T1.a", I(0));
        ExpectResult("T1.b", I(0));
        Action(() => Execute("T1.c;")).Should().Excepts(BishError.AttributeErrorType);
        ExpectResult("T2.a", I(1));
        ExpectResult("T2.b", I(0));
        ExpectResult("T2.c", I(1));
        ExpectResult("x.a", I(1));
        ExpectResult("x.b", I(0));
        ExpectResult("x.c", I(2));
    }

    [Fact]
    public void TestSetMember()
    {
        Execute("T2.a=3;x.b=4;x.c=5;");

        ExpectResult("T1.a", I(0));
        ExpectResult("T1.b", I(4));
        Action(() => Execute("T1.c;")).Should().Excepts(BishError.AttributeErrorType);
        ExpectResult("T2.a", I(3));
        ExpectResult("T2.b", I(4));
        ExpectResult("T2.c", I(1));
        ExpectResult("x.a", I(3));
        ExpectResult("x.b", I(4));
        ExpectResult("x.c", I(5));
    }

    [Fact]
    public void TestDefMember()
    {
        Execute("T2.a:=3;x.b:=4;x.c:=5;");

        ExpectResult("T1.a", I(0));
        ExpectResult("T1.b", I(0));
        Action(() => Execute("T1.c;")).Should().Excepts(BishError.AttributeErrorType);
        ExpectResult("T2.a", I(3));
        ExpectResult("T2.b", I(0));
        ExpectResult("T2.c", I(1));
        ExpectResult("x.a", I(3));
        ExpectResult("x.b", I(4));
        ExpectResult("x.c", I(5));
    }

    [Fact]
    public void TestDelMember()
    {
        ExpectResult("del T2.a", I(1));
        Action(() => Execute("del x.b;")).Should().Excepts(BishError.AttributeErrorType);
        ExpectResult("del x.c", I(2));

        ExpectResult("T1.a", I(0));
        ExpectResult("T1.b", I(0));
        Action(() => Execute("T1.c;")).Should().Excepts(BishError.AttributeErrorType);
        ExpectResult("T2.a", I(0));
        ExpectResult("T2.b", I(0));
        ExpectResult("T2.c", I(1));
        ExpectResult("x.a", I(0));
        ExpectResult("x.b", I(0));
        ExpectResult("x.c", I(1));
    }
}