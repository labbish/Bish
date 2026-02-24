namespace BishTest.Compiler;

public class StatementsTest : CompilerTest
{
    [Fact]
    public void TestIf()
    {
        Execute("x:=0;if(true)x=1;");
        Scope.GetVar("x").Should().BeEquivalentTo(I(1));
        Execute("x:=0;if(false)x=1;");
        Scope.GetVar("x").Should().BeEquivalentTo(I(0));
        Execute("x:=0;if(true){x=1;y:=2;}else y:=3;");
        Scope.GetVar("x").Should().BeEquivalentTo(I(1));
        Scope.TryGetVar("y").Should().BeNull();
        Execute("x:=0;if(false){x=1;y:=2;}else y:=3;");
        Scope.GetVar("x").Should().BeEquivalentTo(I(0));
        Scope.TryGetVar("y").Should().BeNull();
    }

    [Fact]
    public void TestWhile()
    {
        Execute("x:=1;i:=4;while(i>0){x=x*i;i=i-1;}");
        Scope.GetVar("x").Should().BeEquivalentTo(I(24));
        Execute("x:=0;while(false)x=1;");
        Scope.GetVar("x").Should().BeEquivalentTo(I(0));
        Execute("x:=1;i:=4;do{x=x*i;i=i-1;}while(i>0);");
        Scope.GetVar("x").Should().BeEquivalentTo(I(24));
        Execute("x:=0;do x=1;while(false);");
        Scope.GetVar("x").Should().BeEquivalentTo(I(1));
    }

    [Fact]
    public void TestFor()
    {
        Execute("x:=1;for(i:=1;i<5;i=i+1)x=x*i;");
        Scope.GetVar("x").Should().BeEquivalentTo(I(24));
        Execute("x:=1;for(i:range(1,5))x=x*i;");
        Scope.GetVar("x").Should().BeEquivalentTo(I(24));
    }
}