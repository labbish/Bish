namespace BishTest.Core;

public class MROTest : Test
{
    public MROTest(TestInfoFixture fixture) : base(fixture) =>
        Execute("class O;class A:O;class B:O;class C:A,B;class D:B,A;");

    [Fact]
    public void TestMRO()
    {
        Execute("func f(t)t.MRO.iter().join();");
        ExpectResult("f(O)", "'O'");
        ExpectResult("f(A)", "'AO'");
        ExpectResult("f(B)", "'BO'");
        ExpectResult("f(C)", "'CABO'");
        ExpectResult("f(D)", "'DBAO'");
        ExpectError("class E:C,D;", BishError.ArgumentErrorType);
    }
}