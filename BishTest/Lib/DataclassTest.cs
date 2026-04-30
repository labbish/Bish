namespace BishTest.Lib;

public class DataclassTest(TestInfoFixture fixture) : LibTest(fixture, "dataclass", ["dataclass", "Field"])
{
    [Fact]
    public void TestDataclass()
    {
        Execute("class C:dataclass('a','b',Field('c').default(0));x:=C(1,2,0);y:=C(1,2);");
        ExpectResult("string.from(C.parents[0])", "'dataclass(a, b, c)'");
        ExpectResult("string.from(y)", "'C(a=1, b=2, c=0)'");
        ExpectResult("x==y", "true");
    }
}