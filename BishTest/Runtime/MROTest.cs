#pragma warning disable CS0108, CS0114
namespace BishTest.Runtime;

public class MROTest : Test
{
    public readonly BishType O, A, B, C, D, E;

    public MROTest()
    {
        O = T("O");
        A = T("A", O);
        B = T("B", O);
        C = T("C", A, B);
        D = T("D", B, A);
        E = T("E", C, D);
    }
    
    [Fact]
    public void TestMRO()
    {
        O.GetMRO().Select(type => type.Name).Should().BeEquivalentTo("O");
        A.GetMRO().Select(type => type.Name).Should().BeEquivalentTo("A", "O");
        B.GetMRO().Select(type => type.Name).Should().BeEquivalentTo("B", "O");
        C.GetMRO().Select(type => type.Name).Should().BeEquivalentTo("C", "A", "B", "O");
        D.GetMRO().Select(type => type.Name).Should().BeEquivalentTo("D", "B", "A", "O");
        Action(() => E.GetMRO()).Should().Excepts(BishError.ArgumentErrorType);
    }
}