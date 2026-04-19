namespace BishTest.Core;

public class MROTest : Test
{
    public MROTest(TestInfoFixture fixture) : base(fixture) =>
        Execute("class O;class A:O;class B:O;class C:A,B;class D:B,A;");

    [Fact]
    public void TestMRO()
    {
        Execute("func f(t)t.MRO.iter().join();");
        ExpectResult("f(O)", S("O"));
        ExpectResult("f(A)", S("AO"));
        ExpectResult("f(B)", S("BO"));
        ExpectResult("f(C)", S("CABO"));
        ExpectResult("f(D)", S("DBAO"));
        Action(() => Execute("class E:C,D;")).Should().Excepts(BishError.ArgumentErrorType);
    }
}